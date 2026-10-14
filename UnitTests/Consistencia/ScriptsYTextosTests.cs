using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CAPAS_Web;

namespace UnitTests.Consistencia
{
    /// <summary>
    /// Revisa el contenido del repositorio sin tocar la base: que los scripts SQL tengan el formato que
    /// espera sqlcmd y que cada texto traducible del código tenga su fila en el script de traducciones.
    /// </summary>
    [TestClass]
    public class ScriptsYTextosTests
    {
        private static readonly string[] Scripts =
        {
            @"DAL\BD_Universidad.sql", @"DAL\traducciones.sql", @"DAL\Multiidioma_Actualizacion.sql"
        };

        // Claves (primer valor de cada fila) del script de traducciones: (N'Clave', N'ES', N'EN', N'PT')
        private static HashSet<string> ClavesDelScript()
        {
            string sql = Repositorio.Leer("DAL", "traducciones.sql");
            return new HashSet<string>(
                Regex.Matches(sql, @"^\s*\(N'((?:[^']|'')+)',\s*N'", RegexOptions.Multiline)
                     .Cast<Match>().Select(m => m.Groups[1].Value));
        }

        [TestMethod]
        public void LosScriptsSql_NoLlevanBom_ParaQueSqlcmdConF65001NoLoTomeComoTexto()
        {
            foreach (string s in Scripts)
                Assert.IsFalse(Repositorio.TieneBom(s.Split('\\')), $"{s} empieza con BOM (sqlcmd -f 65001 falla con 'Incorrect syntax near ﻿')");
        }

        [TestMethod]
        public void LosScriptsSql_UsanLaBaseBDUNIVERSIDAD_YTerminanConGo()
        {
            foreach (string s in new[] { @"DAL\traducciones.sql", @"DAL\Multiidioma_Actualizacion.sql" })
            {
                string sql = Repositorio.Leer(s.Split('\\'));
                StringAssert.Contains(sql, "USE BDUNIVERSIDAD");
                Assert.IsTrue(Regex.IsMatch(sql.TrimEnd(), @"^GO\s*$", RegexOptions.Multiline), $"{s} no tiene GO");
            }
        }

        [TestMethod]
        public void ElScriptUnico_ContieneLoMismoQueLosScriptsSeparados()
        {
            string unico = Repositorio.Leer("DAL", "Multiidioma_Actualizacion.sql");
            string traducciones = Repositorio.Leer("DAL", "traducciones.sql");

            // Cada fila de traducciones.sql está en el script único.
            foreach (string clave in ClavesDelScript())
                StringAssert.Contains(unico, $"(N'{clave}', N'", $"falta la clave {clave} en Multiidioma_Actualizacion.sql");

            // Y trae las tablas, la migración de columnas y los procedimientos de idiomas.
            foreach (string necesario in new[]
            {
                "CREATE TABLE IDIOMA", "CREATE TABLE CONTROL_IDIOMA", "CREATE TABLE TRADUCCION",
                "ALTER TABLE USUARIO ADD IDIOMA_ID", "ALTER COLUMN TEXTO_DEFAULT NVARCHAR(500)",
                "CREATE PROCEDURE IDIOMA_LISTAR_HABILITADOS", "CREATE PROCEDURE TRADUCCION_GUARDAR",
                "CREATE PROCEDURE USUARIO_ACTUALIZAR_IDIOMA"
            })
                StringAssert.Contains(unico, necesario);
        }

        [TestMethod]
        public void ElScriptDeTraducciones_TieneLosTresIdiomasYSinClavesRepetidas()
        {
            string sql = Repositorio.Leer("DAL", "traducciones.sql");
            var claves = Regex.Matches(sql, @"^\s*\(N'((?:[^']|'')+)',\s*N'", RegexOptions.Multiline)
                              .Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            Assert.IsTrue(claves.Count > 500, "se esperaban más de 500 claves y hay " + claves.Count);
            Assert.AreEqual(claves.Count, claves.Distinct().Count(), "hay claves repetidas (la variable de tabla tiene PRIMARY KEY)");
            Assert.IsTrue(claves.All(c => c.Length <= 100), "una clave supera VARCHAR(100)");
            StringAssert.Contains(sql, "Ingl%");
            StringAssert.Contains(sql, "Portugu%");
        }

