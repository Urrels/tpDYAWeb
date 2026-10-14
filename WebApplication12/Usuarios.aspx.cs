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

        // Usuarios sobre los que el actor puede operar: la página solo lo refleja,
        // la BLL vuelve a comprobarlo en cada operación.
        private HashSet<int> _actuables = new HashSet<int>();

        // Usuario que se está editando; sobrevive a los postbacks.
        private int? EditandoId
        {
            get => ViewState["EditandoId"] as int?;
            set => ViewState["EditandoId"] = value;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            // Solo los perfiles que el actor puede asignar.
            List<int> asignables = _bll.ListarRolesAsignablesPor(UsuarioActual);
            foreach (BE.Rol r in _bll.ListarRoles().Where(x => asignables.Contains(x.Id)).OrderBy(x => x.Nombre))
            {
                cblPerfilesAlta.Items.Add(new ListItem(r.Nombre, r.Id.ToString()));
                cblPerfilesEdit.Items.Add(new ListItem(r.Nombre, r.Id.ToString()));
            }
            CargarGrilla();
        }

        private void CargarGrilla()
        {
            _actuables = _bll.ListarUsuariosActuablesPor(UsuarioActual);
            // La BLL filtra por visibilidad, ordena y recién ahí pagina (ajustando la página a la última real).
            var resultado = _bll.ListarUsuariosVisiblesPor(UsuarioActual, paginador.PaginaActual, paginador.TamanioPagina);
            gvUsuarios.DataSource = resultado.Filas;
            gvUsuarios.DataBind();
            paginador.Establecer(resultado);
        }

        protected void paginador_CambioPagina(object sender, EventArgs e) => CargarGrilla();

        // Muestra "Dar de baja" o "Reactivar" según el estado del usuario de la fila.
        protected void gvUsuarios_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var u = (BE.USUARIO)e.Row.DataItem;
            bool puede = _actuables.Contains(u.Id);
            e.Row.FindControl("lnkEditar").Visible = puede;
            e.Row.FindControl("lnkBaja").Visible = puede && u.Activo && u.Id != UsuarioActual.Id;
            e.Row.FindControl("lnkReactivar").Visible = puede && !u.Activo;
        }

        protected void gvUsuarios_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Editar" && e.CommandName != "Baja" && e.CommandName != "Reactivar") return;
            int id = int.Parse(e.CommandArgument.ToString());

            switch (e.CommandName)
            {
                case "Editar":
                    try { CargarEdicion(id); }
                    catch (InvalidOperationException ex)
                    {
                        EditandoId = null;
                        pnlEdicion.Visible = false;
                        MostrarMsg("alert-danger", HttpUtility.HtmlEncode(ex.Message));
                        CargarGrilla();
                    }
                    break;
                case "Baja":
                    Ejecutar(() => _bll.CambiarEstadoUsuario(id, false, UsuarioActual),
                        "Usuario dado de baja.");
                    break;
                case "Reactivar":
                    Ejecutar(() => _bll.CambiarEstadoUsuario(id, true, UsuarioActual),
                        "Usuario reactivado.");
                    break;
            }
        }

        private void CargarEdicion(int id)
        {
            BE.USUARIO u = _bll.ListarUsuariosVisiblesPor(UsuarioActual).FirstOrDefault(x => x.Id == id);
            if (u == null || !_bll.ListarUsuariosActuablesPor(UsuarioActual).Contains(id))
            { EditandoId = null; pnlEdicion.Visible = false; return; }

            EditandoId = id;
            lblEditUsuario.Text = HttpUtility.HtmlEncode(u.Usuario);
            txtEditDireccion.Text = _bll.ObtenerDireccion(u.Usuario, UsuarioActual) ?? "";
            List<int> asignados = _bll.ObtenerIdsRolesDeUsuarioPor(id, UsuarioActual);
            foreach (ListItem item in cblPerfilesEdit.Items)
                item.Selected = asignados.Contains(int.Parse(item.Value));
            // Los perfiles propios no se editan desde acá, ni se restablece la propia contraseña.
            bool esUnoMismo = id == UsuarioActual.Id;
            cblPerfilesEdit.Enabled = !esUnoMismo;
            pnlReset.Visible = !esUnoMismo;
            pnlEdicion.Visible = true;
        }

        protected void btnCrear_Click(object sender, EventArgs e)
        {
            List<int> roles = Seleccionados(cblPerfilesAlta);
            Ejecutar(() => _bll.CrearUsuario(txtUsuario.Text, txtPass.Text, txtConf.Text, txtDireccion.Text,
                    roles, UsuarioActual),
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
            // Los perfiles propios no se editan acá (el control está deshabilitado y no envía valores).
            List<int> roles = id == UsuarioActual.Id ? _bll.ObtenerIdsRolesDeUsuarioPor(id, UsuarioActual) : Seleccionados(cblPerfilesEdit);
            Ejecutar(() => _bll.ModificarUsuario(id, txtEditDireccion.Text, roles, UsuarioActual),
                "Cambios guardados.", () => CargarEdicion(id));
        }

        protected void btnResetPass_Click(object sender, EventArgs e)
        {
            if (!EditandoId.HasValue) return;
            int id = EditandoId.Value;
            Ejecutar(() => _bll.ResetearContrasena(id, txtResetPass.Text, txtResetConf.Text, UsuarioActual),
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
