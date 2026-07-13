using System;

namespace CAPAS_Web
{
    public partial class MisCursadas : System.Web.UI.Page
    {
        private readonly BLL.AlumnoMateriaBLL _bll = new BLL.AlumnoMateriaBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            if ((Session["Usuario"] as BE.USUARIO).EsWebmaster) { Response.Redirect("~/Integridad.aspx"); return; }
            if ((Session["Usuario"] as BE.USUARIO).EsAdmin) Response.Redirect("~/Materias.aspx");
            if (!IsPostBack) CargarGrilla();
        }

        private void CargarGrilla()
        {
            int id = (Session["Usuario"] as BE.USUARIO).Id;
            var eventos = new BLL.EventoBLL().ListarPorUsuario(id);
            var cursadas = _bll.ListarConRiesgo(id, eventos);
            gvCursadas.DataSource = cursadas;
            gvCursadas.DataBind();
        }
        protected string ObtenerCssRiesgo(string nivel)
        {
            switch (nivel)
            {
                case "Alto": return "badge bg-danger";
                case "Medio": return "badge bg-warning text-dark";
                case "Bajo": return "badge bg-success";
                case "Aprobada": return "badge bg-primary";
                default: return "badge bg-secondary";
            }
        }
    }
}