        [TestMethod]
        public void CadaTextoTraducibleDelCodigo_TieneFilaEnElScript()
        {
            var enScript = ClavesDelScript();
            var re = new Regex(@"Textos\.(?:T|TH|THF|Js)\(\s*""([^""]+)""\s*,\s*""", RegexOptions.Compiled);
            var faltan = new List<string>();

            foreach (string archivo in Directory.EnumerateFiles(Repositorio.Ruta("WebApplication12"), "*.*", SearchOption.AllDirectories)
                         .Where(f => (f.EndsWith(".cs") || f.EndsWith(".aspx") || f.EndsWith(".ascx") || f.EndsWith(".Master"))
                                     && !f.Contains(@"\bin\") && !f.Contains(@"\obj\")))
            {
                foreach (Match m in re.Matches(File.ReadAllText(archivo, System.Text.Encoding.UTF8)))
                    if (!enScript.Contains(m.Groups[1].Value))
                        faltan.Add($"{Path.GetFileName(archivo)}: {m.Groups[1].Value}");
            }

            Assert.AreEqual(0, faltan.Count,
                "Faltan en DAL/traducciones.sql (y en Multiidioma_Actualizacion.sql) las claves:\n" + string.Join("\n", faltan.Distinct()));
        }

        [TestMethod]
        public void CadaPlantillaDeMensajes_TieneFilaEnElScript()
        {
            var enScript = ClavesDelScript();
            var campo = typeof(Textos).GetField("Plantillas", BindingFlags.NonPublic | BindingFlags.Static);
            var plantillas = (KeyValuePair<Regex, string>[])campo.GetValue(null);
            var faltan = plantillas.Select(p => Textos.ClaveMensaje(p.Value)).Where(k => !enScript.Contains(k)).ToList();
            Assert.AreEqual(0, faltan.Count, "Plantillas sin traducción en el script: " + string.Join(", ", faltan));
        }

        [TestMethod]
        public void LosLiteralesYBotonesConIdDelMarkup_TienenFilaEnElScript()
        {
            // Texto fijo de las páginas: <asp:Literal ID="x" Text="..."/> y <asp:Button ID="x" Text="..."/>.
            // La clave es "<Clase de la página>.<Id>" y la clase sale del atributo Inherits (o del nombre del archivo).
            var enScript = ClavesDelScript();
            var tags = new Regex(@"<asp:(?:Literal|Button)\b[^>]*?\bID=""([^""]+)""[^>]*?\bText=""([^""]+)""", RegexOptions.Compiled | RegexOptions.Singleline);
            var plantilla = new Regex(@"<(ItemTemplate|HeaderTemplate|FooterTemplate|AlternatingItemTemplate|EditItemTemplate|EmptyDataTemplate)\b.*?</\1>", RegexOptions.Singleline);
            var linkButtons = new Regex(@"<asp:LinkButton\b((?:[^>""']|""[^""]*""|'[^']*')*)>(.*?)</asp:LinkButton\s*>", RegexOptions.Singleline | RegexOptions.Compiled);
            var faltan = new List<string>();

            foreach (string archivo in Directory.EnumerateFiles(Repositorio.Ruta("WebApplication12"), "*.*", SearchOption.TopDirectoryOnly)
                         .Where(f => f.EndsWith(".aspx") || f.EndsWith(".ascx") || f.EndsWith(".Master")))
            {
                string markup = File.ReadAllText(archivo, System.Text.Encoding.UTF8);
                string clase = Regex.Match(markup, @"Inherits=""[^""]*?([A-Za-z0-9_]+)""").Groups[1].Value;
                if (clase.Length == 0) clase = Path.GetFileNameWithoutExtension(archivo);
                string nombre = Path.GetFileName(archivo);

                // Literal y Button: el texto está en el atributo Text. Un Button con data-i18n="manual" lo escribe
                // el código (con Textos.T), y un Text="<%# ... %>" no es texto fijo.
                foreach (Match m in tags.Matches(markup))
                {
                    if (m.Groups[2].Value.Contains("<%") || m.Value.Contains("data-i18n=\"manual\"")) continue;
                    string clave = clase + "." + m.Groups[1].Value;
                    if (!enScript.Contains(clave)) faltan.Add(nombre + ": " + clave);
                }

                // LinkButton: el texto es el contenido (después del ícono) y puede llevar un confirm('...').
                foreach (Match m in linkButtons.Matches(markup))
                {
                    string id = Regex.Match(m.Groups[1].Value, @"\bID=""([^""]+)""").Groups[1].Value;
                    if (id.Length == 0) { faltan.Add(nombre + ": LinkButton sin ID"); continue; }
                    string texto = Regex.Replace(m.Groups[2].Value, @"^\s*<i\b[^>]*>\s*</i>\s*", "");
                    if (!texto.Contains("<") && Regex.IsMatch(texto, @"\p{L}") && !enScript.Contains(clase + "." + id))
                        faltan.Add(nombre + ": " + clase + "." + id);
                    if (Regex.IsMatch(m.Groups[1].Value, @"confirm\(") && !enScript.Contains(clase + "." + id + ".confirmar"))
                        faltan.Add(nombre + ": " + clase + "." + id + ".confirmar");
                }
            }

            Assert.AreEqual(0, faltan.Count,
                "Controles de texto fijo sin fila en traducciones.sql (corré la app y regenerá el script, o agregalos):\n" + string.Join("\n", faltan));
        }
    }
}
