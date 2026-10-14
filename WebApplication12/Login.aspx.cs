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
                lblError.Text = "Completá usuario y contraseña.";
                lblError.Visible = true;
                return;
            }

            var bll = new BLL.LoginBLL();
            BLL.LoginResultado resultado = bll.AutenticarUsuario(usuario, contrasena, Session.SessionID);

            if (resultado == BLL.LoginResultado.Exito)
            {
                BE.USUARIO u = BE.SessionManager.getInstane().getUsuario();

                var integridadBll = new BLL.IntegridadBLL();
                BLL.ResultadoIntegridad resultadoIntegridad = integridadBll.VerificarTodasLasTablas();

                if (!resultadoIntegridad.EsValido && !u.TienePermiso(BE.Permisos.IngresarConIntegridadInvalida))
                {
                    // Usuario sin permiso para operar con la base alterada: fue manipulada externamente.
                    // No se revela el detalle de resultadoIntegridad.Errores (eso es
                    // tarea del panel de Webmaster, no implementada acá).
                    BE.SessionRegistry.Instancia.CerrarSesion(u.Usuario);
                    BE.SessionManager.getInstane().cerrarSesion();
                    Session["Usuario"] = null;
                    pnlInconsistencia.Visible = true;
                    return;
                }

                string inicio = PaginaBase.ObtenerPaginaInicio(u);
                if (inicio == null)
                {
                    // El usuario no tiene ningún permiso que le dé una página de inicio.
                    BE.SessionRegistry.Instancia.CerrarSesion(u.Usuario);
                    BE.SessionManager.getInstane().cerrarSesion();
                    lblError.Text = "Tu cuenta no tiene permisos asignados. Contactá a un administrador.";
                    lblError.Visible = true;
                    txtContrasena.Text = "";
                    return;
                }

                Session["Usuario"] = u;
                BE.SessionManager.getInstane().cerrarSesion();
                Response.Redirect(inicio);
            }
            else if (resultado == BLL.LoginResultado.UsuarioInactivo)
            {
                lblError.Text = "Tu cuenta fue dada de baja. Contactá a un administrador.";
                lblError.Visible = true;
                txtContrasena.Text = "";
            }
            else if (resultado == BLL.LoginResultado.UsuarioBloqueado)
            {
                lblError.Text = "Tu cuenta fue bloqueada por demasiados intentos fallidos. Contactá a un administrador para desbloquearla.";
                lblError.Visible = true;
                txtContrasena.Text = "";
            }
            else if (resultado == BLL.LoginResultado.SesionYaActiva)
            {
                lblError.Text = "Ya hay una sesión activa con este usuario. Cerrá la sesión anterior antes de volver a ingresar.";
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