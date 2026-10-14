using System;
using System.Text.RegularExpressions;

namespace CAPAS_Web
{
    public partial class CambiarContrasena : PaginaBase
    {
        protected override string PermisoRequerido => BE.Permisos.CambiarContrasena;

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            lblError.Visible = lblExito.Visible = false;
            string actual = txtPassActual.Text.Trim();
            string nueva = txtNuevaPass.Text.Trim();
            string conf = txtConfPass.Text.Trim();

            if (string.IsNullOrEmpty(actual) || string.IsNullOrEmpty(nueva) || string.IsNullOrEmpty(conf))
            { lblError.Text = Textos.TH("CambiarContrasena.msgCompletar", "Completá todos los campos."); lblError.Visible = true; return; }

            if (nueva != conf)
            { lblError.Text = Textos.TH("CambiarContrasena.msgNoCoinciden", "Las contraseñas no coinciden."); lblError.Visible = true; return; }

            if (!Regex.IsMatch(nueva, @"^(?=.*[A-Z])(?=.*\d).{6,}$"))
            { lblError.Text = Textos.TH("CambiarContrasena.msgPolitica", "Debe tener 6+ caracteres, 1 mayúscula y 1 número."); lblError.Visible = true; return; }

            var u = Session["Usuario"] as BE.USUARIO;
            var bll = new BLL.UsuarioBLL();

            if (!bll.VerificarContrasena(u.Usuario, actual))
            { lblError.Text = Textos.TH("CambiarContrasena.msgActualIncorrecta", "La contraseña actual es incorrecta."); lblError.Visible = true; return; }

            bool ok = bll.CambiarContrasena(u.Usuario, nueva);
            if (ok)
            {
                txtPassActual.Text = txtNuevaPass.Text = txtConfPass.Text = "";
                lblExito.Text = Textos.TH("CambiarContrasena.msgCambiada", "✔ Contraseña cambiada."); lblExito.Visible = true;
            }
            else { lblError.Text = Textos.TH("CambiarContrasena.msgError", "✖ Error al cambiar."); lblError.Visible = true; }
        }
    }
}