using System;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    public partial class Materias : System.Web.UI.Page
    {
        private readonly BLL.MateriaBLL _bll = new BLL.MateriaBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            var u = Session["Usuario"] as BE.USUARIO;
            if (!u.EsAdmin) Response.Redirect("~/Menu.aspx");
            if (!IsPostBack) CargarGrilla();
        }

        private void CargarGrilla()
        {
            gvMaterias.DataSource = _bll.Listar();
            gvMaterias.DataBind();
        }

        protected void btnNueva_Click(object sender, EventArgs e)
        {
            hfIdMateria.Value = "0";
            txtNombre.Text = txtCodigo.Text = "";
            ddlModalidad.SelectedValue = "Presencial";
            ddlPeso.SelectedValue = "3";
            lblTituloForm.Text = "Nueva Materia";
            pnlForm.Visible = true;
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            var m = new BE.MATERIA
            {
                Id = int.Parse(hfIdMateria.Value),
                Nombre = txtNombre.Text.Trim(),
                Codigo = txtCodigo.Text.Trim(),
                Modalidad = ddlModalidad.SelectedValue,
                Peso = int.Parse(ddlPeso.SelectedValue)
            };
            string usuario = (Session["Usuario"] as BE.USUARIO).Usuario;
            bool ok = m.Id == 0 ? _bll.Insertar(m, usuario) > 0 : _bll.Actualizar(m, usuario);
            MostrarMsg(ok ? "alert-success" : "alert-danger",
                       ok ? "✔ Guardado." : "✖ Error al guardar.");
            pnlForm.Visible = false;
            CargarGrilla();
        }

        protected void btnCancelar_Click(object sender, EventArgs e) => pnlForm.Visible = false;

        protected void gvMaterias_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id = int.Parse(e.CommandArgument.ToString());
            string usuario = (Session["Usuario"] as BE.USUARIO).Usuario;

            if (e.CommandName == "Editar")
            {
                BE.MATERIA m = _bll.Obtener(id);
                hfIdMateria.Value = m.Id.ToString();
                txtNombre.Text = m.Nombre;
                txtCodigo.Text = m.Codigo;
                ddlModalidad.SelectedValue = m.Modalidad;
                ddlPeso.SelectedValue = m.Peso.ToString();
                lblTituloForm.Text = "Editar Materia";
                pnlForm.Visible = true;
            }
            else if (e.CommandName == "Eliminar")
            {
                bool ok = _bll.Eliminar(id, usuario);
                MostrarMsg(ok ? "alert-success" : "alert-danger",
                           ok ? "✔ Materia dada de baja." : "✖ Error.");
                CargarGrilla();
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