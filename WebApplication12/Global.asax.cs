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

            // Siembra los datos base de seguridad (permisos, perfiles, admin y webmaster)
            // y calcula la integridad de lo que nunca la tuvo.
            // Si falla, el error queda en IntegridadBLL.ErrorInicializacionDatosBase
            // (visible en Integridad.aspx) y se reintenta en el próximo login.
            BLL.IntegridadBLL.IntentarInicializarDatosBase();
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