using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    public partial class Eventos : System.Web.UI.Page
    {
        private readonly BLL.EventoBLL _bll = new BLL.EventoBLL();
        private readonly BLL.MateriaBLL _matBll = new BLL.MateriaBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            if (!IsPostBack) CargarTodo();
        }

        private void CargarTodo()
        {
            ddlMateria.DataSource = _matBll.Listar();
            ddlMateria.DataTextField = "Nombre";
            ddlMateria.DataValueField = "Id";
            ddlMateria.DataBind();
            int id = (Session["Usuario"] as BE.USUARIO).Id;
            gvEventos.DataSource = _bll.ListarPorUsuario(id);
            gvEventos.DataBind();
        }

        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            hfIdEvento.Value = "0";
            txtFecha.Text = txtDescripcion.Text = "";
            lblTituloForm.Text = "Nuevo Evento";
            pnlForm.Visible = true;
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            var u = Session["Usuario"] as BE.USUARIO;
            var ev = new BE.EVENTO_ACADEMICO
            {
                Id = int.Parse(hfIdEvento.Value),
                IdMateria = int.Parse(ddlMateria.SelectedValue),
                IdUsuario = u.Id,
                Tipo = ddlTipo.SelectedValue,
                Descripcion = txtDescripcion.Text.Trim(),
                Fecha = DateTime.Parse(txtFecha.Text),
                Peso = int.Parse(ddlPeso.SelectedValue)
            };
            bool ok = ev.Id == 0 ? _bll.Insertar(ev, u.Usuario) : _bll.Actualizar(ev, u.Usuario);
            MostrarMsg(ok ? "alert-success" : "alert-danger",
                       ok ? "✔ Evento guardado." : "✖ Error.");
            pnlForm.Visible = false;
            CargarTodo();
        }

        protected void btnCancelar_Click(object sender, EventArgs e) => pnlForm.Visible = false;

        protected void gvEventos_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            var u = Session["Usuario"] as BE.USUARIO;
            int id = int.Parse(e.CommandArgument.ToString());

            if (e.CommandName == "Editar")
            {
                var ev = _bll.ListarPorUsuario(u.Id).FirstOrDefault(x => x.Id == id);
                if (ev == null) return;
                hfIdEvento.Value = ev.Id.ToString();
                ddlMateria.SelectedValue = ev.IdMateria.ToString();
                ddlTipo.SelectedValue = ev.Tipo;
                txtFecha.Text = ev.Fecha.ToString("yyyy-MM-ddTHH:mm");
                txtDescripcion.Text = ev.Descripcion;
                ddlPeso.SelectedValue = ev.Peso.ToString();
                lblTituloForm.Text = "Editar Evento";
                pnlForm.Visible = true;
            }
            else if (e.CommandName == "Eliminar")
            {
                bool ok = _bll.Eliminar(id, u.Usuario);
                MostrarMsg(ok ? "alert-success" : "alert-danger",
                           ok ? "✔ Eliminado." : "✖ Error.");
                CargarTodo();
            }
        }

        private void MostrarMsg(string css, string texto)
        {
            lblMsg.CssClass = "alert " + css + " d-block mb-3";
            lblMsg.Text = texto; lblMsg.Visible = true;
        }
    }
}