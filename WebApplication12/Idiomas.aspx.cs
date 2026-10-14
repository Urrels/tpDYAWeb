using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    // Gestión de idiomas y de sus traducciones (equivale a frmIdiomas de la versión de escritorio).
    public partial class Idiomas : PaginaBase
    {
        protected override string PermisoRequerido => BE.Permisos.GestionarIdiomas;

        private readonly BLL.IdiomaBLL _bll = new BLL.IdiomaBLL();

        private int IdiomaSeleccionado
        {
            get => ViewState["IdiomaSel"] is int id ? id : 0;
            set => ViewState["IdiomaSel"] = value;
        }

        private string Usuario => (Session["Usuario"] as BE.USUARIO).Usuario;

        // Las grillas se enlazan al final, con el estado ya modificado por el evento y el idioma ya cambiado,
        // para que los textos que arma el código salgan en el idioma vigente.
        protected override void OnPreRender(EventArgs e)
        {
            Cargar();
            base.OnPreRender(e);
        }

        private void Cargar()
        {
            gvIdiomas.DataSource = _bll.ListarTodos();
            gvIdiomas.DataBind();

            BE.IDIOMA seleccionado = IdiomaSeleccionado == 0 ? null : _bll.ObtenerPorId(IdiomaSeleccionado);
            pnlTraducciones.Visible = seleccionado != null;
            if (seleccionado == null) { IdiomaSeleccionado = 0; return; }

            lblIdiomaSeleccionado.Text = System.Web.HttpUtility.HtmlEncode(seleccionado.Nombre);
            string filtro = txtFiltro.Text.Trim();
            List<BE.CONTROL_IDIOMA> controles = _bll.ListarControlesConTraduccion(seleccionado.Id)
                .Where(c => filtro.Length == 0 || c.Clave.IndexOf(filtro, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            gvTraducciones.DataSource = controles;
            gvTraducciones.DataBind();
        }

        protected override void ActualizarTextosDinamicos()
        {
            Encabezados(gvIdiomas, "Idiomas.colIdioma", "Idioma", "Idiomas.colHabilitado", "Habilitado",
                "Idiomas.colPredeterminado", "Predeterminado", "Idiomas.colAcciones", "Acciones");
            Encabezados(gvTraducciones, "Idiomas.colClave", "Clave", "Idiomas.colTextoDefault", "Texto por defecto",
                "Idiomas.colTraduccion", "Traducción");
        }

        private static void Encabezados(GridView grilla, params string[] clavesYTextos)
        {
            if (grilla.HeaderRow == null) return;
            for (int i = 0; i * 2 < clavesYTextos.Length && i < grilla.HeaderRow.Cells.Count; i++)
                grilla.HeaderRow.Cells[i].Text = Textos.T(clavesYTextos[i * 2], clavesYTextos[i * 2 + 1]);
        }

        protected void gvIdiomas_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var idioma = (BE.IDIOMA)e.Row.DataItem;
            e.Row.Cells[1].Text = idioma.Habilitado ? Textos.TH("Idiomas.si", "Sí") : Textos.TH("Idiomas.no", "No");
            e.Row.Cells[2].Text = idioma.Predeterminado ? Textos.TH("Idiomas.si", "Sí") : Textos.TH("Idiomas.no", "No");
            ((LinkButton)e.Row.FindControl("lnkSeleccionar")).Text = Textos.TH("Idiomas.btnTraducciones", "Ver traducciones");
            ((LinkButton)e.Row.FindControl("lnkAlternar")).Text = Textos.TH("Idiomas.btnAlternar", "Habilitar/Deshabilitar");
            var eliminar = (LinkButton)e.Row.FindControl("lnkEliminar");
            eliminar.Text = Textos.TH("Idiomas.btnEliminar", "Eliminar");
            eliminar.OnClientClick = "return confirm('" +
                System.Web.HttpUtility.JavaScriptStringEncode(Textos.T("Idiomas.confirmarEliminar",
                    "¿Eliminar el idioma? Se borrarán todas sus traducciones.")) + "')";
        }

        protected void gvIdiomas_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out int id)) return;
            try
            {
                if (e.CommandName == "Seleccionar")
                {
                    IdiomaSeleccionado = id;
                    txtFiltro.Text = "";
                }
                else if (e.CommandName == "Alternar")
                {
                    BE.IDIOMA idioma = _bll.ObtenerPorId(id);
                    if (idioma == null) throw new InvalidOperationException("El idioma no existe.");
                    _bll.ActualizarEstado(id, !idioma.Habilitado, Usuario);
                    Exito(Textos.TH("Idiomas.msgEstado", "Estado actualizado."));
                }
                else if (e.CommandName == "Eliminar")
                {
                    _bll.Eliminar(id, Usuario);
                    if (IdiomaSeleccionado == id) IdiomaSeleccionado = 0;
                    Exito(Textos.TH("Idiomas.msgEliminado", "Idioma eliminado."));
                }
            }
            catch (InvalidOperationException ex)
            {
                // Los mensajes de la BLL son reglas de negocio en español.
                MostrarError(System.Web.HttpUtility.HtmlEncode(ex.Message));
            }
        }

        protected void btnAgregar_Click(object sender, EventArgs e)
        {
            try
            {
                _bll.Crear(txtNombre.Text, false, Usuario);
                txtNombre.Text = "";
                Exito(Textos.TH("Idiomas.msgCreado", "Idioma agregado. Está deshabilitado hasta que lo habilites."));
            }
            catch (InvalidOperationException ex) { MostrarError(System.Web.HttpUtility.HtmlEncode(ex.Message)); }
        }

        protected void btnRenombrar_Click(object sender, EventArgs e)
        {
            if (IdiomaSeleccionado == 0)
            {
                MostrarError(Textos.TH("Idiomas.msgSeleccionar", "Seleccioná un idioma."));
                return;
            }
            try
            {
                _bll.Renombrar(IdiomaSeleccionado, txtNombre.Text, Usuario);
                txtNombre.Text = "";
                Exito(Textos.TH("Idiomas.msgRenombrado", "Idioma renombrado."));
            }
            catch (InvalidOperationException ex) { MostrarError(System.Web.HttpUtility.HtmlEncode(ex.Message)); }
        }

        protected void btnFiltrar_Click(object sender, EventArgs e) { }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (IdiomaSeleccionado == 0) return;
            var textos = new Dictionary<int, string>();
            foreach (GridViewRow fila in gvTraducciones.Rows)
            {
                int controlId = (int)gvTraducciones.DataKeys[fila.RowIndex].Value;
                textos[controlId] = ((TextBox)fila.FindControl("txtTraduccion")).Text;
            }
            try
            {
                int guardadas = _bll.GuardarTraducciones(IdiomaSeleccionado, textos, Usuario);
                // Si se editó el idioma que se está usando, se aplica enseguida.
                IdiomaWeb.Fachada.RefrescarSiHaceFalta();
                Exito(Textos.TH("Idiomas.msgGuardadas", "Traducciones guardadas: {0}.", guardadas));
            }
            catch (InvalidOperationException ex) { MostrarError(System.Web.HttpUtility.HtmlEncode(ex.Message)); }
        }

        // Estos métodos reciben texto ya codificado para HTML (lblMsg es un Label, que no codifica).
        private void Exito(string html) => Mostrar("alert-success", html);
        private void MostrarError(string html) => Mostrar("alert-danger", html);

        private void Mostrar(string css, string html)
        {
            lblMsg.CssClass = "alert " + css + " d-block mb-3";
            lblMsg.Text = html;
            lblMsg.Visible = true;
        }
    }
}
