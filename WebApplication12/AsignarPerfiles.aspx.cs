using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    public partial class AsignarPerfiles : PaginaBase
    {
        protected override string PermisoRequerido => BE.Permisos.GestionarPerfiles;

        private readonly BLL.PerfilBLL _bll = new BLL.PerfilBLL();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            foreach (BE.USUARIO u in new BLL.UsuarioBLL().ListarTodos())
                ddlUsuario.Items.Add(new ListItem(u.Usuario, u.Id.ToString()));

            foreach (BE.Rol r in _bll.ListarRoles().OrderBy(x => x.Nombre))
                cblRoles.Items.Add(new ListItem(r.Nombre, r.Id.ToString()));

            CargarUsuario();
        }

        private void CargarUsuario()
        {
            if (ddlUsuario.Items.Count == 0) return;
            int usuarioId = int.Parse(ddlUsuario.SelectedValue);

            List<int> asignados = _bll.ObtenerIdsRolesDeUsuario(usuarioId);
            foreach (ListItem item in cblRoles.Items)
                item.Selected = asignados.Contains(int.Parse(item.Value));

            blEfectivos.Items.Clear();
            List<string> permisos = _bll.ObtenerPermisosEfectivos(usuarioId);
            foreach (string nombre in permisos.OrderBy(n => n))
                blEfectivos.Items.Add(nombre);
            lblSinPermisos.Visible = permisos.Count == 0;
        }

        protected void ddlUsuario_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblMsg.Visible = false;
            CargarUsuario();
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (ddlUsuario.Items.Count == 0) return;
            int usuarioId = int.Parse(ddlUsuario.SelectedValue);
            List<int> rolIds = cblRoles.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => int.Parse(i.Value)).ToList();

            try
            {
                _bll.AsignarPerfilesAUsuario(usuarioId, rolIds, UsuarioActual);
                lblMsg.CssClass = "alert alert-success d-block mb-3";
                lblMsg.Text = "Asignación guardada.";
            }
            catch (InvalidOperationException ex)
            {
                lblMsg.CssClass = "alert alert-danger d-block mb-3";
                lblMsg.Text = HttpUtility.HtmlEncode(ex.Message);
            }
            catch (Exception)
            {
                lblMsg.CssClass = "alert alert-danger d-block mb-3";
                lblMsg.Text = "Ocurrió un error inesperado al guardar. Revisá el estado y volvé a intentar.";
                try { new BLL.BitacoraBLL().RegistrarAccion(UsuarioActual.Usuario, "ERROR_GESTION_PERFILES"); }
                catch { }
            }
            lblMsg.Visible = true;
            CargarUsuario();
        }
    }
}
