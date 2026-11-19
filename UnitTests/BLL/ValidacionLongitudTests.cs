using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BLL
{
    [TestClass]
    public class ValidacionLongitudTests
    {
        private static string Euros(int n) => new string('€', n);

        private static int Cifrado(string texto) => global::BE.AESHelper.Encriptar(texto).Length;

        private static void Rechaza(Action accion, string fragmento)
        {
            var ex = Assert.ThrowsException<InvalidOperationException>(accion);
            StringAssert.Contains(ex.Message, fragmento);
        }

        [TestMethod]
        public void LosMaximosEnCaracteres_EntranEnLaColumnaVarbinaryAunConTresBytesPorCaracter()
        {
            Assert.IsTrue(Cifrado(Euros(global::BLL.ValidacionLongitud.MaxNombreMateria)) <= 512);
            Assert.IsTrue(Cifrado(Euros(global::BLL.ValidacionLongitud.MaxCodigoMateria)) <= 64);
            Assert.IsTrue(Cifrado(Euros(global::BLL.ValidacionLongitud.MaxModalidadMateria)) <= 64);
            Assert.IsTrue(Cifrado(Euros(global::BLL.ValidacionLongitud.MaxDescripcionEvento)) <= 512);
            Assert.IsTrue(Cifrado(Euros(global::BLL.ValidacionLongitud.MaxDescripcionPeriodo)) <= 512);
            Assert.IsTrue(Cifrado(Euros(100)) <= 512, "nombre de perfil");
            Assert.IsTrue(Cifrado(Euros(50)) <= 256, "usuario");
        }

        [TestMethod]
        public void UnCaracterMasDelMaximo_PuedeNoEntrarEnLaColumna()
        {
            Assert.IsTrue(Cifrado(Euros(global::BLL.ValidacionLongitud.MaxDescripcionEvento + 1)) > 512);
        }

        [TestMethod]
        public void Materia_RechazaNombreCodigoYModalidadLargos_YAceptaElMaximo()
        {
            global::BLL.ValidacionLongitud.Materia(new global::BE.MATERIA
            {
                Nombre = new string('n', global::BLL.ValidacionLongitud.MaxNombreMateria),
                Codigo = new string('c', global::BLL.ValidacionLongitud.MaxCodigoMateria),
                Modalidad = "Presencial"
            });

            Rechaza(() => global::BLL.ValidacionLongitud.Materia(new global::BE.MATERIA
                { Nombre = new string('n', 101), Codigo = "A", Modalidad = "Virtual" }), "nombre");
            Rechaza(() => global::BLL.ValidacionLongitud.Materia(new global::BE.MATERIA
                { Nombre = "N", Codigo = new string('c', 21), Modalidad = "Virtual" }), "código");
            Rechaza(() => global::BLL.ValidacionLongitud.Materia(new global::BE.MATERIA
                { Nombre = "N", Codigo = "A", Modalidad = new string('m', 21) }), "modalidad");
        }

        [TestMethod]
        public void Evento_RechazaDescripcionLarga()
        {
            global::BLL.ValidacionLongitud.Evento(new global::BE.EVENTO_ACADEMICO { Descripcion = null });
            global::BLL.ValidacionLongitud.Evento(new global::BE.EVENTO_ACADEMICO
                { Descripcion = new string('d', global::BLL.ValidacionLongitud.MaxDescripcionEvento) });
            Rechaza(() => global::BLL.ValidacionLongitud.Evento(new global::BE.EVENTO_ACADEMICO
                { Descripcion = new string('d', global::BLL.ValidacionLongitud.MaxDescripcionEvento + 1) }), "descripción del evento");
        }

        [TestMethod]
        public void Periodo_RechazaDescripcionLarga()
        {
            global::BLL.ValidacionLongitud.Periodo(new global::BE.PERIODO_ACADEMICO { Descripcion = "ok" });
            Rechaza(() => global::BLL.ValidacionLongitud.Periodo(new global::BE.PERIODO_ACADEMICO
                { Descripcion = new string('d', global::BLL.ValidacionLongitud.MaxDescripcionPeriodo + 1) }), "descripción del período");
        }

        [TestMethod]
        public void LasBll_ValidanAntesDeTocarLaBase()
        {
            Rechaza(() => new global::BLL.MateriaBLL().Insertar(new global::BE.MATERIA
                { Nombre = new string('n', 500), Codigo = "A", Modalidad = "Virtual" }, "x"), "nombre");
            Rechaza(() => new global::BLL.MateriaBLL().Actualizar(new global::BE.MATERIA
                { Id = 1, Nombre = "N", Codigo = new string('c', 99), Modalidad = "Virtual" }, "x"), "código");
            Rechaza(() => new global::BLL.EventoBLL().Insertar(new global::BE.EVENTO_ACADEMICO
                { Descripcion = new string('d', 999) }, "x"), "descripción del evento");
            Rechaza(() => new global::BLL.EventoBLL().Actualizar(new global::BE.EVENTO_ACADEMICO
                { Id = 1, Descripcion = new string('d', 999) }, "x"), "descripción del evento");
            Rechaza(() => new global::BLL.PeriodoBLL().Insertar(new global::BE.PERIODO_ACADEMICO
                { Descripcion = new string('d', 999) }, "x"), "descripción del período");
            Rechaza(() => new global::BLL.PeriodoBLL().Actualizar(new global::BE.PERIODO_ACADEMICO
                { Id = 1, Descripcion = new string('d', 999) }, "x"), "descripción del período");
        }

        [TestMethod]
        public void LaDireccionDelPerfil_SeValidaEnLaBllAntesDeTocarLaBase()
        {
            Rechaza(() => new global::BLL.UsuarioBLL().ActualizarDireccion("admin", new string('d', 400)), "dirección");
        }

        [TestMethod]
        public void ElPlanDeEstudios_RespetaLosMaximos()
        {
            foreach (var m in global::BLL.SiembraBLL.MateriasDelPlan())
                global::BLL.ValidacionLongitud.Materia(new global::BE.MATERIA { Nombre = m.Nombre, Codigo = m.Codigo, Modalidad = m.Modalidad });
        }
    }
}
