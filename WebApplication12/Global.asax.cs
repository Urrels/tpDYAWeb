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
        }

        protected void Session_End(object sender, EventArgs e)
        {
        }
    }
}