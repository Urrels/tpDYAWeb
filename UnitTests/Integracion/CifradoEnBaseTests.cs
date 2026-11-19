using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Integracion
{
    /// <summary>
    /// Verifican contra la base real que lo que se ve abriendo la base en SSMS no es legible. Usan SqlConnection
    /// directo (la aplicación solo accede por procedimientos almacenados) porque justamente miran las tablas.
    /// </summary>
    [TestClass]
    [TestCategory("Integracion")]
    public class CifradoEnBaseTests
    {
        private static readonly string[] TablasDeNegocio =
        {
            "USUARIO", "BITACORA", "MATERIA", "CORRELATIVA", "ALUMNO_MATERIA", "EVENTO_ACADEMICO",
            "PERIODO_ACADEMICO", "INSCRIPCION", "INSCRIPCION_DETALLE", "ROL", "PERMISO", "ROL_PERMISO", "USUARIO_PERFIL"
        };

        private static SqlConnection Abrir()
        {
            Type acceso = typeof(global::DAL.BitacoraDAL).Assembly.GetType("DAL.Acceso");
            string cadena = (string)acceso.GetField("CONNECTION_STRING", BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue();
            var conexion = new SqlConnection(cadena);
            try { conexion.Open(); }
            catch (SqlException ex) { Assert.Inconclusive("No se pudo conectar a la base: " + ex.Message); }
            return conexion;
        }

        private static List<object[]> Consultar(SqlConnection c, string sql)
        {
            var filas = new List<object[]>();
            using (var cmd = new SqlCommand(sql, c))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                {
                    var fila = new object[r.FieldCount];
                    r.GetValues(fila);
                    filas.Add(fila);
                }
            return filas;
        }

        private static bool EsClaveONumeroDeControl(string columna) =>
            columna == "ID" || columna == "DVH" || columna.EndsWith("_ID") || columna.StartsWith("ID_");

        [TestMethod]
        public void LasColumnasDeNegocio_SonVarbinary_SalvoIdsClavesForaneasYDigitosVerificadores()
        {
            using (var c = Abrir())
            {
                string lista = string.Join(",", TablasDeNegocio.Select(t => "'" + t + "'"));
                var columnas = Consultar(c, "SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME IN (" + lista + ")");
                Assert.IsTrue(columnas.Count > 0, "no se encontraron las tablas: ¿se corrió BD_Universidad.sql?");

                var enClaro = columnas
                    .Where(f => !EsClaveONumeroDeControl((string)f[1]) && (string)f[2] != "varbinary")
                    .Select(f => f[0] + "." + f[1] + " (" + f[2] + ")").ToList();
                Assert.AreEqual(0, enClaro.Count, "columnas de negocio sin cifrar: " + string.Join(", ", enClaro));
            }
        }

        [TestMethod]
        public void LosDatosAlmacenados_NoContienenTextoLegible()
        {
            string[] legibles = { "admin", "webmaster", "Administrador", "Webmaster", "Alumno", "Presencial", "Cursando", "LOGIN", "Gestionar" };
            using (var c = Abrir())
            {
                foreach (string tabla in new[] { "USUARIO", "BITACORA", "MATERIA", "ROL", "PERMISO" })
                {
                    foreach (object[] fila in Consultar(c, "SELECT * FROM " + tabla))
                        foreach (byte[] valor in fila.OfType<byte[]>())
                        {
                            string comoTexto = Encoding.UTF8.GetString(valor) + Encoding.Unicode.GetString(valor);
                            foreach (string palabra in legibles)
                                Assert.IsFalse(comoTexto.Contains(palabra), tabla + " contiene '" + palabra + "' legible");
                        }
                }
            }
        }

        [TestMethod]
        public void LosSnapshotsDeIntegridad_GuardanSoloIdsYTextoCifradoEnBase64()
        {
            using (var c = Abrir())
            {
                var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                var snapshots = Consultar(c, "SELECT TABLA, DATOS_JSON FROM SNAPSHOT_INTEGRIDAD");
                if (snapshots.Count == 0) Assert.Inconclusive("no hay snapshots: levantá la aplicación una vez");

                foreach (object[] s in snapshots)
                {
                    string json = (string)s[1];
                    foreach (string palabra in new[] { "admin", "Administrador", "Presencial", "LOGIN" })
                        Assert.IsFalse(json.Contains(palabra), "el snapshot de " + s[0] + " contiene '" + palabra + "' en claro");

                    var filas = serializer.Deserialize<List<Dictionary<string, object>>>(json);
                    foreach (var fila in filas)
                        foreach (string atributo in ((System.Collections.IEnumerable)fila["Atributos"]).Cast<string>())
                        {
                            if (atributo.Length == 0 || atributo.All(char.IsDigit)) continue;
                            byte[] bytes = null;
                            try { bytes = Convert.FromBase64String(atributo); }
                            catch (FormatException) { Assert.Fail(s[0] + ": '" + atributo + "' no es Base64"); }
                            Assert.AreEqual(0, bytes.Length % 16, s[0] + ": '" + atributo + "' no tiene largo de bloque AES");
                        }
                }
            }
        }

        [TestMethod]
        public void ElBackup_TieneVersionYNoTrae_TextoEnClaro()
        {
            Abrir().Dispose();
            string json = new global::BLL.IntegridadBLL().ExportarBackupJson();
            StringAssert.Contains(json, "\"Version\":" + global::BLL.IntegridadBLL.VersionBackup);
            foreach (string palabra in new[] { "admin", "Administrador", "Presencial" })
                Assert.IsFalse(json.Contains(palabra), "el backup contiene '" + palabra + "' en claro");
        }

        [TestMethod]
        [TestCategory("Escritura")]
        public void ElBackup_ExportarYRestaurar_DejaLaIntegridadValida()
        {
            Abrir().Dispose();
            var bll = new global::BLL.IntegridadBLL();
            if (new global::BLL.PerfilBLL().ListarRoles().Count == 0)
                Assert.Inconclusive("la base todavía no fue sembrada: levantá la aplicación una vez");
            if (!bll.VerificarTodasLasTablas().EsValido)
                Assert.Inconclusive("la integridad ya estaba inválida: levantá la aplicación una vez");

            string json = bll.ExportarBackupJson();
            bll.RestaurarDesdeBackupJson(json);

            var resultado = bll.VerificarTodasLasTablas();
            Assert.IsTrue(resultado.EsValido, string.Join("\n", resultado.Errores));
            Assert.AreEqual(Tablas(json), Tablas(bll.ExportarBackupJson()), "el backup restaurado debe ser idéntico al original");
        }

        [TestMethod]
        [TestCategory("Escritura")]
        public void UnBackupConUnaCeldaCorrupta_SeRechazaSinModificarLaBase()
        {
            Abrir().Dispose();
            var bll = new global::BLL.IntegridadBLL();
            if (new global::BLL.PerfilBLL().ListarRoles().Count == 0)
                Assert.Inconclusive("la base todavía no fue sembrada: levantá la aplicación una vez");

            string original = bll.ExportarBackupJson();
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var backup = (Dictionary<string, object>)serializer.DeserializeObject(original);
            var tablas = (Dictionary<string, object>)backup["Tablas"];
            var usuarios = (System.Collections.IList)tablas["USUARIO"];
            var primera = (Dictionary<string, object>)usuarios[usuarios.Count - 1];
            var atributos = (System.Collections.IList)primera["Atributos"];
            atributos[2] = "esto-no-es-base64!";
            string corrupto = serializer.Serialize(backup);

            Assert.ThrowsException<ArgumentException>(() => bll.RestaurarDesdeBackupJson(corrupto));

            Assert.AreEqual(Tablas(original), Tablas(bll.ExportarBackupJson()), "el rechazo no debe tocar ni los snapshots");
            Assert.IsTrue(bll.VerificarTodasLasTablas().EsValido);
        }

        [TestMethod]
        public void LaEspecificacionDeColumnasDelBackup_CoincideConLosTamaniosYNulabilidadDeLaBase()
        {
            var spec = (System.Collections.IDictionary)typeof(global::BLL.IntegridadBLL)
                .GetField("EspecColumnas", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var columnas = (System.Collections.IDictionary)typeof(global::BLL.IntegridadBLL)
                .GetField("ColumnasDeTabla", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            using (var c = Abrir())
            {
                foreach (string tabla in TablasDeNegocio)
                {
                    var nombres = (string[])columnas[tabla];
                    var especs = (string[])spec[tabla];
                    for (int i = 0; i < nombres.Length; i++)
                    {
                        if (!especs[i].StartsWith("c")) continue;
                        bool nulable = especs[i].EndsWith("?");
                        int tamanio = int.Parse(especs[i].Substring(1).TrimEnd('?'));
                        var fila = Consultar(c, "SELECT CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '"
                            + tabla + "' AND COLUMN_NAME = '" + nombres[i] + "'");
                        Assert.AreEqual(1, fila.Count, tabla + "." + nombres[i]);
                        Assert.AreEqual(tamanio, (int)fila[0][0], tabla + "." + nombres[i] + " tamaño");
                        Assert.AreEqual(nulable, (string)fila[0][1] == "YES", tabla + "." + nombres[i] + " nulabilidad");
                    }
                }
            }
        }

        private static string Tablas(string backupJson)
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var backup = (Dictionary<string, object>)serializer.DeserializeObject(backupJson);
            return serializer.Serialize(backup["Tablas"]);
        }
    }
}
