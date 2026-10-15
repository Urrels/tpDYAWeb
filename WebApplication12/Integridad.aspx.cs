using System;
using System.Linq;

namespace CAPAS_Web
{
    public partial class Integridad : PaginaBase
    {
        protected override string PermisoRequerido => BE.Permisos.VerificarIntegridad;

        private readonly BLL.IntegridadBLL _bll = new BLL.IntegridadBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            BLL.IntegridadBLL.IntentarInicializarDatosBase();
            if (!IsPostBack)
                MostrarResultado(_bll.VerificarTodasLasTablas());
        }

        /// <summary>
        /// Acepta el estado actual de los datos como legítimo: recalcula
        /// DVH/DVV (y el snapshot) de las 13 tablas sobre los valores
        /// presentes hoy en la base, y vuelve a mostrar una verificación
        /// (que debería dar OK, ya que lo que se acaba de recalcular es por
        /// definición consistente consigo mismo).
        /// </summary>
        protected void btnRecalcular_Click(object sender, EventArgs e)
        {
            _bll.RecalcularTodasLasTablas();
            // Se registra DESPUÉS del recálculo: RegistrarAccion deja la bitácora con DVH/DVV al día.
            new BLL.BitacoraBLL().RegistrarAccion(UsuarioActual.Usuario, "RECALCULO_INTEGRIDAD");
            MostrarResultado(_bll.VerificarTodasLasTablas());
        }

        /// <summary>
        /// No modifica la base: descarga como .json el snapshot actual de
        /// las 13 tablas (el mismo que usaría Restaurar), para tener un
        /// respaldo externo antes de aplicar una acción irreversible.
        /// </summary>
        protected void btnBackup_Click(object sender, EventArgs e)
        {
            string json = _bll.ExportarBackupJson();
            string nombreArchivo = $"backup_integridad_{DateTime.Now:yyyyMMdd_HHmmss}.json";

            Response.Clear();
            Response.ContentType = "application/json";
            Response.AddHeader("Content-Disposition", $"attachment; filename={nombreArchivo}");
            Response.Write(json);
            Response.End();
        }

        /// <summary>
        /// Acción destructiva/irreversible: devuelve las 13 tablas al último
        /// estado guardado en el snapshot (confirmación ya pedida en el
        /// cliente vía OnClientClick antes de llegar a este postback).
        /// </summary>
        protected void btnRestaurar_Click(object sender, EventArgs e)
        {
            var avisos = _bll.RestaurarTodasLasTablas();
            // El restore puede reescribir la propia bitácora: se registra DESPUÉS, así la
            // entrada no se pierde y el DVH/DVV de BITACORA queda consistente.
            new BLL.BitacoraBLL().RegistrarAccion(UsuarioActual.Usuario, "RESTAURACION_DATOS");
            MostrarResultado(_bll.VerificarTodasLasTablas());
            MostrarAvisos(avisos);
        }

        /// <summary>
        /// Restaura usando el .json subido por el usuario (generado antes con
        /// "Backup") en vez del snapshot interno de la base — cubre el caso
        /// en que el snapshot interno también se perdió o corrompió junto
        /// con los datos.
        /// </summary>
        protected void btnRestaurarDesdeBackup_Click(object sender, EventArgs e)
        {
            if (!fileBackup.HasFile)
            {
                MostrarMsg("alert-warning",
                    "<i class='bi bi-exclamation-triangle me-2'></i>" + Textos.TH("Integridad.msgElegirBackup", "Elegí un archivo de backup (.json) antes de restaurar."));
                return;
            }

            string json;
            using (var reader = new System.IO.StreamReader(fileBackup.FileContent))
                json = reader.ReadToEnd();

            System.Collections.Generic.List<string> avisos;
            try
            {
                avisos = _bll.RestaurarDesdeBackupJson(json);
            }
            catch (Exception ex)
            {
                MostrarMsg("alert-danger",
                    "<i class='bi bi-exclamation-triangle me-2'></i>" + Textos.TH("Integridad.msgErrorBackup", "No se pudo restaurar desde el backup: {0}", Textos.Mensaje(ex.Message)));
                return;
            }

            // Igual que en la restauración desde snapshot: se registra después de restaurar.
            new BLL.BitacoraBLL().RegistrarAccion(UsuarioActual.Usuario, "RESTAURACION_BACKUP");
            MostrarResultado(_bll.VerificarTodasLasTablas());
            MostrarAvisos(avisos);
        }

        /// <summary>
        /// No ejecuta ninguna acción sobre la base de datos — vuelve la
        /// página a un estado neutro ocultando el cartel de resultados.
        /// </summary>
        protected void btnCancelar_Click(object sender, EventArgs e)
        {
            pnlResultados.Visible = false;
            lblMsg.Visible = false;
        }

        private void MostrarResultado(BLL.ResultadoIntegridad resultado)
        {
            string errorInicio = BLL.IntegridadBLL.ErrorInicializacionDatosBase;
            if (errorInicio != null)
            {
                resultado.EsValido = false;
                resultado.Errores.Insert(0, "No se pudieron sembrar los datos base o calcular su integridad inicial: " + errorInicio);
            }

            rptResultados.DataSource = resultado.Errores;
            rptResultados.DataBind();

            lblSinAlumnos.Visible = resultado.Errores.Count == 0;
            pnlResultados.Visible = true;
            pnlAcciones.Visible = !resultado.EsValido;

            if (resultado.EsValido)
                MostrarMsg("alert-success",
                    "<i class='bi bi-shield-check me-2'></i>" + Textos.TH("Integridad.msgOk", "Integridad verificada correctamente en las 13 tablas de negocio."));
            else
            {
                string usuariosAfectados = resultado.IdsUsuariosAfectados.Any()
                    ? " " + Textos.TH("Integridad.msgUsuariosAfectados", "Usuario(s) afectado(s): {0}.", string.Join(", ", resultado.IdsUsuariosAfectados.Distinct()))
                    : "";
                MostrarMsg("alert-danger",
                    "<i class='bi bi-exclamation-triangle me-2'></i>" + Textos.THF("Integridad.msgAlteraciones", "{0} detectada(s).", "<strong>" + Textos.TH("Integridad.alteraciones", "{0} alteración(es)", resultado.Errores.Count) + "</strong>") + usuariosAfectados);
            }
        }

        // Agrega al mensaje actual los avisos del restore (tablas omitidas, borrados evitados).
        private void MostrarAvisos(System.Collections.Generic.List<string> avisos)
        {
            if (avisos == null || avisos.Count == 0) return;
            lblMsg.Text += "<br/><small>" + string.Join("<br/>", avisos.Select(Textos.MH)) + "</small>";
        }

        private void MostrarMsg(string css, string texto)
        {
            lblMsg.CssClass = "alert " + css + " d-block mb-3";
            lblMsg.Text = texto;
            lblMsg.Visible = true;
        }
    }
}