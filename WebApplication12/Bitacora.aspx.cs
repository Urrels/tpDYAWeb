using System;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    public partial class Bitacora : PaginaBase
    {
        protected override string PermisoRequerido => BE.Permisos.VerBitacora;

        private readonly BLL.BitacoraBLL _bll = new BLL.BitacoraBLL();

        protected void Page_Load(object sender, EventArgs e)
        {

            if (!IsPostBack)
            {
                CargarUsuarios();
                paginador.PaginaActual = 1;
                CargarGrilla();
            }
        }

        private void CargarUsuarios()
        {
            ddlUsuario.Items.Clear();
            ddlUsuario.Items.Add(new ListItem("Todos", ""));
            foreach (string usuario in _bll.ListarUsuariosDistinct())
                ddlUsuario.Items.Add(new ListItem(usuario, usuario));
        }

        // Filtro aplicado (el de la última vez que se pulsó Filtrar/Limpiar). La grilla y el
        // paginador usan esta copia y no los controles, que el usuario puede haber editado sin aplicar.
        private string FiltroUsuario { get => ViewState["FiltroUsuario"] as string; set => ViewState["FiltroUsuario"] = value; }
        private string FiltroCriticidad { get => ViewState["FiltroCriticidad"] as string; set => ViewState["FiltroCriticidad"] = value; }
        private DateTime? FiltroDesde { get => ViewState["FiltroDesde"] as DateTime?; set => ViewState["FiltroDesde"] = value; }
        private DateTime? FiltroHasta { get => ViewState["FiltroHasta"] as DateTime?; set => ViewState["FiltroHasta"] = value; }

        private void AplicarFiltroDesdeControles()
        {
            FiltroUsuario = string.IsNullOrEmpty(ddlUsuario.SelectedValue) ? null : ddlUsuario.SelectedValue;
            FiltroCriticidad = string.IsNullOrEmpty(ddlCriticidad.SelectedValue) ? null : ddlCriticidad.SelectedValue;

            FiltroDesde = null;
            FiltroHasta = null;
            if (DateTime.TryParse(txtFechaDesde.Text, out DateTime fd)) FiltroDesde = fd.Date;
            // "hasta" es un límite exclusivo en el SP (FECHA < @fecha_hasta), así que
            // se corre un día para incluir todo el día seleccionado por el usuario.
            if (DateTime.TryParse(txtFechaHasta.Text, out DateTime fh)) FiltroHasta = fh.Date.AddDays(1);
        }

        private void CargarGrilla()
        {
            var resultado = _bll.ListarPaginado(FiltroUsuario, FiltroCriticidad, FiltroDesde, FiltroHasta,
                paginador.PaginaActual, paginador.TamanioPagina);

            gvBitacora.DataSource = resultado.Filas;
            gvBitacora.DataBind();
            paginador.Establecer(resultado);
        }

        protected void btnFiltrar_Click(object sender, EventArgs e)
        {
            AplicarFiltroDesdeControles();
            paginador.PaginaActual = 1;
            CargarGrilla();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            ddlUsuario.SelectedIndex = 0;
            ddlCriticidad.SelectedIndex = 0;
            txtFechaDesde.Text = "";
            txtFechaHasta.Text = "";
            AplicarFiltroDesdeControles();
            paginador.PaginaActual = 1;
            CargarGrilla();
        }

        protected void paginador_CambioPagina(object sender, EventArgs e) => CargarGrilla();

        protected string GetCriticidadCssClass(string criticidad)
        {
            switch (criticidad)
            {
                case "Alta": return "bg-danger";
                case "Media": return "bg-warning text-dark";
                case "Baja": return "bg-success";
                default: return "bg-secondary";
            }
        }
    }
}
