using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Integracion
{
    /// <summary>
    /// Pruebas contra la base real (solo lectura). Usan la cadena de conexión que tenga compilada DAL/Acceso.cs,
    /// así que andan en cada PC después de correr los scripts SQL y ajustar el nombre de la instancia.
    /// Si no hay base, cada prueba queda "no ejecutada" (Inconclusive) en vez de fallar.
    /// </summary>
    [TestClass]
    [TestCategory("Integracion")]
    public class BaseDeDatosTests
    {
        private static void RequiereBase()
        {
            try { new global::BLL.IdiomaBLL().ListarTodos(); }
            catch (SqlException ex) { Assert.Inconclusive("No se pudo conectar a la base (¿instancia o scripts?): " + ex.Message); }
            catch (InvalidOperationException ex) { Assert.Inconclusive("No se pudo abrir la conexión: " + ex.Message); }
        }

        // ---------- idiomas y traducciones ----------

        [TestMethod]
        public void Idiomas_HayExactamenteUnoPredeterminado_YEstaHabilitado()
        {
            RequiereBase();
            var todos = new global::BLL.IdiomaBLL().ListarTodos();
            Assert.AreEqual(1, todos.Count(i => i.Predeterminado), "debe haber un único idioma predeterminado");
            Assert.IsTrue(todos.Single(i => i.Predeterminado).Habilitado, "el predeterminado debe estar habilitado");
            Assert.IsNotNull(new global::BLL.IdiomaBLL().ObtenerPredeterminado());
        }

        [TestMethod]
        public void Idiomas_ExistenEspanolInglesYPortugues()
        {
            RequiereBase();
            var nombres = new global::BLL.IdiomaBLL().ListarTodos().Select(i => i.Nombre).ToList();
            Assert.IsTrue(nombres.Any(n => n.StartsWith("Espa")), "falta Español");
            Assert.IsTrue(nombres.Any(n => n.StartsWith("Ingl")), "falta Inglés");
            Assert.IsTrue(nombres.Any(n => n.StartsWith("Portugu")), "falta Portugués");
        }

        [TestMethod]
        public void Traducciones_CadaClaveDelScriptTieneTextoEnTodosLosIdiomasBase()
        {
            RequiereBase();
            var claves = new HashSet<string>(
                Regex.Matches(Repositorio.Leer("DAL", "traducciones.sql"), @"^\s*\(N'((?:[^']|'')+)',\s*N'", RegexOptions.Multiline)
                     .Cast<Match>().Select(m => m.Groups[1].Value.Replace("''", "'")));
            var bll = new global::BLL.IdiomaBLL();

            foreach (string prefijo in new[] { "Espa", "Ingl", "Portugu" })
            {
                var idioma = bll.ListarTodos().FirstOrDefault(i => i.Nombre.StartsWith(prefijo));
                Assert.IsNotNull(idioma, "no existe el idioma " + prefijo);
                var vacias = bll.ListarControlesConTraduccion(idioma.Id)
                    .Where(c => claves.Contains(c.Clave) && string.IsNullOrWhiteSpace(c.TextoTraduccion))
                    .Select(c => c.Clave).ToList();
                Assert.AreEqual(0, vacias.Count,
                    $"{idioma.Nombre}: faltan traducciones (corré DAL/Multiidioma_Actualizacion.sql): {string.Join(", ", vacias.Take(10))}");
            }
        }

        [TestMethod]
        public void Fachada_CambiarDeIdioma_CambiaLosTextos()
        {
            RequiereBase();
            var bll = new global::BLL.IdiomaBLL();
            var manager = new global::BE.IdiomaManager();
            var fachada = new global::BLL.FachadaIdioma(manager);

            var es = bll.ListarHabilitados().First(i => i.Nombre.StartsWith("Espa"));
            var en = bll.ListarHabilitados().FirstOrDefault(i => i.Nombre.StartsWith("Ingl"));
            if (en == null) Assert.Inconclusive("el inglés está deshabilitado");

            fachada.Cambiar(es, null);
            Assert.AreEqual("Cerrar sesión", fachada.Traducir("Site.btnCerrarSesion"));
            fachada.Cambiar(en, null);
            Assert.AreEqual("Log out", fachada.Traducir("Site.btnCerrarSesion"));
            Assert.AreEqual(en.Id, fachada.IdiomaActivo.Id);
        }

        [TestMethod]
        public void Fachada_SinIdioma_UsaElPredeterminado()
        {
            RequiereBase();
            var manager = new global::BE.IdiomaManager();
            var fachada = new global::BLL.FachadaIdioma(manager);
            fachada.AplicarPredeterminadoSiHaceFalta();
            Assert.IsNotNull(fachada.IdiomaActivo);
            Assert.IsTrue(fachada.IdiomaActivo.Predeterminado);
        }

        [TestMethod]
        public void Idiomas_NoSePuedeBorrarNiDeshabilitarElPredeterminado()
        {
            RequiereBase();
            var bll = new global::BLL.IdiomaBLL();
            var predeterminado = bll.ObtenerPredeterminado();
            Assert.ThrowsException<InvalidOperationException>(() => bll.Eliminar(predeterminado.Id, "pruebas"));
            Assert.ThrowsException<InvalidOperationException>(() => bll.ActualizarEstado(predeterminado.Id, false, "pruebas"));
        }

        // ---------- materias y correlativas ----------

        [TestMethod]
        public void Materias_TienenCodigoUnicoYValoresValidos()
        {
            RequiereBase();
            var materias = new global::BLL.MateriaBLL().Listar();
            Assert.IsTrue(materias.Count > 0, "no hay materias: ¿se corrió BD_Universidad.sql?");
            Assert.AreEqual(materias.Count, materias.Select(m => m.Codigo.ToLowerInvariant()).Distinct().Count(), "códigos repetidos");
            Assert.IsTrue(materias.All(m => m.Modalidad == "Presencial" || m.Modalidad == "Virtual"), "modalidad inválida");
            Assert.IsTrue(materias.All(m => m.Peso >= 1 && m.Peso <= 5), "peso fuera de 1-5");
        }

        [TestMethod]
        public void Materias_LasCorrelativasNoFormanCiclos()
        {
            RequiereBase();
            var bll = new global::BLL.MateriaBLL();
            var materias = bll.Listar();
            var aristas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in materias)
                foreach (var c in bll.ListarCorrelativas(m.Id))
                    aristas.Add(m.Codigo + "|" + c.Codigo);

            MethodInfo buscar = typeof(global::BLL.MateriaXmlBLL).GetMethod("BuscarCiclo", BindingFlags.NonPublic | BindingFlags.Static);
            string ciclo = (string)buscar.Invoke(null, new object[] { aristas, new List<KeyValuePair<string, string>>() });
            Assert.IsNull(ciclo, "el plan de estudios tiene un ciclo de correlativas: " + ciclo);
        }

        // ---------- seguridad: perfiles y permisos sembrados ----------

        [TestMethod]
        public void Perfiles_ExistenLosTresBaseConSusPermisos()
        {
            RequiereBase();
            var bll = new global::BLL.PerfilBLL();
            var planos = bll.ListarRoles();
            if (!planos.Any(r => r.Nombre == "Administrador"))
                Assert.Inconclusive("la base todavía no fue sembrada: levantá la aplicación una vez");
            // ObtenerRol devuelve cada perfil con su subárbol de hijos y permisos ya cargado.
            var porNombre = planos.ToDictionary(r => r.Nombre, r => bll.ObtenerRol(r.Id), StringComparer.OrdinalIgnoreCase);

            Assert.IsTrue(porNombre["Administrador"].TienePermiso(global::BE.Permisos.GestionarMaterias));
            Assert.IsTrue(porNombre["Administrador"].TienePermiso(global::BE.Permisos.GestionarPerfiles));
            Assert.IsFalse(porNombre["Administrador"].TienePermiso(global::BE.Permisos.VerificarIntegridad),
                "el Administrador no debe poder verificar integridad (es del Webmaster)");
            Assert.IsTrue(porNombre["Webmaster"].TienePermiso(global::BE.Permisos.VerificarIntegridad));
            Assert.IsTrue(porNombre["Webmaster"].TienePermiso(global::BE.Permisos.IngresarConIntegridadInvalida));
            Assert.IsFalse(porNombre["Alumno"].TienePermiso(global::BE.Permisos.GestionarUsuarios));
            Assert.IsTrue(porNombre["Alumno"].TienePermiso(global::BE.Permisos.Inscribirse));
        }

        // ---------- integridad de datos ----------

        [TestMethod]
        public void Integridad_LasTrece_TablasDeNegocioVerifican()
        {
            RequiereBase();
            var resultado = new global::BLL.IntegridadBLL().VerificarTodasLasTablas();
            Assert.IsTrue(resultado.EsValido,
                "Integridad inválida. Si la base es nueva, levantá la aplicación una vez para que calcule los dígitos verificadores. Detalle:\n"
                + string.Join("\n", resultado.Errores));
        }
    }
}
