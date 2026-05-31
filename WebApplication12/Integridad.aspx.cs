using System;

namespace CAPAS_Web
{
    public partial class Integridad : System.Web.UI.Page
    {
        private readonly BLL.AlumnoMateriaBLL _bll = new BLL.AlumnoMateriaBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            if (!(Session["Usuario"] as BE.USUARIO).EsAdmin) Response.Redirect("~/Menu.aspx");
        }

        protected void btnVerificar_Click(object sender, EventArgs e)
        {
            var resultados = _bll.VerificarTodos();

            rptResultados.DataSource = resultados;
            rptResultados.DataBind();

            lblSinAlumnos.Visible = resultados.Count == 0;
            pnlResultados.Visible = true;

            int alertas = resultados.FindAll(r => !r.Ok).Count;
            if (alertas == 0)
                MostrarMsg("alert-success",
                    $"<i class='bi bi-shield-check me-2'></i>Todos los alumnos verificados correctamente ({resultados.Count} en total).");
            else
                MostrarMsg("alert-danger",
                    $"<i class='bi bi-exclamation-triangle me-2'></i><strong>{alertas} alumno(s)</strong> con posible alteración de datos detectada.");
        }

        private void MostrarMsg(string css, string texto)
        {
            lblMsg.CssClass = "alert " + css + " d-block mb-3";
            lblMsg.Text = texto;
            lblMsg.Visible = true;
        }
    }
}
