using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    public partial class Materias : PaginaBase
    {
        protected override string PermisoRequerido => BE.Permisos.GestionarMaterias;

        private readonly BLL.MateriaBLL _bll = new BLL.MateriaBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) CargarGrilla();
        }

        private void CargarGrilla()
        {
            // La BLL ajusta la página a la última real (p. ej. tras dar de baja la única fila de la última).
            var resultado = _bll.ListarPaginado(paginador.PaginaActual, paginador.TamanioPagina);
            gvMaterias.DataSource = resultado.Filas;
            gvMaterias.DataBind();
            paginador.Establecer(resultado);
        }

        protected void paginador_CambioPagina(object sender, EventArgs e) => CargarGrilla();

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
            // Una materia nueva queda al final (ORDER BY ID): se salta a la página donde caería con una fila más.
            if (ok && m.Id == 0)
                paginador.PaginaActual = (paginador.TotalFilas + paginador.TamanioPagina) / paginador.TamanioPagina;
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

        protected void btnExportarXml_Click(object sender, EventArgs e)
        {
            string usuario = (Session["Usuario"] as BE.USUARIO).Usuario;
            byte[] xml = new BLL.MateriaXmlBLL().Exportar(usuario);

            Response.Clear();
            Response.ContentType = "application/xml";
            Response.AddHeader("Content-Disposition", $"attachment; filename=materias_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
            Response.BinaryWrite(xml);
            Response.End();
        }

        protected void btnImportarXml_Click(object sender, EventArgs e)
        {
            if (!fileXml.HasFile)
            {
                MostrarMsg("alert-warning", "Elegí un archivo .xml para importar.");
                CargarGrilla();
                return;
            }
            // Se lee como máximo el tope + 1 byte para no cargar en memoria un archivo enorme.
            byte[] contenido;
            using (var ms = new System.IO.MemoryStream())
            {
                var buffer = new byte[8192];
                int leidos;
                var entrada = fileXml.PostedFile.InputStream;
                while (ms.Length <= BLL.MateriaXmlBLL.TamanioMaximoBytes && (leidos = entrada.Read(buffer, 0, buffer.Length)) > 0)
                    ms.Write(buffer, 0, leidos);
                contenido = ms.ToArray();
            }

            string usuario = (Session["Usuario"] as BE.USUARIO).Usuario;
            BLL.ResultadoImportacionXml r = new BLL.MateriaXmlBLL().Importar(contenido, usuario);
            if (r.Ok)
                MostrarMsg("alert-success", $"✔ Importación completa: {r.Creadas} materias nuevas, {r.Actualizadas} actualizadas, {r.CorrelativasAgregadas} correlativas agregadas.");
            else
                MostrarMsg("alert-danger", "✖ No se importó nada:<br/>" +
                    string.Join("<br/>", r.Errores.Take(20).Select(System.Web.HttpUtility.HtmlEncode)) +
                    (r.Errores.Count > 20 ? $"<br/>… y {r.Errores.Count - 20} errores más." : ""));
            CargarGrilla();
        }

        private void MostrarMsg(string css, string texto)
        {
            lblMsg.CssClass = "alert " + css + " d-block mb-3";
            lblMsg.Text = texto;
            lblMsg.Visible = true;
        }
    }
}