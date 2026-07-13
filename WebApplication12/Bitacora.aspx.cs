using System;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    public partial class Bitacora : System.Web.UI.Page
    {
        private const int TAMANIO_PAGINA = 50;

        private readonly BLL.BitacoraBLL _bll = new BLL.BitacoraBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            var u = Session["Usuario"] as BE.USUARIO;
            if (!u.EsAdmin) Response.Redirect("~/Menu.aspx");

            if (!IsPostBack)
            {
                CargarUsuarios();
                PaginaActual = 1;
                CargarGrilla();
            }
        }

        private int PaginaActual
        {
            get => ViewState["Pagina"] != null ? (int)ViewState["Pagina"] : 1;
            set => ViewState["Pagina"] = value;
        }

        private void CargarUsuarios()
        {
            ddlUsuario.Items.Clear();
            ddlUsuario.Items.Add(new ListItem("Todos", ""));
            foreach (string usuario in _bll.ListarUsuariosDistinct())
                ddlUsuario.Items.Add(new ListItem(usuario, usuario));
        }

        private void CargarGrilla()
        {
            string usuario = string.IsNullOrEmpty(ddlUsuario.SelectedValue) ? null : ddlUsuario.SelectedValue;
            string criticidad = string.IsNullOrEmpty(ddlCriticidad.SelectedValue) ? null : ddlCriticidad.SelectedValue;

            DateTime? fechaDesde = null;
            DateTime? fechaHasta = null;
            if (DateTime.TryParse(txtFechaDesde.Text, out DateTime fd)) fechaDesde = fd.Date;
            // "hasta" es un límite exclusivo en el SP (FECHA < @fecha_hasta), así que
            // se corre un día para incluir todo el día seleccionado por el usuario.
            if (DateTime.TryParse(txtFechaHasta.Text, out DateTime fh)) fechaHasta = fh.Date.AddDays(1);

            var resultado = _bll.ListarPaginado(usuario, criticidad, fechaDesde, fechaHasta, PaginaActual, TAMANIO_PAGINA);

            gvBitacora.DataSource = resultado.Filas;
            gvBitacora.DataBind();

            int totalPaginas = resultado.TotalFilas == 0 ? 1 : (int)Math.Ceiling(resultado.TotalFilas / (double)TAMANIO_PAGINA);
            if (PaginaActual > totalPaginas) PaginaActual = totalPaginas;

            lblPagina.Text = $"Página {PaginaActual} de {totalPaginas} ({resultado.TotalFilas} registros)";
            btnAnterior.Enabled = PaginaActual > 1;
            btnSiguiente.Enabled = PaginaActual < totalPaginas;
        }

        protected void btnFiltrar_Click(object sender, EventArgs e)
        {
            PaginaActual = 1;
            CargarGrilla();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            ddlUsuario.SelectedIndex = 0;
            ddlCriticidad.SelectedIndex = 0;
            txtFechaDesde.Text = "";
            txtFechaHasta.Text = "";
            PaginaActual = 1;
            CargarGrilla();
        }

        protected void btnAnterior_Click(object sender, EventArgs e)
        {
            if (PaginaActual > 1) PaginaActual--;
            CargarGrilla();
        }

        protected void btnSiguiente_Click(object sender, EventArgs e)
        {
            PaginaActual++;
            CargarGrilla();
        }

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
