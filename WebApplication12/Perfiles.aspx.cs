using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    public partial class Perfiles : PaginaBase
    {
        protected override string PermisoRequerido => BE.Permisos.GestionarPerfiles;

        private readonly BLL.PerfilBLL _bll = new BLL.PerfilBLL();

        // Perfil seleccionado en el árbol; sobrevive a los postbacks.
        private int? RolSeleccionadoId
        {
            get => ViewState["RolId"] as int?;
            set => ViewState["RolId"] = value;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) Recargar();
        }

        // =====================================================
        // Carga de la pantalla
        // =====================================================

        // Evita reentrancia si al seleccionar un nodo por código se dispara el evento del árbol.
        private bool _recargando;

        private void Recargar()
        {
            _recargando = true;
            try { RecargarInterno(); }
            finally { _recargando = false; }
        }

        private void RecargarInterno()
        {
            List<BE.Rol> raices = _bll.ObtenerArbol();
            tvPerfiles.Nodes.Clear();
            foreach (BE.Rol raiz in raices)
                tvPerfiles.Nodes.Add(ConstruirNodo(raiz));

            // Si el perfil seleccionado ya no existe (por ejemplo, se eliminó), se limpia la selección.
            BE.Rol seleccionado = RolSeleccionadoId.HasValue ? _bll.ObtenerRol(RolSeleccionadoId.Value) : null;
            if (seleccionado == null) RolSeleccionadoId = null;

            if (seleccionado != null)
            {
                TreeNode nodo = BuscarNodo(tvPerfiles.Nodes, "R:" + seleccionado.Id);
                if (nodo != null) nodo.Select();
            }
            MostrarRol(seleccionado);
        }

        private static TreeNode ConstruirNodo(BE.Rol rol)
        {
            var nodo = new TreeNode
            {
                Text = "<i class='bi bi-diagram-3'></i> <strong>" + HttpUtility.HtmlEncode(rol.Nombre) + "</strong>",
                Value = "R:" + rol.Id,
                SelectAction = TreeNodeSelectAction.Select
            };
            foreach (BE.NodoPermiso hijo in rol.ObtenerHijos())
            {
                if (hijo is BE.Rol subRol)
                    nodo.ChildNodes.Add(ConstruirNodo(subRol));
                else
                    nodo.ChildNodes.Add(new TreeNode
                    {
                        Text = "<i class='bi bi-key'></i> <span class='text-muted'>" + HttpUtility.HtmlEncode(hijo.Nombre) + "</span>",
                        Value = "P:" + rol.Id + ":" + hijo.Id,
                        SelectAction = TreeNodeSelectAction.None
                    });
            }
            return nodo;
        }

        private static TreeNode BuscarNodo(TreeNodeCollection nodos, string valor)
        {
            foreach (TreeNode n in nodos)
            {
                if (n.Value == valor) return n;
                TreeNode hallado = BuscarNodo(n.ChildNodes, valor);
                if (hallado != null) return hallado;
            }
            return null;
        }

        private void MostrarRol(BE.Rol rol)
        {
            pnlRol.Visible = rol != null;
            lblSinSeleccion.Visible = rol == null;
            btnCrearHijo.Enabled = rol != null;
            if (rol == null) return;

            lblRolNombre.Text = HttpUtility.HtmlEncode(rol.Nombre);
            lblProtegido.Visible = rol.Protegido;
            txtRenombrar.Text = rol.Nombre;

            // Padres posibles: cualquier perfil que no genere un ciclo (no él mismo ni sus descendientes).
            List<BE.Rol> todos = _bll.ListarRoles();
            ddlPadre.Items.Clear();
            ddlPadre.Items.Add(new ListItem("(raíz — sin padre)", ""));
            foreach (BE.Rol r in todos.OrderBy(x => x.Nombre))
                if (!BLL.PerfilBLL.GenerariaCiclo(rol.Id, r.Id, todos))
                    ddlPadre.Items.Add(new ListItem(r.Nombre, r.Id.ToString()));
            ddlPadre.SelectedValue = rol.PadreId.HasValue ? rol.PadreId.Value.ToString() : "";

            List<int> propios = _bll.ObtenerIdsPermisosDirectos(rol.Id);
            cblPermisos.Items.Clear();
            foreach (BE.Permiso p in _bll.ListarPermisosDisponibles())
                cblPermisos.Items.Add(new ListItem(p.Nombre, p.Id.ToString()) { Selected = propios.Contains(p.Id) });

            blEfectivos.Items.Clear();
            foreach (string nombre in rol.ObtenerPermisos().OrderBy(n => n))
                blEfectivos.Items.Add(nombre);

            bool editable = !rol.Protegido;
            txtRenombrar.Enabled = btnRenombrar.Enabled = editable;
            ddlPadre.Enabled = btnCambiarPadre.Enabled = editable;
            cblPermisos.Enabled = btnGuardarPermisos.Enabled = editable;
            btnEliminar.Enabled = editable;
        }

        // =====================================================
        // Eventos
        // =====================================================

        protected void tvPerfiles_SelectedNodeChanged(object sender, EventArgs e)
        {
            if (_recargando) return;
            string valor = tvPerfiles.SelectedNode?.Value;
            RolSeleccionadoId = valor != null && valor.StartsWith("R:") ? int.Parse(valor.Substring(2)) : (int?)null;
            Recargar();
        }

        protected void btnCrearRaiz_Click(object sender, EventArgs e) => Ejecutar(
            () => _bll.CrearRol(txtNuevoNombre.Text, null, UsuarioActual),
            "Perfil creado.", () => txtNuevoNombre.Text = "");

        protected void btnCrearHijo_Click(object sender, EventArgs e)
        {
            if (!RolSeleccionadoId.HasValue) return;
            Ejecutar(() => _bll.CrearRol(txtNuevoNombre.Text, RolSeleccionadoId, UsuarioActual),
                "Perfil creado dentro del seleccionado.", () => txtNuevoNombre.Text = "");
        }

        protected void btnRenombrar_Click(object sender, EventArgs e)
        {
            if (!RolSeleccionadoId.HasValue) return;
            Ejecutar(() => _bll.RenombrarRol(RolSeleccionadoId.Value, txtRenombrar.Text, UsuarioActual),
                "Perfil renombrado.");
        }

        protected void btnCambiarPadre_Click(object sender, EventArgs e)
        {
            if (!RolSeleccionadoId.HasValue) return;
            int? padre = string.IsNullOrEmpty(ddlPadre.SelectedValue) ? (int?)null : int.Parse(ddlPadre.SelectedValue);
            Ejecutar(() => _bll.CambiarPadre(RolSeleccionadoId.Value, padre, UsuarioActual),
                "Padre actualizado.");
        }

        protected void btnGuardarPermisos_Click(object sender, EventArgs e)
        {
            if (!RolSeleccionadoId.HasValue) return;
            List<int> ids = cblPermisos.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => int.Parse(i.Value)).ToList();
            Ejecutar(() => _bll.GuardarPermisosDeRol(RolSeleccionadoId.Value, ids, UsuarioActual),
                "Permisos guardados.");
        }

        protected void btnEliminar_Click(object sender, EventArgs e)
        {
            if (!RolSeleccionadoId.HasValue) return;
            int id = RolSeleccionadoId.Value;
            Ejecutar(() => _bll.EliminarRol(id, UsuarioActual), "Perfil eliminado.",
                () => RolSeleccionadoId = null);
        }

        /// <summary>
        /// Ejecuta una operación de la BLL: muestra el mensaje de éxito o el
        /// motivo del rechazo, y siempre vuelve a cargar el árbol.
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
                RegistrarErrorInesperado();
            }
            Recargar();
        }

        // Deja constancia en bitácora; si la base no responde no se agrega otro error.
        private void RegistrarErrorInesperado()
        {
            try { new BLL.BitacoraBLL().RegistrarAccion(UsuarioActual.Usuario, "ERROR_GESTION_PERFILES"); }
            catch { }
        }

        private void MostrarMsg(string css, string texto)
        {
            lblMsg.CssClass = "alert " + css + " d-block mb-3";
            lblMsg.Text = texto;
            lblMsg.Visible = true;
        }
    }
}
