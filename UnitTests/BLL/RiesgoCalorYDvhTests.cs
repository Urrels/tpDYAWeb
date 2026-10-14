using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BLL
{
    [TestClass]
    public class RiesgoCalorYDvhTests
    {
        // ---------- riesgo académico (método privado de AlumnoMateriaBLL, por reflexión) ----------

        private static global::BE.ALUMNO_MATERIA Calcular(global::BE.ALUMNO_MATERIA am, params global::BE.EVENTO_ACADEMICO[] eventos)
        {
            MethodInfo m = typeof(global::BLL.AlumnoMateriaBLL).GetMethod("CalcularRiesgo", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, "no se encontró CalcularRiesgo (¿cambió el nombre?)");
            m.Invoke(new global::BLL.AlumnoMateriaBLL(), new object[] { am, eventos.ToList() });
            return am;
        }

        private static global::BE.ALUMNO_MATERIA Cursada(decimal? p1 = null, decimal? p2 = null, string estado = "Cursando") =>
            new global::BE.ALUMNO_MATERIA { IdMateria = 7, Estado = estado, NotaParcial1 = p1, NotaParcial2 = p2 };

        private static global::BE.EVENTO_ACADEMICO Examen(int idMateria, int dias) =>
            new global::BE.EVENTO_ACADEMICO { IdMateria = idMateria, Fecha = DateTime.Today.AddDays(dias) };

        [TestMethod]
        public void Riesgo_MateriaAprobada()
        {
            var am = Calcular(Cursada(estado: "Aprobada"));
            Assert.AreEqual("Aprobada", am.NivelRiesgo);
            StringAssert.Contains(am.MensajeRiesgo, "aprobada");
        }

        [TestMethod]
        public void Riesgo_Parcial1Desaprobado_EsAlto()
        {
            var am = Calcular(Cursada(p1: 3));
            Assert.AreEqual("Alto", am.NivelRiesgo);
            StringAssert.Contains(am.MensajeRiesgo, "Parcial 1 desaprobado");
        }

        [TestMethod]
        public void Riesgo_Parcial2Desaprobado_EsAlto()
        {
            var am = Calcular(Cursada(p1: 7, p2: 3.5m));
            Assert.AreEqual("Alto", am.NivelRiesgo);
            StringAssert.Contains(am.MensajeRiesgo, "Parcial 2 desaprobado");
        }

        [TestMethod]
        public void Riesgo_Parcial1ConMargenJusto_EsMedio()
        {
            foreach (decimal nota in new[] { 4m, 5m, 5.9m })
            {
                var am = Calcular(Cursada(p1: nota));
                Assert.AreEqual("Medio", am.NivelRiesgo, $"con {nota}");
                StringAssert.Contains(am.MensajeRiesgo, "margen justo");
            }
        }

        [TestMethod]
        public void Riesgo_Parcial1Aprobado_EsBajo()
        {
            foreach (decimal nota in new[] { 6m, 8m, 10m })
                Assert.AreEqual("Bajo", Calcular(Cursada(p1: nota, p2: 7)).NivelRiesgo, $"con {nota}");
        }

        [TestMethod]
        public void Riesgo_SinNotas_EsMedio()
        {
            var am = Calcular(Cursada());
            Assert.AreEqual("Medio", am.NivelRiesgo);
            StringAssert.Contains(am.MensajeRiesgo, "Sin notas");
        }

        [TestMethod]
        public void Riesgo_SinNotasYExamenEnMenosDeUnaSemana_EsAlto()
        {
            var am = Calcular(Cursada(), Examen(7, 3));
            Assert.AreEqual("Alto", am.NivelRiesgo);
            StringAssert.Contains(am.MensajeRiesgo, "7 días");
        }

        [TestMethod]
        public void Riesgo_ElExamenDeOtraMateriaOMuyLejano_NoCuenta()
        {
            Assert.AreEqual("Medio", Calcular(Cursada(), Examen(99, 3)).NivelRiesgo, "otra materia");
            Assert.AreEqual("Medio", Calcular(Cursada(), Examen(7, 10)).NivelRiesgo, "dentro de 10 días");
            Assert.AreEqual("Medio", Calcular(Cursada(), Examen(7, -2)).NivelRiesgo, "ya pasó");
        }

        [TestMethod]
        public void Riesgo_ElExamenDeHoy_CuentaComoProximo()
        {
            Assert.AreEqual("Alto", Calcular(Cursada(), Examen(7, 0)).NivelRiesgo);
            Assert.AreEqual("Alto", Calcular(Cursada(), Examen(7, 7)).NivelRiesgo);
        }

        // ---------- mapa de calor del calendario ----------

        private static global::BE.EVENTO_ACADEMICO Evento(int dia, int peso) =>
            new global::BE.EVENTO_ACADEMICO { Fecha = new DateTime(2026, 3, dia, 10, 0, 0), Peso = peso };

        [TestMethod]
        public void MapaDeCalor_SegunLaCargaDeLaSemana()
        {
            // Semana del 2 al 8 de marzo: 2+3 = 5 (verde). Semana del 9 al 15: 4+3+3 = 10 (amarillo).
            // Semana del 16 al 22: 5+4+3 = 12 (rojo).
            var verdes = new[] { Evento(3, 2), Evento(4, 3) };
            var amarillos = new[] { Evento(10, 4), Evento(11, 3), Evento(12, 3) };
            var rojos = new[] { Evento(17, 5), Evento(18, 4), Evento(19, 3) };
            var todos = verdes.Concat(amarillos).Concat(rojos).ToList();

            new global::BLL.EventoBLL().AplicarMapaDeCalor(todos);

            Assert.IsTrue(verdes.All(e => e.ColorCalor == "Verde"));
            Assert.IsTrue(amarillos.All(e => e.ColorCalor == "Amarillo"));
            Assert.IsTrue(rojos.All(e => e.ColorCalor == "Rojo"));
        }

        [TestMethod]
        public void MapaDeCalor_SinEventos_NoRompe()
        {
            new global::BLL.EventoBLL().AplicarMapaDeCalor(new List<global::BE.EVENTO_ACADEMICO>());
        }

        // ---------- dígito verificador de las cursadas ----------

        private static global::BE.ALUMNO_MATERIA Cursada10() => new global::BE.ALUMNO_MATERIA
        {
            Id = 1, IdUsuario = 2, IdMateria = 3, Estado = "Cursando"
        };

        [TestMethod]
        public void DvhCursada_CoincideConElCalculoManualDeLosAtributos()
        {
            var am = Cursada10();
            int esperado = global::BE.CalculadorDVH.Calcular(new[] { "1", "2", "3", "Cursando", "", "", "", "", "", "" });
            Assert.AreEqual(esperado, new global::BLL.IntegridadBLL().CalcularDVHAlumnoMateria(am));
        }

        [TestMethod]
        public void DvhCursada_CambiaSiCambiaUnaNota()
        {
            var bll = new global::BLL.IntegridadBLL();
            var a = Cursada10();
            var b = Cursada10();
            b.NotaParcial1 = 8;
            Assert.AreNotEqual(bll.CalcularDVHAlumnoMateria(a), bll.CalcularDVHAlumnoMateria(b));
        }

        [TestMethod]
        public void DvhCursada_NoDependeDeLaCulturaDelEquipo()
        {
            var original = Thread.CurrentThread.CurrentCulture;
            try
            {
                var am = Cursada10();
                am.NotaParcial1 = 7.5m;     // con coma decimal el texto sería "7,5"
                am.FechaFinal = new DateTime(2026, 3, 4, 10, 30, 0);

                Thread.CurrentThread.CurrentCulture = new CultureInfo("es-AR");
                int enEspanol = new global::BLL.IntegridadBLL().CalcularDVHAlumnoMateria(am);
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                int enIngles = new global::BLL.IntegridadBLL().CalcularDVHAlumnoMateria(am);

                Assert.AreEqual(enEspanol, enIngles);
                int esperado = global::BE.CalculadorDVH.Calcular(new[] { "1", "2", "3", "Cursando", "7.5", "", "", "", "20260304103000", "" });
                Assert.AreEqual(esperado, enIngles);
            }
            finally { Thread.CurrentThread.CurrentCulture = original; }
        }

        // ---------- catálogo de permisos (lo que siembra la aplicación) ----------

        [TestMethod]
        public void CatalogoDePermisos_SinRepetidosNiPerfilesYSoloAscii()
        {
            var catalogo = global::BLL.SiembraBLL.CatalogoDePermisos();
            Assert.AreEqual(catalogo.Count, catalogo.Distinct().Count(), "hay permisos repetidos");
            Assert.IsFalse(catalogo.Contains(global::BE.Permisos.PerfilAdministrador), "los perfiles no son permisos");
            Assert.IsFalse(catalogo.Contains(global::BE.Permisos.PerfilWebmaster));
            Assert.IsFalse(catalogo.Contains(global::BE.Permisos.PerfilAlumno));
            foreach (string nombre in catalogo)
                Assert.IsTrue(nombre.All(c => c < 128), $"'{nombre}' lleva caracteres no ASCII (se compara como texto en la base)");
        }

        [TestMethod]
        public void CatalogoDePermisos_IncluyeLosPrivilegiadosYLosNuevos()
        {
            var catalogo = global::BLL.SiembraBLL.CatalogoDePermisos();
            foreach (string p in global::BE.Permisos.Privilegiados)
                CollectionAssert.Contains(catalogo, p);
            CollectionAssert.Contains(catalogo, global::BE.Permisos.GestionarIdiomas);
            CollectionAssert.Contains(catalogo, global::BE.Permisos.GestionarMaterias);
        }
    }
}
