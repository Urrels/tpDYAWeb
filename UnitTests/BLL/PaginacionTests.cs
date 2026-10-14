using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BLL
{
    [TestClass]
    public class PaginacionTests
    {
        // Simula la consulta paginada de la base: `total` filas numeradas, páginas de `tamanio`.
        private static global::BE.PaginaResultado<int> Consulta(int total, int tamanio, int pagina)
        {
            var r = new global::BE.PaginaResultado<int> { Pagina = pagina, Tamanio = tamanio, TotalFilas = total };
            r.Filas.AddRange(Enumerable.Range(1, total).Skip((pagina - 1) * tamanio).Take(tamanio));
            return r;
        }

        [TestMethod]
        public void PaginaValida_DevuelveEsaPaginaConUnaSolaConsulta()
        {
            var pedidas = new List<int>();
            var r = global::BLL.Paginacion.ConsultarAjustando(2, p => { pedidas.Add(p); return Consulta(25, 10, p); });

            Assert.AreEqual(2, r.Pagina);
            CollectionAssert.AreEqual(Enumerable.Range(11, 10).ToArray(), r.Filas.ToArray());
            CollectionAssert.AreEqual(new[] { 2 }, pedidas);
        }

        [TestMethod]
        public void PaginaMasAllaDeLaUltima_VuelveALaUltimaReal()
        {
            var pedidas = new List<int>();
            var r = global::BLL.Paginacion.ConsultarAjustando(99, p => { pedidas.Add(p); return Consulta(25, 10, p); });

            Assert.AreEqual(3, r.Pagina);
            CollectionAssert.AreEqual(new[] { 21, 22, 23, 24, 25 }, r.Filas.ToArray());
            CollectionAssert.AreEqual(new[] { 99, 1, 3 }, pedidas);
        }

        [TestMethod]
        public void PaginaCeroONegativa_SeTrataComoLaPrimera()
        {
            var pedidas = new List<int>();
            global::BLL.Paginacion.ConsultarAjustando(0, p => { pedidas.Add(p); return Consulta(5, 10, p); });
            global::BLL.Paginacion.ConsultarAjustando(-4, p => { pedidas.Add(p); return Consulta(5, 10, p); });
            CollectionAssert.AreEqual(new[] { 1, 1 }, pedidas);
        }

        [TestMethod]
        public void SinFilas_DevuelveLaPrimeraPaginaVacia_SinReintentar()
        {
            int consultas = 0;
            var r = global::BLL.Paginacion.ConsultarAjustando(1, p => { consultas++; return Consulta(0, 10, p); });

            Assert.AreEqual(0, r.Filas.Count);
            Assert.AreEqual(1, r.TotalPaginas);
            Assert.AreEqual(1, consultas);
        }

        [TestMethod]
        public void SeBorraronFilas_UnaSolaPaginaRestante_DevuelveLaPrimera()
        {
            // Estaba en la página 3, pero ahora quedan 4 filas (una sola página).
            var r = global::BLL.Paginacion.ConsultarAjustando(3, p => Consulta(4, 10, p));
            Assert.AreEqual(1, r.Pagina);
            Assert.AreEqual(4, r.Filas.Count);
        }
    }
}
