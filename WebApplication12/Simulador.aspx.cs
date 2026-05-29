using System;
using System.Linq;

namespace CAPAS_Web
{
    public partial class Simulador : System.Web.UI.Page
    {
        private readonly BLL.AlumnoMateriaBLL _bll = new BLL.AlumnoMateriaBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            if (!IsPostBack) CargarMaterias();
        }

        private void CargarMaterias()
        {
            int id = (Session["Usuario"] as BE.USUARIO).Id;
            var cursadas = _bll.Listar(id)
                .Where(am => am.Estado != "Aprobada")
                .ToList();

            ddlMateria.DataSource = cursadas;
            ddlMateria.DataTextField = "NombreMateria";
            ddlMateria.DataValueField = "Id";
            ddlMateria.DataBind();
        }

        protected void btnSimular_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtNotaHipotetica.Text)) return;

            int id = (Session["Usuario"] as BE.USUARIO).Id;
            decimal notaNueva = Math.Min(10, Math.Max(0, decimal.Parse(txtNotaHipotetica.Text)));
            int idCursada = int.Parse(ddlMateria.SelectedValue);

            var todasLasCursadas = _bll.Listar(id);

            // Promedio actual (solo materias con nota final)
            var conFinal = todasLasCursadas.Where(am => am.NotaFinal.HasValue).ToList();
            decimal promedioActual = conFinal.Any()
                ? conFinal.Sum(am => (am.NotaFinal ?? 0) * am.PesoMateria) /
                  conFinal.Sum(am => am.PesoMateria)
                : 0;

            // Simulación: reemplazar la nota de la materia seleccionada
            var seleccionada = todasLasCursadas.FirstOrDefault(am => am.Id == idCursada);
            if (seleccionada == null) return;

            // Clonar lista para simular sin modificar los datos reales
            var simuladas = todasLasCursadas
                .Where(am => am.NotaFinal.HasValue || am.Id == idCursada)
                .Select(am => new
                {
                    Nota = am.Id == idCursada ? notaNueva : (am.NotaFinal ?? 0),
                    Peso = am.PesoMateria
                }).ToList();

            decimal promedioSimulado = simuladas.Any()
                ? simuladas.Sum(s => s.Nota * s.Peso) / simuladas.Sum(s => s.Peso)
                : 0;

            decimal diferencia = promedioSimulado - promedioActual;

            lblPromedioActual.Text = promedioActual > 0 ? promedioActual.ToString("F2") : "--";
            lblPromedioSimulado.Text = promedioSimulado.ToString("F2");

            string signo = diferencia >= 0 ? "+" : "";
            lblMensajeSimulacion.Text = diferencia > 0
                ? $"✔ Si sacás {notaNueva} en {seleccionada.NombreMateria}, " +
                  $"tu promedio sube {signo}{diferencia:F2} puntos → quedás en {promedioSimulado:F2}"
                : $"⚠️ Con {notaNueva} en {seleccionada.NombreMateria}, " +
                  $"tu promedio quedaría en {promedioSimulado:F2}";

            pnlResultado.Visible = true;
        }
    }
}