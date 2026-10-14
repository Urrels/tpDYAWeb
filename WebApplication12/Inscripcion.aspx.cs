using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using System.Text;

namespace CAPAS_Web
{
    public partial class Inscripcion : PaginaBase
    {
        // La misma página sirve a quien gestiona periodos y a quien se inscribe.
        protected override bool TieneAcceso(BE.USUARIO usuario) =>
            usuario.TienePermiso(BE.Permisos.GestionarPeriodos) || usuario.TienePermiso(BE.Permisos.Inscribirse);

        private readonly BLL.InscripcionBLL _bll    = new BLL.InscripcionBLL();
        private readonly BLL.PeriodoBLL     _perBll = new BLL.PeriodoBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) CargarPagina();
        }

        private void CargarPagina()
        {
            var u = Session["Usuario"] as BE.USUARIO;
            var periodo = _bll.ObtenerPeriodoActual();

            if (u.TienePermiso(BE.Permisos.GestionarPeriodos))
            {
                pnlAdminAcciones.Visible = true;
                CargarListaPeriodos();
                if (periodo == null)
                {
                    pnlSinPeriodo.Visible = true;
                    return;
                }
                MostrarMsg("alert-info",
                    "<i class='bi bi-info-circle me-1'></i>" + Textos.THF("Inscripcion.msgPeriodoActivo",
                        "Período activo: {0}. Los alumnos pueden inscribirse hasta {1}.",
                        Textos.Negrita(periodo.Etiqueta),
                        Textos.Negrita(periodo.FechaFin.HasValue ? periodo.FechaFin.Value.ToString("dd/MM/yyyy") : Textos.T("Inscripcion.fechaNoDefinida", "fecha no definida"))));
                return;
            }

            CargarHistorial(u.Id);

            if (periodo == null)
            {
                pnlSinPeriodo.Visible = true;
                return;
            }

            if (_bll.YaInscripto(u.Id, periodo.Id))
                MostrarResumen(u.Id, periodo);

            MostrarFormulario(u.Id, periodo);
        }

        private void MostrarResumen(int idUsuario, BE.PERIODO_ACADEMICO periodo)
        {
            var historial = _bll.ListarHistorial(idUsuario);
            var inscActual = historial.FirstOrDefault(i => i.IdPeriodo == periodo.Id);

            var sb = new StringBuilder();
            sb.Append($"<div class='alert alert-success'>");
            sb.Append($"<i class='bi bi-check-circle-fill me-2'></i>");
            sb.Append("<strong>" + Textos.TH("Inscripcion.yaInscripto", "Ya estás inscripto en {0}.", periodo.Etiqueta) + "</strong>");
            if (inscActual != null)
            {
                sb.Append(" " + Textos.TH("Inscripcion.registradoEl", "Registrado el {0}.", inscActual.FechaInscripcion.ToString("dd/MM/yyyy")) + "<br/><div class='mt-2 d-flex flex-wrap gap-2'>");
                foreach (var m in inscActual.Materias)
                    sb.Append("<span class='badge bg-primary'>" + System.Web.HttpUtility.HtmlEncode(m.Nombre) + "</span>");
                sb.Append("</div>");
            }
            sb.Append("</div>");

            lblResumen.Text = sb.ToString();
            pnlYaInscripto.Visible = true;
        }

        private void MostrarFormulario(int idUsuario, BE.PERIODO_ACADEMICO periodo)
        {
            lblPeriodoBadge.Text = periodo.Etiqueta;
            var (disponibles, noDisponibles) = _bll.ClasificarMaterias(idUsuario);

            rptMateriasDisponibles.DataSource = disponibles;
            rptMateriasDisponibles.DataBind();

            if (disponibles.Count == 0)
            {
                MostrarMsg("alert-warning", Textos.TH("Inscripcion.msgSinMaterias",
                    "No hay materias disponibles para inscribirte. Revisá si ya estás cursando todo o si te faltan correlativas."));
            }

            if (noDisponibles.Count > 0)
            {
                rptNoDisponibles.DataSource = noDisponibles;
                rptNoDisponibles.DataBind();
                pnlNoDisponibles.Visible = true;
            }

            pnlInscribir.Visible = true;
            ViewState["IdPeriodo"] = periodo.Id;
        }

        private void CargarHistorial(int idUsuario)
        {
            var historial = _bll.ListarHistorial(idUsuario);
            rptHistorial.DataSource = historial;
            rptHistorial.DataBind();
        }

        private void CargarListaPeriodos()
        {
            rptPeriodos.DataSource = _perBll.Listar();
            rptPeriodos.DataBind();
            pnlListaPeriodos.Visible = true;
        }

        protected void rptPeriodos_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "EditarPeriodo") return;
            if (!UsuarioActual.TienePermiso(BE.Permisos.GestionarPeriodos)) return;

            int id = int.Parse(e.CommandArgument.ToString());
            var periodo = _perBll.Listar().FirstOrDefault(p => p.Id == id);
            if (periodo == null) return;

            hfIdPeriodo.Value               = periodo.Id.ToString();
            lblTituloFormPeriodo.Text        = Textos.TH("Inscripcion.tituloEditar", "Editar Período Académico");
            txtAnio.Text                     = periodo.Anio.ToString();
            ddlCuatrimestre.SelectedValue    = periodo.Cuatrimestre.ToString();
            txtDescripcion.Text              = periodo.Descripcion;
            txtFechaInicio.Text              = periodo.FechaInicio.HasValue
                                               ? periodo.FechaInicio.Value.ToString("yyyy-MM-dd") : "";
            txtFechaFin.Text                 = periodo.FechaFin.HasValue
                                               ? periodo.FechaFin.Value.ToString("yyyy-MM-dd") : "";
            btnCrearPeriodo.Text             = Textos.T("Inscripcion.btnGuardarCambios", "Guardar Cambios");
            pnlCrearPeriodo.Visible          = true;
            pnlAdminAcciones.Visible         = false;
        }

        protected void btnMostrarCrear_Click(object sender, EventArgs e)
        {
            if (!UsuarioActual.TienePermiso(BE.Permisos.GestionarPeriodos)) return;
            hfIdPeriodo.Value            = "0";
            lblTituloFormPeriodo.Text    = Textos.TH("Inscripcion.tituloNuevo", "Nuevo Período Académico");
            txtAnio.Text                 = DateTime.Today.Year.ToString();
            txtDescripcion.Text          = "";
            txtFechaInicio.Text          = "";
            txtFechaFin.Text             = "";
            ddlCuatrimestre.SelectedIndex = 0;
            btnCrearPeriodo.Text         = Textos.T("Inscripcion.btnGuardarPeriodo", "Guardar Período");
            pnlCrearPeriodo.Visible      = true;
            pnlSinPeriodo.Visible        = false;
            pnlAdminAcciones.Visible     = false;
        }

        protected void btnCancelarCrear_Click(object sender, EventArgs e)
        {
            pnlCrearPeriodo.Visible  = false;
            pnlAdminAcciones.Visible = true;
            CargarListaPeriodos();
        }

        protected void btnCrearPeriodo_Click(object sender, EventArgs e)
        {
            var u = Session["Usuario"] as BE.USUARIO;
            if (!u.TienePermiso(BE.Permisos.GestionarPeriodos)) return;

            if (!int.TryParse(txtAnio.Text, out int anio) || anio < 2000 || anio > 2100)
            {
                MostrarMsg("alert-danger", "El año ingresado no es válido.");
                return;
            }

            var periodo = new BE.PERIODO_ACADEMICO
            {
                Id           = int.Parse(hfIdPeriodo.Value),
                Anio         = anio,
                Cuatrimestre = int.Parse(ddlCuatrimestre.SelectedValue),
                Descripcion  = txtDescripcion.Text.Trim(),
                FechaInicio  = string.IsNullOrEmpty(txtFechaInicio.Text) ? (DateTime?)null
                               : DateTime.Parse(txtFechaInicio.Text),
                FechaFin     = string.IsNullOrEmpty(txtFechaFin.Text) ? (DateTime?)null
                               : DateTime.Parse(txtFechaFin.Text)
            };

            bool esNuevo = periodo.Id == 0;
            bool ok;
            if (esNuevo)
                ok = _perBll.Insertar(periodo, u.Usuario) > 0;
            else
                ok = _perBll.Actualizar(periodo, u.Usuario);

            if (ok)
            {
                pnlCrearPeriodo.Visible  = false;
                pnlSinPeriodo.Visible    = false;
                pnlAdminAcciones.Visible = true;
                CargarListaPeriodos();
                MostrarMsg("alert-success",
                    esNuevo ? Textos.TH("Inscripcion.msgPeriodoCreado", "✔ Período {0} creado correctamente.", periodo.Etiqueta)
                            : Textos.TH("Inscripcion.msgPeriodoActualizado", "✔ Período {0} actualizado correctamente.", periodo.Etiqueta));
            }
            else
            {
                MostrarMsg("alert-danger", esNuevo ? Textos.TH("Inscripcion.msgErrorCrear", "Error al crear el período.") : Textos.TH("Inscripcion.msgErrorActualizar", "Error al actualizar el período."));
            }
        }

        protected void btnInscribir_Click(object sender, EventArgs e)
        {
            var u = Session["Usuario"] as BE.USUARIO;
            // La página la comparten quien gestiona periodos y quien se inscribe:
            // se revalida en el servidor para que no se pueda forjar el POST.
            if (!u.TienePermiso(BE.Permisos.Inscribirse)) return;
            int idPeriodo = (int)(ViewState["IdPeriodo"] ?? 0);

            var valores = Request.Form.GetValues("materiaId");
            var seleccionadas = valores != null
                ? valores.Select(int.Parse).ToList()
                : new System.Collections.Generic.List<int>();

            var (ok, mensaje, _) = _bll.Inscribir(u.Id, idPeriodo, seleccionadas, u.Usuario);

            MostrarMsg(ok ? "alert-success" : "alert-danger", Textos.MH(mensaje));

            if (ok)
            {
                pnlInscribir.Visible = false;
                var periodo = _bll.ObtenerPeriodoActual();
                if (periodo != null) MostrarResumen(u.Id, periodo);
                CargarHistorial(u.Id);
            }
        }

        protected string ObtenerNombresCorrelativas(List<BE.MATERIA> correlativas)
        {
            if (correlativas == null || correlativas.Count == 0) return "—";
            return string.Join(", ", correlativas.Select(c => c.Nombre));
        }

        protected string RenderBadgesMaterias(List<BE.MATERIA> materias)
        {
            if (materias == null || materias.Count == 0)
                return "<span class='text-muted small'>" + Textos.TH("Inscripcion.sinMaterias", "Sin materias registradas") + "</span>";
            return string.Join("",
                materias.Select(m =>
                    "<span class='badge bg-secondary me-1'>" + System.Web.HttpUtility.HtmlEncode(m.Nombre) + "</span>"));
        }

        private void MostrarMsg(string css, string texto)
        {
            lblMsg.CssClass = "alert " + css + " d-block mb-3";
            lblMsg.Text = texto;
            lblMsg.Visible = true;
        }
    }
}
