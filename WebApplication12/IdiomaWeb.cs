using System.Web;
using System.Web.SessionState;

namespace CAPAS_Web
{
    /// <summary>
    /// Punto de acceso al multiidioma desde la capa web: guarda un IdiomaManager (el Subject del
    /// Observer) por sesión y arma la fachada sobre él.
    /// </summary>
    internal static class IdiomaWeb
    {
        private const string ClaveSesion = "IdiomaManager";

        internal static BE.IdiomaManager Manager
        {
            get
            {
                HttpSessionState sesion = HttpContext.Current?.Session;
                // Sin sesión (p. ej. un servicio sin EnableSession) no hay idioma: se devuelve un
                // manager vacío y los textos salen con su valor por defecto.
                if (sesion == null) return new BE.IdiomaManager();
                var manager = sesion[ClaveSesion] as BE.IdiomaManager;
                if (manager == null)
                {
                    manager = new BE.IdiomaManager();
                    sesion[ClaveSesion] = manager;
                }
                return manager;
            }
        }

        internal static BLL.FachadaIdioma Fachada => new BLL.FachadaIdioma(Manager);

        /// <summary>Id del usuario con sesión iniciada, o null si todavía no ingresó.</summary>
        internal static int? IdUsuarioActual =>
            (HttpContext.Current?.Session?["Usuario"] as BE.USUARIO)?.Id;
    }
}
