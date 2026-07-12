using System;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    public partial class UsuariosBloqueados : System.Web.UI.Page
    {
        private readonly BLL.UsuarioBLL _bll = new BLL.UsuarioBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            var u = Session["Usuario"] as BE.USUARIO;
            if (!u.EsAdmin) Response.Redirect("~/Menu.aspx");
            if (!IsPostBack) CargarGrilla();
        }

        private void CargarGrilla()
        {
            var bloqueados = _bll.ListarBloqueados();
            rptBloqueados.DataSource = bloqueados;
            rptBloqueados.DataBind();
            lblSinBloqueados.Visible = bloqueados.Count == 0;
        }

        protected void rptBloqueados_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Desbloquear") return;

            string usuarioBloqueado = e.CommandArgument.ToString();
            string admin = (Session["Usuario"] as BE.USUARIO).Usuario;

            _bll.Desbloquear(usuarioBloqueado, admin);

            lblMsg.CssClass = "alert alert-success d-block mb-3";
            lblMsg.Text = $"Usuario '{usuarioBloqueado}' desbloqueado correctamente.";
            lblMsg.Visible = true;

            CargarGrilla();
        }
    }
}
