using System;

namespace CAPAS_Web
{
    public partial class Login : PaginaConIdioma
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] != null)
            {
                var u = Session["Usuario"] as BE.USUARIO;
                string inicio = PaginaBase.ObtenerPaginaInicio(u);
                if (inicio != null) Response.Redirect(inicio);
            }
        }

        protected void btnIngresar_Click(object sender, EventArgs e)
        {
            string usuario = txtUsuario.Text.Trim();
            string contrasena = txtContrasena.Text.Trim();

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(contrasena))
            {
                lblError.Text = Textos.TH("Login.msgCompletar", "Completá usuario y contraseña.");
                lblError.Visible = true;
                return;
            }

            var bll = new BLL.LoginBLL();
            BE.USUARIO u;
            BLL.LoginResultado resultado = bll.AutenticarUsuario(usuario, contrasena, Session.SessionID, out u);

            if (resultado == BLL.LoginResultado.Exito)
            {
                var integridadBll = new BLL.IntegridadBLL();
                BLL.ResultadoIntegridad resultadoIntegridad = integridadBll.VerificarTodasLasTablas();

                if (!resultadoIntegridad.EsValido && !u.TienePermiso(BE.Permisos.IngresarConIntegridadInvalida))
                {
                    BE.SessionRegistry.Instancia.CerrarSesion(u.Usuario, Session.SessionID);
                    Session["Usuario"] = null;
                    pnlInconsistencia.Visible = true;
                    return;
                }

                string inicio = PaginaBase.ObtenerPaginaInicio(u);
                if (inicio == null)
                {
                    BE.SessionRegistry.Instancia.CerrarSesion(u.Usuario, Session.SessionID);
                    lblError.Text = Textos.TH("Login.msgSinPermisos", "Tu cuenta no tiene permisos asignados. Contactá a un administrador.");
                    lblError.Visible = true;
                    txtContrasena.Text = "";
                    return;
                }

                Session["Usuario"] = u;
                IdiomaWeb.Fachada.AplicarPreferencia(u.Id);
                Response.Redirect(inicio);
            }
            else if (resultado == BLL.LoginResultado.UsuarioInactivo)
            {
                lblError.Text = Textos.TH("Login.msgInactivo", "Tu cuenta fue dada de baja. Contactá a un administrador.");
                lblError.Visible = true;
                txtContrasena.Text = "";
            }
            else if (resultado == BLL.LoginResultado.UsuarioBloqueado)
            {
                lblError.Text = Textos.TH("Login.msgBloqueado", "Tu cuenta fue bloqueada por demasiados intentos fallidos. Contactá a un administrador para desbloquearla.");
                lblError.Visible = true;
                txtContrasena.Text = "";
            }
            else if (resultado == BLL.LoginResultado.SesionYaActiva)
            {
                lblError.Text = Textos.TH("Login.msgSesionActiva", "Ya hay una sesión activa con este usuario. Cerrá la sesión anterior antes de volver a ingresar.");
                lblError.Visible = true;
                txtContrasena.Text = "";
            }
            else
            {
                lblError.Text = Textos.TH("Login.msgCredenciales", "Usuario o contraseña incorrectos.");
                lblError.Visible = true;
                txtContrasena.Text = "";
            }
        }
    }
}