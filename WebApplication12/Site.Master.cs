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
            lnkInicio.NavigateUrl = u.EsAdmin ? "~/Materias.aspx" : "~/Menu.aspx";
            phNavAdmin.Visible  = u.EsAdmin;
            phNavAlumno.Visible = !u.EsAdmin;
        }

        protected void btnCerrarSesion_Click(object sender, EventArgs e)
        {
            var u = Session["Usuario"] as BE.USUARIO;
            if (u != null)
                new BLL.BitacoraBLL().RegistrarLogout(u.Usuario);
            Session.Abandon();
            Response.Redirect("~/Login.aspx");
        }
    }
}