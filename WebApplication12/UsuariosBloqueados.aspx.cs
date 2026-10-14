using System;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    public partial class UsuariosBloqueados : PaginaBase
    {
        protected override string PermisoRequerido => BE.Permisos.DesbloquearUsuarios;

        private readonly BLL.PerfilBLL _bll = new BLL.PerfilBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) CargarGrilla();
        }

        private void CargarGrilla()
        {
            var bloqueados = _bll.ListarBloqueadosVisiblesPor(UsuarioActual);
            rptBloqueados.DataSource = bloqueados;
            rptBloqueados.DataBind();
            lblSinBloqueados.Visible = bloqueados.Count == 0;
        }

        protected void rptBloqueados_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Desbloquear") return;

            string usuarioBloqueado = e.CommandArgument.ToString();

            try
            {
                _bll.DesbloquearUsuario(usuarioBloqueado, UsuarioActual);
                lblMsg.CssClass = "alert alert-success d-block mb-3";
                lblMsg.Text = Textos.TH("UsuariosBloqueados.msgDesbloqueado", "Usuario '{0}' desbloqueado correctamente.", usuarioBloqueado);
            }
            catch (InvalidOperationException ex)
            {
                lblMsg.CssClass = "alert alert-danger d-block mb-3";
                lblMsg.Text = Textos.MH(ex.Message);
            }
            lblMsg.Visible = true;

            CargarGrilla();
        }
    }
}
