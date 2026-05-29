using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Helpers;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    public partial class Calendario : System.Web.UI.Page
    {
        private readonly BLL.EventoBLL _eventoBll = new BLL.EventoBLL();
        private readonly BLL.FeriadosBLL _feriadosBll = new BLL.FeriadosBLL();

        private int Anio
        {
            get => (int)(ViewState["Anio"] ?? DateTime.Today.Year);
            set => ViewState["Anio"] = value;
        }
        private int Mes
        {
            get => (int)(ViewState["Mes"] ?? DateTime.Today.Month);
            set => ViewState["Mes"] = value;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            if (!IsPostBack) RenderizarCalendario();
        }

        protected void btnAnterior_Click(object sender, EventArgs e)
        {
            if (Mes == 1) { Mes = 12; Anio--; } else Mes--;
            RenderizarCalendario();
        }

        protected void btnSiguiente_Click(object sender, EventArgs e)
        {
            if (Mes == 12) { Mes = 1; Anio++; } else Mes++;
            RenderizarCalendario();
        }

        protected void btnHoy_Click(object sender, EventArgs e)
        {
            Anio = DateTime.Today.Year;
            Mes = DateTime.Today.Month;
            RenderizarCalendario();
        }

        private void RenderizarCalendario()
        {
            tblCalendario.Rows.Clear();

            int idUsuario = (Session["Usuario"] as BE.USUARIO).Id;

            // Eventos del mes con mapa de calor
            var eventos = _eventoBll.ListarPorMes(idUsuario, Anio, Mes);

            // Fixing the error by removing the invalid 'Json' parameter
            var feriados = _feriadosBll.ObtenerFeriados(Anio);

            // Título del mes en español
            lblMesAnio.Text = ObtenerNombreMes(Mes) + " " + Anio;

            // Fila encabezado con días en español
            var rowHead = new TableRow();
            string[] dias = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
            foreach (string dia in dias)
            {
                var th = new TableHeaderCell
                {
                    Text = dia,
                    CssClass = "cal-tabla"
                };
                rowHead.Cells.Add(th);
            }
            tblCalendario.Rows.Add(rowHead);

            // Calcular offset (Lunes = 0)
            DateTime primero = new DateTime(Anio, Mes, 1);
            int diasEnMes = DateTime.DaysInMonth(Anio, Mes);
            int offset = ((int)primero.DayOfWeek + 6) % 7;

            TableRow fila = new TableRow();

            // Celdas vacías al inicio
            for (int i = 0; i < offset; i++)
                fila.Cells.Add(CeldaVacia());

            for (int d = 1; d <= diasEnMes; d++)
            {
                DateTime fecha = new DateTime(Anio, Mes, d);
                bool esHoy = fecha.Date == DateTime.Today;
                bool esSabado = fecha.DayOfWeek == DayOfWeek.Saturday;
                bool esDomingo = fecha.DayOfWeek == DayOfWeek.Sunday;

                // Buscar feriado
                var feriado = feriados.FirstOrDefault(f => f.Mes == Mes && f.Dia == d);
                bool esFeriado = feriado != null && feriado.Tipo != "no_laborable";
                bool esNoLaborable = feriado != null && feriado.Tipo == "no_laborable";

                // Eventos del día
                var evsDia = eventos.Where(ev => ev.Fecha.Date == fecha.Date).ToList();

                // CSS de la celda
                string css = "cal-celda";
                if (esHoy) css += " cal-hoy";
                else if (esFeriado) css += " cal-feriado";
                else if (esNoLaborable || esSabado || esDomingo)
                    css += " cal-vacio";

                // Construir HTML de la celda
                string html = $"<div class='cal-numero" +
                              $"{(esSabado || esDomingo ? " text-secondary" : "")}'>{d}</div>";

                // Feriado o no laborable
                if (feriado != null)
                {
                    string cssF = esNoLaborable ? "cal-feriado-nombre text-muted" : "cal-feriado-nombre";
                    string icono = esNoLaborable ? "🚫" : "🎌";
                    html += $"<span class='{cssF}'>{icono} {feriado.Motivo}</span>";
                }

                // Eventos académicos
                foreach (var ev in evsDia)
                {
                    string cssEv = ev.ColorCalor == "Verde" ? "evento-verde"
                                 : ev.ColorCalor == "Amarillo" ? "evento-amarillo"
                                 : "evento-rojo";
                    string icono = ev.Tipo == "Parcial" ? "📝"
                                 : ev.Tipo == "Final" ? "🎓"
                                 : "📋";
                    html += $"<span class='{cssEv}'>{icono} {ev.Tipo}: {ev.NombreMateria}</span>";
                }

                TableCell celda = new TableCell { CssClass = css, Text = html };
                fila.Cells.Add(celda);

                if (fila.Cells.Count == 7)
                {
                    tblCalendario.Rows.Add(fila);
                    fila = new TableRow();
                }
            }

            // Completar última fila
            while (fila.Cells.Count > 0 && fila.Cells.Count < 7)
                fila.Cells.Add(CeldaVacia());
            if (fila.Cells.Count > 0)
                tblCalendario.Rows.Add(fila);
        }

        private TableCell CeldaVacia() =>
            new TableCell { CssClass = "cal-celda cal-vacio" };

        private string ObtenerNombreMes(int mes)
        {
            string[] meses =
            {
                "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
            };
            return meses[mes];
        }
    }
}