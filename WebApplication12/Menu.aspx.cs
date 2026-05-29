using System;
using System.Linq;
using System.Net.Http;
using System.Web.Script.Serialization;

namespace CAPAS_Web
{
    public partial class Menu : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            if (!IsPostBack)
            {
                var u = Session["Usuario"] as BE.USUARIO;
                lblNombre.Text = u.Usuario;
                CargarClima();
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
        private void CargarClima()
        {
            try
            {
                string url = "https://api.open-meteo.com/v1/forecast" +
                             "?latitude=-34.6037&longitude=-58.3816" +
                             "&current_weather=true&forecast_days=1";

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    string json = client.GetStringAsync(url).Result;
                    var serial = new JavaScriptSerializer();
                    dynamic data = serial.DeserializeObject(json);
                    var w = data["current_weather"];
                    double temp = Convert.ToDouble(w["temperature"]);
                    int cod = Convert.ToInt32(w["weathercode"]);

                    string cond = cod == 0 ? "☀️ Despejado"
                                : cod <= 3 ? "⛅ Nublado"
                                : cod <= 69 ? "🌧️ Lluvia"
                                : "⛈️ Tormenta";

                    lblClima.Text = $"{cond}, {temp}°C en Buenos Aires.";
                    pnlAlerta.Visible = true;
                }
            }
            catch { pnlAlerta.Visible = false; }
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

                // Barra de progreso sobre 10
                int porcentaje = (int)(promedio * 10);
                pnlBarra.Style["width"] = porcentaje + "%";
                pnlBarra.CssClass = promedio >= 7 ? "progress-bar bg-success"
                                  : promedio >= 5 ? "progress-bar bg-warning"
                                  : "progress-bar bg-danger";

                // Diferencia con objetivo 7
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