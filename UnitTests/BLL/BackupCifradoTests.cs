using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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

        private static string CanarioActual() =>
            Convert.ToBase64String(global::BE.AESHelper.Encriptar(global::BLL.IntegridadBLL.TextoControlClave));

        private static string CanarioConOtraClave()
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Enumerable.Repeat((byte)7, 32).ToArray();
                aes.IV = Enumerable.Repeat((byte)9, 16).ToArray();
                byte[] datos = Encoding.UTF8.GetBytes(global::BLL.IntegridadBLL.TextoControlClave);
                return Convert.ToBase64String(aes.CreateEncryptor().TransformFinalBlock(datos, 0, datos.Length));
            }
        }

        private static string Backup(object version, Action<Dictionary<string, object>> ajustar = null, string canario = "actual")
        {
            var tablas = Tablas.ToDictionary(t => t, t => (object)new object[0]);
            ajustar?.Invoke(tablas);
            var backup = new Dictionary<string, object> { { "FechaBackup", "2026-11-19 10:00:00" }, { "Tablas", tablas } };
            if (version != null) backup["Version"] = version;
            if (canario == "actual") backup["ClaveControl"] = CanarioActual();
            else if (canario != null) backup["ClaveControl"] = canario;
            return new JavaScriptSerializer().Serialize(backup);
        }

        private static Dictionary<string, object> Fila(int id, params string[] atributos) =>
            new Dictionary<string, object> { { "Id", id }, { "Atributos", atributos } };

        private static string Cifrado(string texto) => Convert.ToBase64String(global::BE.AESHelper.Encriptar(texto));

        private static string[] UsuarioValido(int id = 1) =>
            new[] { id.ToString(), Cifrado("admin"), Cifrado("hash"), Cifrado("dir"), Cifrado("0"), Cifrado("0"), Cifrado("1") };

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

        [TestMethod]
        public void BackupSinCanarioDeClave_SeRechazaConUnMensajeClaro()
        {
            ArgumentException ex = Rechazo(Backup(global::BLL.IntegridadBLL.VersionBackup, null, null));
            StringAssert.Contains(ex.Message, "clave");
        }

        [TestMethod]
        public void BackupHechoConOtraClaveAes_SeRechaza()
        {
            string json = Backup(global::BLL.IntegridadBLL.VersionBackup, null, CanarioConOtraClave());
            ArgumentException ex = Rechazo(json);
            StringAssert.Contains(ex.Message, "otra clave");
        }

        [TestMethod]
        public void ElBackupExportado_LlevaElCanarioDeLaClaveActual()
        {
            Assert.AreEqual(CanarioActual(), Convert.ToBase64String(global::BE.AESHelper.Encriptar(global::BLL.IntegridadBLL.TextoControlClave)));
            Assert.AreNotEqual(CanarioActual(), CanarioConOtraClave());
        }

        private static void RechazaPorCelda(string tabla, Dictionary<string, object> fila, string fragmento)
        {
            ArgumentException ex = Rechazo(Backup(global::BLL.IntegridadBLL.VersionBackup, t => t[tabla] = new object[] { fila }));
            StringAssert.Contains(ex.Message, tabla);
            StringAssert.Contains(ex.Message, fragmento);
        }

        [TestMethod]
        public void CeldaQueNoEsBase64_SeRechaza()
        {
            string[] a = UsuarioValido(); a[1] = "no es base64!";
            RechazaPorCelda("USUARIO", Fila(1, a), "Base64");
        }

        [TestMethod]
        public void CeldaConLargoQueNoEsDeBloqueAes_SeRechaza()
        {
            string[] a = UsuarioValido(); a[2] = Convert.ToBase64String(new byte[17]);
            RechazaPorCelda("USUARIO", Fila(1, a), "bloque");
        }

        [TestMethod]
        public void CeldaObligatoriaVacia_SeRechaza()
        {
            string[] a = UsuarioValido(); a[1] = "";
            RechazaPorCelda("USUARIO", Fila(1, a), "obligatori");
        }

        [TestMethod]
        public void CeldaMasLargaQueLaColumna_SeRechaza()
        {
            string[] a = UsuarioValido(); a[4] = Convert.ToBase64String(new byte[80]);
            RechazaPorCelda("USUARIO", Fila(1, a), "supera");
        }

        [TestMethod]
        public void IdOClaveForaneaQueNoSonEnteros_SeRechazan()
        {
            RechazaPorCelda("ROL_PERMISO", Fila(1, "1", "x", "3"), "entero");
            RechazaPorCelda("ROL_PERMISO", Fila(1, "abc", "2", "3"), "entero");
            RechazaPorCelda("ROL", Fila(1, "1", Cifrado("a"), "zz", Cifrado("0")), "entero");
        }

        [TestMethod]
        public void IdDeLaFilaQueNoCoincideConElAtributo_SeRechaza()
        {
            RechazaPorCelda("ROL_PERMISO", Fila(5, "6", "2", "3"), "no coincide");
        }

        [TestMethod]
        public void IdsRepetidos_SeRechazan()
        {
            string json = Backup(global::BLL.IntegridadBLL.VersionBackup, t =>
                t["ROL_PERMISO"] = new object[] { Fila(1, "1", "2", "3"), Fila(1, "1", "2", "4") });
            StringAssert.Contains(Rechazo(json).Message, "repetido");
        }

        [TestMethod]
        public void LaSpecDeColumnas_TieneUnaEntradaPorCadaColumnaDeCadaTabla()
        {
            var spec = (System.Collections.IDictionary)typeof(global::BLL.IntegridadBLL)
                .GetField("EspecColumnas", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var columnas = (System.Collections.IDictionary)typeof(global::BLL.IntegridadBLL)
                .GetField("ColumnasDeTabla", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            foreach (string tabla in Tablas)
                Assert.AreEqual(((Array)columnas[tabla]).Length, ((Array)spec[tabla]).Length, tabla);
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
