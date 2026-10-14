using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CAPAS_Web;

namespace UnitTests.Web
{
    /// <summary>Partes de Textos que no consultan la base: las claves y las plantillas de mensajes de la BLL.</summary>
    [TestClass]
    public class TextosTests
    {
        private static KeyValuePair<Regex, string>[] Plantillas()
        {
            FieldInfo f = typeof(Textos).GetField("Plantillas", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(f, "no se encontró Textos.Plantillas (¿cambió el nombre?)");
            return (KeyValuePair<Regex, string>[])f.GetValue(null);
        }

        [TestMethod]
        public void ClaveMensaje_QuitaAcentosEspaciosYSignos()
        {
            Assert.AreEqual("Msg.Elperfilnoexiste", Textos.ClaveMensaje("El perfil no existe."));
            Assert.AreEqual("Msg.Lacontrasenaesobligatoria", Textos.ClaveMensaje("La contraseña es obligatoria."));
        }

        [TestMethod]
        public void ClaveMensaje_EsEstable_YNoSuperaElLargoDeLaColumna()
        {
            string largo = new string('a', 500);
            string clave = Textos.ClaveMensaje(largo);
            Assert.AreEqual(clave, Textos.ClaveMensaje(largo));
            Assert.IsTrue(clave.Length <= 100, "CONTROL_IDIOMA.CLAVE es VARCHAR(100); la clave mide " + clave.Length);
        }

        [TestMethod]
        public void ClaveMensaje_TextoSoloConSignos_NoDaClaveVacia()
        {
            Assert.AreEqual("Msg.vacio", Textos.ClaveMensaje("¿¡!?"));
        }

        [TestMethod]
        public void Plantillas_CadaFormatoCoincideConSuPropiaExpresion()
        {
            // Reemplaza {0}, {1}... por un dato de ejemplo y comprueba que la expresión lo reconoce
            // y captura tantos datos como marcadores tiene el formato.
            foreach (var p in Plantillas())
            {
                int marcadores = Regex.Matches(p.Value, @"\{\d+\}").Count;
                string ejemplo = Regex.Replace(p.Value, @"\{\d+\}", "1");
                Match m = p.Key.Match(ejemplo);
                Assert.IsTrue(m.Success, $"la expresión de '{p.Value}' no reconoce su propio mensaje");
                Assert.AreEqual(marcadores, m.Groups.Count - 1, $"grupos distintos a marcadores en '{p.Value}'");
            }
        }

        [TestMethod]
        public void Plantillas_ReconocenMensajesRealesDeLaBll()
        {
            string[] reales =
            {
                "El perfil Webmaster es del sistema: no se puede eliminar.",
                "No podés inscribirte a 'Algoritmos': tenés correlativas pendientes.",
                "USUARIO: se eliminó el registro 4.",
                "Materia ALG1: no puede ser correlativa de sí misma.",
                "El archivo no es un XML válido (línea 3).",
                "Las correlativas forman un ciclo: A → B → A.",
                "🔴 Parcial 1 desaprobado (3). Necesitás ir al recuperatorio."
            };
            foreach (string mensaje in reales)
                Assert.IsTrue(Plantillas().Any(p => p.Key.IsMatch(mensaje)), $"ninguna plantilla reconoce: {mensaje}");
        }

        [TestMethod]
        public void Plantillas_NoPisanMensajesFijos()
        {
            // Un mensaje sin datos incrustados no debe caer en ninguna plantilla.
            foreach (string fijo in new[] { "El perfil no existe.", "Ya existe un perfil con ese nombre.", "El usuario no existe." })
                Assert.IsFalse(Plantillas().Any(p => p.Key.IsMatch(fijo)), fijo);
        }

        [TestMethod]
        public void Plantillas_SusClavesNoChocanEntreSi()
        {
            var claves = Plantillas().Select(p => Textos.ClaveMensaje(p.Value)).ToList();
            Assert.AreEqual(claves.Count, claves.Distinct().Count());
        }
    }
}
