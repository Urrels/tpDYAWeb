using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    public partial class MisCursadas : System.Web.UI.Page
    {
        private readonly BLL.AlumnoMateriaBLL _bll = new BLL.AlumnoMateriaBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
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

        protected void gvCursadas_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "EditarNotas") return;
            int id = int.Parse(e.CommandArgument.ToString());
            var u = Session["Usuario"] as BE.USUARIO;
            var cur = _bll.Listar(u.Id).FirstOrDefault(x => x.Id == id);
            if (cur == null) return;

            hfIdCursada.Value = cur.Id.ToString();
            hfIdMateriaNotas.Value = cur.IdMateria.ToString();
            ddlEstado.SelectedValue = cur.Estado;
            txtParcial1.Text = cur.NotaParcial1?.ToString() ?? "";
            txtParcial2.Text = cur.NotaParcial2?.ToString() ?? "";
            txtRecuperatorio.Text = cur.NotaRecuperatorio?.ToString() ?? "";
            txtNotaFinal.Text = cur.NotaFinal?.ToString() ?? "";
            txtFechaFinal.Text = cur.FechaFinal?.ToString("yyyy-MM-dd") ?? "";
            txtFechaRecuperatorio.Text = cur.FechaRecuperatorio?.ToString("yyyy-MM-dd") ?? "";

            ActualizarPaneles(cur.AproboAmbosPariales);
            pnlNotas.Visible = true;
        }

        protected void txtParcial_Changed(object sender, EventArgs e) => RefrescarPaneles();
        protected void ddlEstado_Changed(object sender, EventArgs e) => RefrescarPaneles();

        private void RefrescarPaneles()
        {
            decimal p1 = decimal.TryParse(txtParcial1.Text, out decimal v1) ? v1 : 0;
            decimal p2 = decimal.TryParse(txtParcial2.Text, out decimal v2) ? v2 : 0;
            ActualizarPaneles(p1 >= 4 && p2 >= 4);
        }

        private void ActualizarPaneles(bool aproboAmbos)
        {
            pnlRecuperatorio.Visible = !aproboAmbos;
            pnlFinal.Visible = aproboAmbos;
        }

        protected void btnGuardarNotas_Click(object sender, EventArgs e)
        {
            var u = Session["Usuario"] as BE.USUARIO;

            decimal? parcial1 = string.IsNullOrEmpty(txtParcial1.Text) ? (decimal?)null
                              : Math.Min(10, Math.Max(0, decimal.Parse(txtParcial1.Text)));
            decimal? parcial2 = string.IsNullOrEmpty(txtParcial2.Text) ? (decimal?)null
                              : Math.Min(10, Math.Max(0, decimal.Parse(txtParcial2.Text)));
            decimal? recup = string.IsNullOrEmpty(txtRecuperatorio.Text) ? (decimal?)null
                              : Math.Min(10, Math.Max(0, decimal.Parse(txtRecuperatorio.Text)));
            decimal? final_ = string.IsNullOrEmpty(txtNotaFinal.Text) ? (decimal?)null
                              : Math.Min(10, Math.Max(0, decimal.Parse(txtNotaFinal.Text)));

            var am = new BE.ALUMNO_MATERIA
            {
                Id = int.Parse(hfIdCursada.Value),
                IdUsuario = u.Id,
                IdMateria = int.Parse(hfIdMateriaNotas.Value),
                Estado = ddlEstado.SelectedValue,
                NotaParcial1 = parcial1,
                NotaParcial2 = parcial2,
                NotaRecuperatorio = recup,
                NotaFinal = final_,
                FechaFinal = string.IsNullOrEmpty(txtFechaFinal.Text) ? (DateTime?)null
                                   : DateTime.Parse(txtFechaFinal.Text),
                FechaRecuperatorio = string.IsNullOrEmpty(txtFechaRecuperatorio.Text) ? (DateTime?)null
                                   : DateTime.Parse(txtFechaRecuperatorio.Text)
            };

            bool ok = _bll.ActualizarNotas(am, u.Usuario);
            MostrarMsg(ok ? "alert-success" : "alert-danger",
                       ok ? "✔ Notas actualizadas." : "✖ Error.");
            pnlNotas.Visible = false;
            CargarGrilla();
        }

        
        protected void btnCancelarNotas_Click(object sender, EventArgs e) =>
            pnlNotas.Visible = false;

        private void MostrarMsg(string css, string texto)
        {
            lblMsg.CssClass = "alert " + css + " d-block mb-3";
            lblMsg.Text = texto; lblMsg.Visible = true;
        }
    }
}