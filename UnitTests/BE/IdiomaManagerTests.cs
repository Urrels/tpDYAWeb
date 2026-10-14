using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BE
{
    [TestClass]
    public class IdiomaManagerTests
    {
        private class ObservadorFalso : global::BE.IObservadorIdioma
        {
            public int Avisos;
            public global::BE.IdiomaManager Manager;
            public string UltimoTexto;
            public void ActualizarIdioma()
            {
                Avisos++;
                UltimoTexto = Manager?.Traducir("clave");
            }
        }

        private static global::BE.IDIOMA Ingles() => new global::BE.IDIOMA { Id = 2, Nombre = "Inglés", Habilitado = true };

        [TestMethod]
        public void SinIdioma_NoHayIdiomaActivoNiTraducciones()
        {
            var m = new global::BE.IdiomaManager();
            Assert.IsNull(m.IdiomaActivo);
            Assert.IsNull(m.Traducir("cualquiera"));
        }

        [TestMethod]
        public void CambiarIdioma_GuardaElIdiomaYLasTraducciones()
        {
            var m = new global::BE.IdiomaManager();
            m.CambiarIdioma(Ingles(), new Dictionary<string, string> { { "clave", "key" } });

            Assert.AreEqual(2, m.IdiomaActivo.Id);
            Assert.AreEqual("key", m.Traducir("clave"));
            Assert.IsNull(m.Traducir("otra"));
        }

        [TestMethod]
        public void CambiarIdioma_ConTraduccionesNulas_NoRompe()
        {
            var m = new global::BE.IdiomaManager();
            m.CambiarIdioma(Ingles(), null);
            Assert.IsNull(m.Traducir("clave"));
        }

        [TestMethod]
        public void CambiarIdioma_AvisaALosObservadores_YEstosYaVenLasNuevasTraducciones()
        {
            var m = new global::BE.IdiomaManager();
            var o = new ObservadorFalso { Manager = m };
            m.Registrar(o);

            m.CambiarIdioma(Ingles(), new Dictionary<string, string> { { "clave", "key" } });

            Assert.AreEqual(1, o.Avisos);
            Assert.AreEqual("key", o.UltimoTexto);
        }

        [TestMethod]
        public void Registrar_DosVecesElMismoObservador_AvisaUnaSolaVez()
        {
            var m = new global::BE.IdiomaManager();
            var o = new ObservadorFalso();
            m.Registrar(o);
            m.Registrar(o);
            m.CambiarIdioma(Ingles(), null);
            Assert.AreEqual(1, o.Avisos);
        }

        [TestMethod]
        public void Desregistrar_ElObservadorDejaDeRecibirAvisos()
        {
            var m = new global::BE.IdiomaManager();
            var o = new ObservadorFalso();
            m.Registrar(o);
            m.Desregistrar(o);
            m.CambiarIdioma(Ingles(), null);
            Assert.AreEqual(0, o.Avisos);
        }

        [TestMethod]
        public void VariosObservadores_TodosRecibenElAviso()
        {
            var m = new global::BE.IdiomaManager();
            var a = new ObservadorFalso();
            var b = new ObservadorFalso();
            m.Registrar(a);
            m.Registrar(b);
            m.CambiarIdioma(Ingles(), null);
            Assert.AreEqual(1, a.Avisos);
            Assert.AreEqual(1, b.Avisos);
        }

        private class ObservadorQueSeDesregistra : global::BE.IObservadorIdioma
        {
            public global::BE.IdiomaManager Manager;
            public int Avisos;
            public void ActualizarIdioma()
            {
                Avisos++;
                Manager.Desregistrar(this);
            }
        }

        [TestMethod]
        public void UnObservadorPuedeDesregistrarseDentroDelAviso()
        {
            var m = new global::BE.IdiomaManager();
            var o = new ObservadorQueSeDesregistra { Manager = m };
            var otro = new ObservadorFalso();
            m.Registrar(o);
            m.Registrar(otro);

            m.CambiarIdioma(Ingles(), null);   // no debe fallar por modificar la lista mientras se recorre
            m.CambiarIdioma(Ingles(), null);

            Assert.AreEqual(1, o.Avisos);
            Assert.AreEqual(2, otro.Avisos);
        }

        [TestMethod]
        public void CadaManagerEsIndependiente_UnoPorSesion()
        {
            var a = new global::BE.IdiomaManager();
            var b = new global::BE.IdiomaManager();
            a.CambiarIdioma(Ingles(), new Dictionary<string, string> { { "clave", "key" } });
            Assert.IsNull(b.IdiomaActivo);
            Assert.IsNull(b.Traducir("clave"));
        }

        [TestMethod]
        public void Idioma_ToString_EsElNombre()
        {
            Assert.AreEqual("Inglés", Ingles().ToString());
        }
    }
}
