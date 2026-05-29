using System.Web;
using System.Web.SessionState;
using System.Web.UI;

namespace CAPAS_Web.Helpers
{
    /// <summary>
    /// Reemplaza el atributo [SesionActiva] de MVC para Web Forms.
    /// Llamar en Page_Load de cada página protegida.
    /// </summary>
    public static class SesionHelper
    {
        public static BE.USUARIO ObtenerUsuario(HttpSessionState session)
        {
            return session["Usuario"] as BE.USUARIO;
        }

        public static void RequiereLogin(Page page)
        {
            if (page.Session["Usuario"] == null)
                page.Response.Redirect("~/Login.aspx");
        }

        public static string NombreUsuario(HttpSessionState session)
        {
            var u = session["Usuario"] as BE.USUARIO;
            return u?.Usuario ?? string.Empty;
        }

        public static int IdUsuario(HttpSessionState session)
        {
            var u = session["Usuario"] as BE.USUARIO;
            return u?.Id ?? 0;
        }
    }
}
