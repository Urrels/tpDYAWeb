using System;

namespace CAPAS_Web
{
    public partial class Perfil : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            if (!IsPostBack) CargarPerfil();
        }

        private void CargarPerfil()
        {
            var u = Session["Usuario"] as BE.USUARIO;
            txtUsuario.Text = u.Usuario;
            var perfil = new BLL.UsuarioBLL().ObtenerPerfil(u.Usuario);
            if (perfil != null) txtDireccion.Text = perfil.DireccionVisible ?? "";
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            var u = Session["Usuario"] as BE.USUARIO;
            bool ok = new BLL.UsuarioBLL().ActualizarDireccion(u.Usuario, txtDireccion.Text.Trim());
            lblMsg.CssClass = ok ? "alert alert-success d-block mb-3" : "alert alert-danger d-block mb-3";
            lblMsg.Text = ok ? "✔ Dirección guardada con AES-256." : "✖ Error al guardar.";
            lblMsg.Visible = true;
        }
    }
}