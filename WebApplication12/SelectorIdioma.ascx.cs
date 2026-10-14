using System;
using System.Web.UI;

namespace CAPAS_Web
{
    // Selector de idioma reutilizable (equivale a IdiomaUIHelper.AgregarSelector de la versión de escritorio).
    // Se coloca en el master y en Login.aspx; al elegir un idioma llama a la fachada, que avisa a las páginas observadoras.
    public partial class SelectorIdioma : UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) Cargar();
        }

        private void Cargar()
        {
            var fachada = IdiomaWeb.Fachada;
            fachada.AplicarPredeterminadoSiHaceFalta();
            BE.IDIOMA activo = fachada.IdiomaActivo;

            ddlIdioma.Items.Clear();
            foreach (BE.IDIOMA idioma in fachada.ListarDisponibles())
                ddlIdioma.Items.Add(new System.Web.UI.WebControls.ListItem(idioma.Nombre, idioma.Id.ToString()));
            if (activo != null && ddlIdioma.Items.FindByValue(activo.Id.ToString()) != null)
                ddlIdioma.SelectedValue = activo.Id.ToString();
        }

        protected void ddlIdioma_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!int.TryParse(ddlIdioma.SelectedValue, out int id)) return;
            var fachada = IdiomaWeb.Fachada;
            // Se valida contra los habilitados: el valor viene del navegador y podría estar alterado.
            BE.IDIOMA elegido = fachada.ListarDisponibles().Find(i => i.Id == id);
            if (elegido == null) return;
            fachada.Cambiar(elegido, IdiomaWeb.IdUsuarioActual);
            // Se vuelve a pedir la página con GET: así lo que armó el código al cargarla (listas, mensajes,
            // calendario) también sale en el idioma nuevo.
            Response.Redirect(Request.RawUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
