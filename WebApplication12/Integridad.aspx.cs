using System;
using System.Linq;

namespace CAPAS_Web
{
    public partial class Integridad : System.Web.UI.Page
    {
        private readonly BLL.IntegridadBLL _bll = new BLL.IntegridadBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            if (!(Session["Usuario"] as BE.USUARIO).EsAdmin) Response.Redirect("~/Menu.aspx");
        }

        protected void btnVerificar_Click(object sender, EventArgs e)
        {
            var resultado = _bll.VerificarTodasLasTablas();

            rptResultados.DataSource = resultado.Errores;
            rptResultados.DataBind();

            lblSinAlumnos.Visible = resultado.Errores.Count == 0;
            pnlResultados.Visible = true;

            if (resultado.EsValido)
                MostrarMsg("alert-success",
                    "<i class='bi bi-shield-check me-2'></i>Integridad verificada correctamente en las 9 tablas de negocio.");
            else
            {
                string usuariosAfectados = resultado.IdsUsuariosAfectados.Any()
                    ? $" Usuario(s) afectado(s): {string.Join(", ", resultado.IdsUsuariosAfectados.Distinct())}."
                    : "";
                MostrarMsg("alert-danger",
                    $"<i class='bi bi-exclamation-triangle me-2'></i><strong>{resultado.Errores.Count} alteración(es)</strong> detectada(s).{usuariosAfectados}");
            }
        }

        private void MostrarMsg(string css, string texto)
        {
            lblMsg.CssClass = "alert " + css + " d-block mb-3";
            lblMsg.Text = texto;
            lblMsg.Visible = true;
        }
    }
}
