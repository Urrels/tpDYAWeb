using System;
using System.Web.UI;

namespace CAPAS_Web
{
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
            BE.IDIOMA elegido = fachada.ListarDisponibles().Find(i => i.Id == id);
            if (elegido == null) return;
            fachada.Cambiar(elegido, IdiomaWeb.IdUsuarioActual);
            Response.Redirect(Request.RawUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
