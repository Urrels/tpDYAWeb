using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BLL
{
    [TestClass]
    public class BitacoraPaginadoTests
    {
        private static List<global::BE.BITACORA> Filas(int cantidad) =>
            Enumerable.Range(1, cantidad).Select(i => new global::BE.BITACORA
            {
                Id = i,
                Usuario = "u" + i,
                Accion = "A" + i,
                Criticidad = "Media",
                Fecha = new DateTime(2026, 1, 1).AddDays(i)
            }).ToList();

        [TestMethod]
        public void Paginar_OrdenaDelMasNuevoAlMasViejo_PorId()
        {
            var r = global::DAL.BitacoraDAL.Paginar(Filas(5), null, null, 1, 10);
            CollectionAssert.AreEqual(new[] { 5, 4, 3, 2, 1 }, r.Filas.Select(f => f.Id).ToArray());
            Assert.AreEqual(5, r.TotalFilas);
        }

        [TestMethod]
        public void Paginar_DevuelveLaPaginaPedidaYElTotalDeFilas()
        {
            var r = global::DAL.BitacoraDAL.Paginar(Filas(25), null, null, 2, 10);
            Assert.AreEqual(25, r.TotalFilas);
            Assert.AreEqual(3, r.TotalPaginas);
            CollectionAssert.AreEqual(Enumerable.Range(6, 10).Reverse().ToArray(), r.Filas.Select(f => f.Id).ToArray());
            Assert.AreEqual(2, r.Pagina);
        }

        [TestMethod]
        public void Paginar_ElRangoDeFechasEsDesdeInclusivoYHastaExclusivo()
        {
            DateTime desde = new DateTime(2026, 1, 1).AddDays(3);
            DateTime hasta = new DateTime(2026, 1, 1).AddDays(6);
            var r = global::DAL.BitacoraDAL.Paginar(Filas(10), desde, hasta, 1, 50);
            CollectionAssert.AreEqual(new[] { 5, 4, 3 }, r.Filas.Select(f => f.Id).ToArray());
            Assert.AreEqual(3, r.TotalFilas);
        }

        [TestMethod]
        public void Paginar_UnaPaginaPasadaDelFinal_DevuelveVacioConElTotalReal()
        {
            var r = global::DAL.BitacoraDAL.Paginar(Filas(5), null, null, 9, 10);
            Assert.AreEqual(0, r.Filas.Count);
            Assert.AreEqual(5, r.TotalFilas);
        }

        [TestMethod]
        public void Paginar_ConParametrosInvalidos_UsaValoresPorDefecto()
        {
            var r = global::DAL.BitacoraDAL.Paginar(Filas(60), null, null, 0, 0);
            Assert.AreEqual(1, r.Pagina);
            Assert.AreEqual(50, r.Filas.Count);
        }

        [TestMethod]
        public void Paginacion_AjustaAUltimaPaginaCuandoLaPedidaNoExiste()
        {
            var r = global::BLL.Paginacion.ConsultarAjustando(9,
                p => global::DAL.BitacoraDAL.Paginar(Filas(25), null, null, p, 10));
            Assert.AreEqual(3, r.Pagina);
            Assert.AreEqual(5, r.Filas.Count);
        }
    }
}
