using System;
using System.Linq;

namespace CAPAS_Web
{
    public partial class Menu : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            if ((Session["Usuario"] as BE.USUARIO).EsAdmin) Response.Redirect("~/Materias.aspx");
            if (!IsPostBack)
            {
                var u = Session["Usuario"] as BE.USUARIO;
                lblNombre.Text = u.Usuario;
                CargarPromedio();
                CargarAlertaExamenes();
            }
        }

        private void CargarAlertaExamenes()
        {
            try
            {
                var u = Session["Usuario"] as BE.USUARIO;
                var eventos = new BLL.EventoBLL().ListarPorUsuario(u.Id);
                var proximos = eventos
                    .Where(e => e.Fecha.Date >= DateTime.Today &&
                                e.Fecha.Date <= DateTime.Today.AddDays(3))
                    .OrderBy(e => e.Fecha)
                    .ToList();

                if (proximos.Any())
                {
                    rptExamenes.DataSource = proximos;
                    rptExamenes.DataBind();
                    pnlExamenes.Visible = true;
                }
            }
            catch { }
        }

        private void CargarPromedio()
        {
            var u = Session["Usuario"] as BE.USUARIO;
            var bll = new BLL.AlumnoMateriaBLL();
            var (promedio, mensaje) = bll.CalcularPromedioPonderado(u.Id);

            if (promedio > 0)
            {
                lblPromedio.Text = promedio.ToString("F2");
                lblMensajePromedio.Text = mensaje;

                int porcentaje = (int)(promedio * 10);
                pnlBarra.Style["width"] = porcentaje + "%";
                pnlBarra.CssClass = promedio >= 7 ? "progress-bar bg-success"
                                  : promedio >= 5 ? "progress-bar bg-warning"
                                  : "progress-bar bg-danger";

                decimal diff = 7 - promedio;
                lblDiferenciaObjetivo.Text = diff <= 0
                    ? "✔ Objetivo alcanzado"
                    : $"Te faltan {diff:F2} puntos para llegar a 7";
                lblDiferenciaObjetivo.ForeColor = diff <= 0
                    ? System.Drawing.Color.Green
                    : System.Drawing.Color.OrangeRed;
            }
            else
            {
                lblPromedio.Text = "--";
                lblMensajePromedio.Text = "Sin materias finalizadas todavía.";
                lblDiferenciaObjetivo.Text = "";
            }
        }
    }
}
