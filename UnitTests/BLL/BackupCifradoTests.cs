using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BLL
{
    [TestClass]
    public class BackupCifradoTests
    {
        private static readonly string[] Tablas =
        {
            "USUARIO", "BITACORA", "MATERIA", "CORRELATIVA", "ALUMNO_MATERIA", "EVENTO_ACADEMICO",
            "PERIODO_ACADEMICO", "INSCRIPCION", "INSCRIPCION_DETALLE", "ROL", "PERMISO", "ROL_PERMISO", "USUARIO_PERFIL"
        };

        private static string Backup(object version, Action<Dictionary<string, object>> ajustar = null)
        {
            var tablas = Tablas.ToDictionary(t => t, t => (object)new object[0]);
            ajustar?.Invoke(tablas);
            var backup = new Dictionary<string, object> { { "FechaBackup", "2026-11-19 10:00:00" }, { "Tablas", tablas } };
            if (version != null) backup["Version"] = version;
            return new JavaScriptSerializer().Serialize(backup);
        }

        private static ArgumentException Rechazo(string json)
        {
            try { new global::BLL.IntegridadBLL().RestaurarDesdeBackupJson(json); }
            catch (ArgumentException ex) { return ex; }
            Assert.Fail("debería haberse rechazado el backup");
            return null;
        }

        [TestMethod]
        public void BackupSinVersion_SeRechazaConUnMensajeClaro()
        {
            ArgumentException ex = Rechazo(Backup(null));
            StringAssert.Contains(ex.Message, "versión");
            StringAssert.Contains(ex.Message, "cifr");
        }

        [TestMethod]
        public void BackupDeOtraVersion_SeRechaza()
        {
            StringAssert.Contains(Rechazo(Backup(1)).Message, "versión");
            StringAssert.Contains(Rechazo(Backup(global::BLL.IntegridadBLL.VersionBackup + 1)).Message, "versión");
        }

        [TestMethod]
        public void BackupConFilasDeLargoIncorrecto_SeRechazaAntesDeTocarLaBase()
        {
            string json = Backup(global::BLL.IntegridadBLL.VersionBackup, t =>
                t["USUARIO"] = new object[] { new Dictionary<string, object> { { "Id", 1 }, { "Atributos", new[] { "1", "x" } } } });
            StringAssert.Contains(Rechazo(json).Message, "USUARIO");
        }

        [TestMethod]
        public void BackupSinUnaTabla_SeRechaza()
        {
            string json = Backup(global::BLL.IntegridadBLL.VersionBackup, t => t.Remove("ROL"));
            StringAssert.Contains(Rechazo(json).Message, "ROL");
        }

        private static string Legible(string tabla, string columna, string valor)
        {
            MethodInfo m = typeof(global::BLL.IntegridadBLL).GetMethod("ValorLegible", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m);
            return (string)m.Invoke(null, new object[] { tabla, columna, valor });
        }

        [TestMethod]
        public void Diagnostico_DescifraLasColumnasCifradasParaQueSeaLegible()
        {
            string cifrado = Convert.ToBase64String(global::BE.AESHelper.Encriptar("admin"));
            Assert.AreEqual("admin", Legible("USUARIO", "USUARIO", cifrado));
            Assert.AreEqual("4", Legible("MATERIA", "PESO", Convert.ToBase64String(global::BE.AESHelper.EncriptarEntero(4))));
            Assert.AreEqual("", Legible("USUARIO", "DIRECCION", ""));
        }

        [TestMethod]
        public void Diagnostico_NoMuestraElHashDeLaContrasena_NiTocaLasColumnasEnClaro()
        {
            string cifrado = Convert.ToBase64String(global::BE.AESHelper.Encriptar("ba7816bf8f01cfea414140de5dae2223"));
            string visto = Legible("USUARIO", "PASS", cifrado);
            Assert.IsFalse(visto.Contains("ba7816bf"));
            Assert.AreEqual("12", Legible("ROL", "PADRE_ID", "12"));
            Assert.AreEqual("no-es-base64!", Legible("USUARIO", "USUARIO", "no-es-base64!"));
        }
    }
}
