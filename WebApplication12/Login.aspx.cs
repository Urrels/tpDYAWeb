using System;

namespace CAPAS_Web
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] != null)
                Response.Redirect("~/Menu.aspx");
        }

        protected void btnIngresar_Click(object sender, EventArgs e)
        {
            string usuario = txtUsuario.Text.Trim();
            string contrasena = txtContrasena.Text.Trim();

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(contrasena))
            {
                lblError.Text = "Completá usuario y contraseña.";
                lblError.Visible = true;
                return;
            }

            var bll = new BLL.LoginBLL();
            bool ok = bll.AutenticarUsuario(usuario, contrasena);

            if (ok)
            {
                BE.USUARIO u = BE.SessionManager.getInstane().getUsuario();
                Session["Usuario"] = u;
                BE.SessionManager.getInstane().cerrarSesion();
                Response.Redirect("~/Menu.aspx");
            }
            else
            {
                lblError.Text = "Usuario o contraseña incorrectos.";
                lblError.Visible = true;
                txtContrasena.Text = "";
            }
        }
    }
}