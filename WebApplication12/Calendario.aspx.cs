using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    public partial class Calendario : System.Web.UI.Page
    {
        private readonly BLL.EventoBLL   _eventoBll   = new BLL.EventoBLL();
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
            if (!IsPostBack)
            {
                CargarMateriasDesplegable();
                RenderizarCalendario();
                CargarGrillaEventos();
            }
        }

        private void CargarMateriasDesplegable()
        {
            int id = (Session["Usuario"] as BE.USUARIO).Id;
            var cursadas = new BLL.AlumnoMateriaBLL().Listar(id);
            ddlMateriaEvento.DataSource     = cursadas;
            ddlMateriaEvento.DataTextField  = "NombreMateria";
            ddlMateriaEvento.DataValueField = "IdMateria";
            ddlMateriaEvento.DataBind();
        }

        private void CargarGrillaEventos()
        {
            int id = (Session["Usuario"] as BE.USUARIO).Id;
            gvEventos.DataSource = _eventoBll.ListarPorMes(id, Anio, Mes);
            gvEventos.DataBind();
        }

        protected void btnAnterior_Click(object sender, EventArgs e)
        {
            if (Mes == 1) { Mes = 12; Anio--; } else Mes--;
            RenderizarCalendario();
            CargarGrillaEventos();
        }

        protected void btnSiguiente_Click(object sender, EventArgs e)
        {
            if (Mes == 12) { Mes = 1; Anio++; } else Mes++;
            RenderizarCalendario();
            CargarGrillaEventos();
        }

        protected void btnHoy_Click(object sender, EventArgs e)
        {
            Anio = DateTime.Today.Year;
            Mes  = DateTime.Today.Month;
            RenderizarCalendario();
            CargarGrillaEventos();
        }

        protected void btnNuevoEvento_Click(object sender, EventArgs e)
        {
            LimpiarForm();
            pnlFormEvento.Visible = true;
        }

        protected void btnCancelarEvento_Click(object sender, EventArgs e)
        {
            pnlFormEvento.Visible = false;
            lblMsg.Visible        = false;
        }

        protected void btnGuardarEvento_Click(object sender, EventArgs e)
        {
            var u = Session["Usuario"] as BE.USUARIO;

            if (string.IsNullOrEmpty(txtFechaEvento.Text))
            {
                MostrarMsg("alert-danger", "La fecha es obligatoria.");
                return;
            }

            int idEvento = int.Parse(hfIdEvento.Value);
            bool esNuevo = idEvento == 0;

            var ev = new BE.EVENTO_ACADEMICO
            {
                Id            = idEvento,
                IdUsuario     = u.Id,
                IdMateria     = int.Parse(ddlMateriaEvento.SelectedValue),
                NombreMateria = ddlMateriaEvento.SelectedItem.Text,
                Tipo          = ddlTipoEvento.SelectedValue,
                Fecha         = DateTime.Parse(txtFechaEvento.Text),
                Peso          = int.Parse(ddlPesoEvento.SelectedValue),
                Descripcion   = txtDescripcionEvento.Text.Trim()
            };

            bool ok = esNuevo
                ? _eventoBll.Insertar(ev, u.Usuario)
                : _eventoBll.Actualizar(ev, u.Usuario);

            MostrarMsg(ok ? "alert-success" : "alert-danger",
                ok ? (esNuevo ? "✔ Evento creado." : "✔ Evento actualizado.")
                   : "✖ Error al guardar el evento.");

            if (ok)
            {
                pnlFormEvento.Visible = false;
                RenderizarCalendario();
                CargarGrillaEventos();
            }
        }

        protected void gvEventos_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id = int.Parse(e.CommandArgument.ToString());
            var u  = Session["Usuario"] as BE.USUARIO;

            if (e.CommandName == "EliminarEvento")
            {
                bool ok = _eventoBll.Eliminar(id, u.Usuario);
                MostrarMsg(ok ? "alert-success" : "alert-danger",
                    ok ? "✔ Evento eliminado." : "✖ Error al eliminar.");
                RenderizarCalendario();
                CargarGrillaEventos();
                return;
            }

            if (e.CommandName == "EditarEvento")
            {
                var ev = _eventoBll.ListarPorUsuario(u.Id)
                                   .FirstOrDefault(x => x.Id == id);
                if (ev == null) return;

                hfIdEvento.Value          = ev.Id.ToString();
                lblTituloForm.Text        = "Editar Evento";
                txtFechaEvento.Text       = ev.Fecha.ToString("yyyy-MM-ddTHH:mm");
                ddlTipoEvento.SelectedValue  = ev.Tipo;
                ddlPesoEvento.SelectedValue  = ev.Peso.ToString();
                txtDescripcionEvento.Text    = ev.Descripcion;

                if (ddlMateriaEvento.Items.FindByValue(ev.IdMateria.ToString()) != null)
                    ddlMateriaEvento.SelectedValue = ev.IdMateria.ToString();

                pnlFormEvento.Visible = true;
            }
        }

        private void LimpiarForm()
        {
            hfIdEvento.Value             = "0";
            lblTituloForm.Text           = "Nuevo Evento";
            txtFechaEvento.Text          = "";
            txtDescripcionEvento.Text    = "";
            ddlTipoEvento.SelectedIndex  = 0;
            ddlPesoEvento.SelectedValue  = "3";
            if (ddlMateriaEvento.Items.Count > 0)
                ddlMateriaEvento.SelectedIndex = 0;
        }

        private void RenderizarCalendario()
        {
            tblCalendario.Rows.Clear();

            int idUsuario = (Session["Usuario"] as BE.USUARIO).Id;

            var eventos  = _eventoBll.ListarPorMes(idUsuario, Anio, Mes);
            var feriados = _feriadosBll.ObtenerFeriados(Anio);

            lblMesAnio.Text = ObtenerNombreMes(Mes) + " " + Anio;

            var rowHead = new TableRow();
            string[] dias = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
            foreach (string dia in dias)
                rowHead.Cells.Add(new TableHeaderCell { Text = dia });
            tblCalendario.Rows.Add(rowHead);

            DateTime primero  = new DateTime(Anio, Mes, 1);
            int diasEnMes     = DateTime.DaysInMonth(Anio, Mes);
            int offset        = ((int)primero.DayOfWeek + 6) % 7;

            TableRow fila = new TableRow();

            for (int i = 0; i < offset; i++)
                fila.Cells.Add(CeldaVacia());

            for (int d = 1; d <= diasEnMes; d++)
            {
                DateTime fecha    = new DateTime(Anio, Mes, d);
                bool esHoy        = fecha.Date == DateTime.Today;
                bool esSabado     = fecha.DayOfWeek == DayOfWeek.Saturday;
                bool esDomingo    = fecha.DayOfWeek == DayOfWeek.Sunday;

                var feriado       = feriados.FirstOrDefault(f => f.Mes == Mes && f.Dia == d);
                bool esFeriado    = feriado != null && feriado.Tipo != "no_laborable";
                bool esNoLaborable = feriado != null && feriado.Tipo == "no_laborable";

                var evsDia        = eventos.Where(ev => ev.Fecha.Date == fecha.Date).ToList();

                string css = "cal-celda";
                if (esHoy)                              css += " cal-hoy";
                else if (esFeriado)                     css += " cal-feriado";
                else if (esNoLaborable || esSabado || esDomingo) css += " cal-vacio";

                string html = $"<div class='cal-numero{(esSabado || esDomingo ? " text-secondary" : "")}'>{d}</div>";

                if (feriado != null)
                {
                    string cssF  = esNoLaborable ? "cal-feriado-nombre text-muted" : "cal-feriado-nombre";
                    string icono = esNoLaborable ? "🚫" : "🎌";
                    html += $"<span class='{cssF}'>{icono} {feriado.Motivo}</span>";
                }

                foreach (var ev in evsDia)
                {
                    string cssEv = ev.ColorCalor == "Verde"    ? "evento-verde"
                                 : ev.ColorCalor == "Amarillo" ? "evento-amarillo"
                                 : "evento-rojo";
                    string icono = ev.Tipo == "Parcial" ? "📝"
                                 : ev.Tipo == "Final"   ? "🎓"
                                 : "📋";
                    html += $"<span class='{cssEv}'>{icono} <strong>{ev.Fecha:HH:mm}</strong> {ev.Tipo}: {ev.NombreMateria}</span>";
                }

                fila.Cells.Add(new TableCell { CssClass = css, Text = html });

                if (fila.Cells.Count == 7)
                {
                    tblCalendario.Rows.Add(fila);
                    fila = new TableRow();
                }
            }

            while (fila.Cells.Count > 0 && fila.Cells.Count < 7)
                fila.Cells.Add(CeldaVacia());
            if (fila.Cells.Count > 0)
                tblCalendario.Rows.Add(fila);
        }

        private TableCell CeldaVacia() =>
            new TableCell { CssClass = "cal-celda cal-vacio" };

        private string ObtenerNombreMes(int mes)
        {
            string[] meses = { "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
            return meses[mes];
        }

        private void MostrarMsg(string css, string texto)
        {
            lblMsg.CssClass = "alert " + css + " d-block mb-3";
            lblMsg.Text     = texto;
            lblMsg.Visible  = true;
        }
    }
}
