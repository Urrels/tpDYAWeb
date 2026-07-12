using System;

namespace CAPAS_Web
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] != null)
            {
                var u = Session["Usuario"] as BE.USUARIO;
                Response.Redirect(u.EsWebmaster ? "~/Integridad.aspx" : (u.EsAdmin ? "~/Materias.aspx" : "~/Menu.aspx"));
            }
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
            BLL.LoginResultado resultado = bll.AutenticarUsuario(usuario, contrasena);

            if (resultado == BLL.LoginResultado.Exito)
            {
                BE.USUARIO u = BE.SessionManager.getInstane().getUsuario();

                var integridadBll = new BLL.IntegridadBLL();
                BLL.ResultadoIntegridad resultadoIntegridad = integridadBll.VerificarTodasLasTablas();

                if (!resultadoIntegridad.EsValido && !u.EsAdmin && !u.EsWebmaster)
                {
                    // Usuario común: la base fue manipulada externamente.
                    // No se revela el detalle de resultadoIntegridad.Errores (eso es
                    // tarea del panel de Webmaster, no implementada acá).
                    BE.SessionManager.getInstane().cerrarSesion();
                    Session["Usuario"] = null;
                    pnlInconsistencia.Visible = true;
                    return;
                }

                Session["Usuario"] = u;
                BE.SessionManager.getInstane().cerrarSesion();
                Response.Redirect(u.EsWebmaster ? "~/Integridad.aspx" : (u.EsAdmin ? "~/Materias.aspx" : "~/Menu.aspx"));
            }
            else if (resultado == BLL.LoginResultado.UsuarioBloqueado)
            {
                lblError.Text = "Tu cuenta fue bloqueada por demasiados intentos fallidos. Contactá a un administrador para desbloquearla.";
                lblError.Visible = true;
                txtContrasena.Text = "";
            }
            else // CredencialesInvalidas
            {
                lblError.Text = "Usuario o contraseña incorrectos.";
                lblError.Visible = true;
                txtContrasena.Text = "";
            }
        }
    }
}