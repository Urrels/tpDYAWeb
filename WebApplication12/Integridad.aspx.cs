using System;
using System.Linq;

namespace CAPAS_Web
{
    public partial class Integridad : System.Web.UI.Page
    {
        private readonly BLL.IntegridadBLL _bll = new BLL.IntegridadBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            BE.USUARIO u = Session["Usuario"] as BE.USUARIO;
            if (!u.EsWebmaster)
            {
                Response.Redirect(u.EsAdmin ? "~/Materias.aspx" : "~/Menu.aspx");
            }
        }

        protected void btnVerificar_Click(object sender, EventArgs e)
        {
            MostrarResultado(_bll.VerificarTodasLasTablas());
        }

        /// <summary>
        /// Acepta el estado actual de los datos como legítimo: recalcula
        /// DVH/DVV (y el snapshot) de las 9 tablas sobre los valores
        /// presentes hoy en la base, y vuelve a mostrar una verificación
        /// (que debería dar OK, ya que lo que se acaba de recalcular es por
        /// definición consistente consigo mismo).
        /// </summary>
        protected void btnRecalcular_Click(object sender, EventArgs e)
        {
            _bll.RecalcularTodasLasTablas();
            MostrarResultado(_bll.VerificarTodasLasTablas());
        }

        /// <summary>
        /// No modifica la base: descarga como .json el snapshot actual de
        /// las 9 tablas (el mismo que usaría Restaurar), para tener un
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
        /// Acción destructiva/irreversible: devuelve las 9 tablas al último
        /// estado guardado en el snapshot (confirmación ya pedida en el
        /// cliente vía OnClientClick antes de llegar a este postback).
        /// </summary>
        protected void btnRestaurar_Click(object sender, EventArgs e)
        {
            _bll.RestaurarTodasLasTablas();
            MostrarResultado(_bll.VerificarTodasLasTablas());
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
                    "<i class='bi bi-exclamation-triangle me-2'></i>Elegí un archivo de backup (.json) antes de restaurar.");
                return;
            }

            string json;
            using (var reader = new System.IO.StreamReader(fileBackup.FileContent))
                json = reader.ReadToEnd();

            try
            {
                _bll.RestaurarDesdeBackupJson(json);
            }
            catch (Exception ex)
            {
                MostrarMsg("alert-danger",
                    "<i class='bi bi-exclamation-triangle me-2'></i>No se pudo restaurar desde el backup: " + ex.Message);
                return;
            }

            MostrarResultado(_bll.VerificarTodasLasTablas());
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
            rptResultados.DataSource = resultado.Errores;
            rptResultados.DataBind();

            lblSinAlumnos.Visible = resultado.Errores.Count == 0;
            pnlResultados.Visible = true;
            pnlAcciones.Visible = !resultado.EsValido;

            if (resultado.EsValido)
                MostrarMsg("alert-success",
                    "<i class='bi bi-shield-check me-2'></i>Integridad verificada correctamente en las 9 tablas de negocio.");
            else
            {
                string usuariosAfectados = resultado.IdsUsuariosAfectados.Any()
                    ? $" Usuario(s) afectado(s): {string.Join(", ", resultado.IdsUsuariosAfectados.Distinct())}."
                    : "";
                MostrarMsg("alert-danger",
                    $"<i class='bi bi-exclamation-triangle me-2'></i><strong>{resultado.Errores.Count} alteración(es)</strong> detectada(s).{usuariosAfectados}");
            }
        }

        private void MostrarMsg(string css, string texto)
        {
            lblMsg.CssClass = "alert " + css + " d-block mb-3";
            lblMsg.Text = texto;
            lblMsg.Visible = true;
        }
    }
}