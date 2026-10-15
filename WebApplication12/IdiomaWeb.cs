using System.Web;
using System.Web.SessionState;

namespace CAPAS_Web
{
    internal static class IdiomaWeb
    {
        private const string ClaveSesion = "IdiomaManager";

        internal static BE.IdiomaManager Manager
        {
            get
            {
                HttpSessionState sesion = HttpContext.Current?.Session;
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

        internal static int? IdUsuarioActual =>
            (HttpContext.Current?.Session?["Usuario"] as BE.USUARIO)?.Id;
    }
}
