using System;
using System.Text.RegularExpressions;

namespace CAPAS_Web
{
    public partial class CambiarContrasena : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            lblError.Visible = lblExito.Visible = false;
            string actual = txtPassActual.Text.Trim();
            string nueva = txtNuevaPass.Text.Trim();
            string conf = txtConfPass.Text.Trim();

            if (string.IsNullOrEmpty(actual) || string.IsNullOrEmpty(nueva) || string.IsNullOrEmpty(conf))
            { lblError.Text = "Completá todos los campos."; lblError.Visible = true; return; }

            if (nueva != conf)
            { lblError.Text = "Las contraseñas no coinciden."; lblError.Visible = true; return; }

            if (!Regex.IsMatch(nueva, @"^(?=.*[A-Z])(?=.*\d).{6,}$"))
            { lblError.Text = "Debe tener 6+ caracteres, 1 mayúscula y 1 número."; lblError.Visible = true; return; }

            var u = Session["Usuario"] as BE.USUARIO;
            var bll = new BLL.UsuarioBLL();

            if (!bll.VerificarContrasena(u.Usuario, actual))
            { lblError.Text = "La contraseña actual es incorrecta."; lblError.Visible = true; return; }

            bool ok = bll.CambiarContrasena(u.Usuario, nueva);
            if (ok)
            {
                txtPassActual.Text = txtNuevaPass.Text = txtConfPass.Text = "";
                lblExito.Text = "✔ Contraseña cambiada."; lblExito.Visible = true;
            }
            else { lblError.Text = "✖ Error al cambiar."; lblError.Visible = true; }
        }
    }
}