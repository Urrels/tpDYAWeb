using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    public partial class Usuarios : PaginaBase
    {
        protected override string PermisoRequerido => BE.Permisos.GestionarUsuarios;

        private readonly BLL.PerfilBLL _bll = new BLL.PerfilBLL();

        // Usuario que se está editando; sobrevive a los postbacks.
        private int? EditandoId
        {
            get => ViewState["EditandoId"] as int?;
            set => ViewState["EditandoId"] = value;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            foreach (BE.Rol r in _bll.ListarRoles().OrderBy(x => x.Nombre))
            {
                cblPerfilesAlta.Items.Add(new ListItem(r.Nombre, r.Id.ToString()));
                cblPerfilesEdit.Items.Add(new ListItem(r.Nombre, r.Id.ToString()));
            }
            CargarGrilla();
        }

        private void CargarGrilla()
        {
            gvUsuarios.DataSource = _bll.ListarUsuariosParaGestion();
            gvUsuarios.DataBind();
        }

        // Muestra "Dar de baja" o "Reactivar" según el estado del usuario de la fila.
        protected void gvUsuarios_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var u = (BE.USUARIO)e.Row.DataItem;
            e.Row.FindControl("lnkBaja").Visible = u.Activo;
            e.Row.FindControl("lnkReactivar").Visible = !u.Activo;
        }

        protected void gvUsuarios_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Editar" && e.CommandName != "Baja" && e.CommandName != "Reactivar") return;
            int id = int.Parse(e.CommandArgument.ToString());

            switch (e.CommandName)
            {
                case "Editar":
                    CargarEdicion(id);
                    break;
                case "Baja":
                    Ejecutar(() => _bll.CambiarEstadoUsuario(id, false, UsuarioActual.Id, UsuarioActual.Usuario),
                        "Usuario dado de baja.");
                    break;
                case "Reactivar":
                    Ejecutar(() => _bll.CambiarEstadoUsuario(id, true, UsuarioActual.Id, UsuarioActual.Usuario),
                        "Usuario reactivado.");
                    break;
            }
        }

        private void CargarEdicion(int id)
        {
            BE.USUARIO u = _bll.ListarUsuariosParaGestion().FirstOrDefault(x => x.Id == id);
            if (u == null) { EditandoId = null; pnlEdicion.Visible = false; return; }

            EditandoId = id;
            lblEditUsuario.Text = HttpUtility.HtmlEncode(u.Usuario);
            txtEditDireccion.Text = _bll.ObtenerDireccion(u.Usuario) ?? "";
            List<int> asignados = _bll.ObtenerIdsRolesDeUsuario(id);
            foreach (ListItem item in cblPerfilesEdit.Items)
                item.Selected = asignados.Contains(int.Parse(item.Value));
            pnlEdicion.Visible = true;
        }

        protected void btnCrear_Click(object sender, EventArgs e)
        {
            List<int> roles = Seleccionados(cblPerfilesAlta);
            Ejecutar(() => _bll.CrearUsuario(txtUsuario.Text, txtPass.Text, txtConf.Text, txtDireccion.Text,
                    roles, UsuarioActual.Usuario),
                "Usuario creado.", () =>
                {
                    txtUsuario.Text = txtDireccion.Text = "";
                    foreach (ListItem i in cblPerfilesAlta.Items) i.Selected = false;
                });
        }

        protected void btnGuardarEdicion_Click(object sender, EventArgs e)
        {
            if (!EditandoId.HasValue) return;
            int id = EditandoId.Value;
            List<int> roles = Seleccionados(cblPerfilesEdit);
            Ejecutar(() => _bll.ModificarUsuario(id, txtEditDireccion.Text, roles, UsuarioActual.Usuario),
                "Cambios guardados.", () => CargarEdicion(id));
        }

        protected void btnResetPass_Click(object sender, EventArgs e)
        {
            if (!EditandoId.HasValue) return;
            int id = EditandoId.Value;
            Ejecutar(() => _bll.ResetearContrasena(id, txtResetPass.Text, txtResetConf.Text, UsuarioActual.Usuario),
                "Contraseña restablecida.", () => txtResetPass.Text = txtResetConf.Text = "");
        }

        protected void btnCancelar_Click(object sender, EventArgs e)
        {
            EditandoId = null;
            pnlEdicion.Visible = false;
        }

        private static List<int> Seleccionados(CheckBoxList lista) =>
            lista.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => int.Parse(i.Value)).ToList();

        /// <summary>
        /// Ejecuta una operación de la BLL: muestra el éxito o el motivo del
        /// rechazo, y vuelve a cargar la grilla. Un error inesperado no llega al usuario como pantalla de error.
        /// </summary>
        private void Ejecutar(Action operacion, string mensajeExito, Action alExito = null)
        {
            try
            {
                operacion();
                alExito?.Invoke();
                MostrarMsg("alert-success", mensajeExito);
            }
            catch (InvalidOperationException ex)
            {
                MostrarMsg("alert-danger", HttpUtility.HtmlEncode(ex.Message));
            }
            catch (Exception)
            {
                MostrarMsg("alert-danger", "Ocurrió un error inesperado al guardar. Revisá el estado y volvé a intentar.");
                try { new BLL.BitacoraBLL().RegistrarAccion(UsuarioActual.Usuario, "ERROR_GESTION_USUARIOS"); }
                catch { }
            }
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
