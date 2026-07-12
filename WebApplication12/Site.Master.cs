using System;

namespace CAPAS_Web
{
    public partial class Site : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }
            var u = Session["Usuario"] as BE.USUARIO;
            lblUsuarioNav.Text  = u.Usuario;
            lnkInicio.NavigateUrl = u.EsWebmaster ? "~/Integridad.aspx" : (u.EsAdmin ? "~/Materias.aspx" : "~/Menu.aspx");
            phNavAdmin.Visible      = u.EsAdmin;
            phNavAlumno.Visible     = !u.EsAdmin && !u.EsWebmaster;
            phNavWebmaster.Visible  = u.EsWebmaster;
        }

        protected void btnCerrarSesion_Click(object sender, EventArgs e)
        {
            var u = Session["Usuario"] as BE.USUARIO;
            if (u != null)
            {
                // El logueo de logout se centraliza en Global.asax.cs (Session_End),
                // que Session.Abandon() dispara siempre (modo InProc) — cubre tanto
                // este botón como el timeout por inactividad, de forma uniforme.
                BE.SessionRegistry.Instancia.CerrarSesion(u.Usuario);
            }
            Session.Abandon();
            Response.Redirect("~/Login.aspx");
        }
    }
}