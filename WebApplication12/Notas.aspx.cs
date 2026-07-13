using System;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    public partial class Notas : System.Web.UI.Page
    {
        private readonly BLL.AlumnoMateriaBLL _amBll = new BLL.AlumnoMateriaBLL();
        private readonly BLL.UsuarioBLL _usuarioBll = new BLL.UsuarioBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            var u = Session["Usuario"] as BE.USUARIO;
            if (u.EsWebmaster) { Response.Redirect("~/Integridad.aspx"); return; }
            if (!u.EsAdmin) { Response.Redirect("~/Menu.aspx"); return; }
            if (!IsPostBack)
            {
                CargarAlumnos();
                if (ddlAlumno.Items.Count > 0) CargarGrilla();
            }
        }

        private void CargarAlumnos()
        {
            ddlAlumno.DataSource = _usuarioBll.ListarAlumnos();
            ddlAlumno.DataTextField = "Usuario";
            ddlAlumno.DataValueField = "Id";
            ddlAlumno.DataBind();
        }

        protected void ddlAlumno_Changed(object sender, EventArgs e)
        {
            pnlNotas.Visible = false;
            CargarGrilla();
        }

        private void CargarGrilla()
        {
            if (ddlAlumno.Items.Count == 0) { gvCursadas.DataSource = null; gvCursadas.DataBind(); return; }
            int idAlumno = int.Parse(ddlAlumno.SelectedValue);
            gvCursadas.DataSource = _amBll.Listar(idAlumno);
            gvCursadas.DataBind();
        }

        protected void gvCursadas_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "EditarNotas") return;
            int id = int.Parse(e.CommandArgument.ToString());
            int idAlumno = int.Parse(ddlAlumno.SelectedValue);
            var cur = _amBll.Listar(idAlumno).Find(x => x.Id == id);
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
            var admin = Session["Usuario"] as BE.USUARIO;
            int idAlumno = int.Parse(ddlAlumno.SelectedValue);

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
                IdUsuario = idAlumno,
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

            bool ok = _amBll.ActualizarNotas(am, admin.Usuario);
            MostrarMsg(ok ? "alert-success" : "alert-danger",
                       ok ? "✔ Notas actualizadas." : "✖ Error.");
            pnlNotas.Visible = false;
            CargarGrilla();
        }

        protected void btnCancelarNotas_Click(object sender, EventArgs e) => pnlNotas.Visible = false;

        private void MostrarMsg(string css, string texto)
        {
            lblMsg.CssClass = "alert " + css + " d-block mb-3";
            lblMsg.Text = texto;
            lblMsg.Visible = true;
        }
    }
}
