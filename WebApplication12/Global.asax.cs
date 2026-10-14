using System;
using System.Web;

namespace CAPAS_Web
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            try { DAL.DBInicializador.InicializarSiNoExiste(); }
            catch (Exception ex)
            {
                Application["DBError"] = ex.Message;
            }

            // Tras migrar al modelo de perfiles, las tablas nuevas llegan sin
            // DVH/DVV: se calculan por primera vez (solo si nunca se calcularon).
            // Si falla, el error queda en IntegridadBLL.ErrorInicializacionPerfiles
            // (visible en Integridad.aspx) y se reintenta en el próximo login.
            BLL.IntegridadBLL.IntentarInicializarTablasDePerfiles();
        }

        protected void Session_End(object sender, EventArgs e)
        {
            var u = Session["Usuario"] as BE.USUARIO;
            if (u != null)
            {
                BE.SessionRegistry.Instancia.CerrarSesion(u.Usuario, Session.SessionID);
                new BLL.BitacoraBLL().RegistrarLogout(u.Usuario);
            }
        }
    }
}