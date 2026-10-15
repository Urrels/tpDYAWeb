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

            foreach (BE.USUARIO u in _bll.ListarUsuariosVisiblesPor(UsuarioActual))
                ddlUsuario.Items.Add(new ListItem(u.Usuario, u.Id.ToString()));

            List<int> asignables = _bll.ListarRolesAsignablesPor(UsuarioActual);
            foreach (BE.Rol r in _bll.ListarRoles().Where(x => asignables.Contains(x.Id)).OrderBy(x => x.Nombre))
                cblRoles.Items.Add(new ListItem(r.Nombre, r.Id.ToString()));

            CargarUsuario();
        }

        private void CargarUsuario(bool reintentar = true)
        {
            if (ddlUsuario.Items.Count == 0) return;
            try
            {
                int usuarioId = int.Parse(ddlUsuario.SelectedValue);

                List<int> asignados = _bll.ObtenerIdsRolesDeUsuarioPor(usuarioId, UsuarioActual);
                foreach (ListItem item in cblRoles.Items)
                    item.Selected = asignados.Contains(int.Parse(item.Value));

                blEfectivos.Items.Clear();
                List<string> permisos = _bll.ObtenerPermisosEfectivosPor(usuarioId, UsuarioActual);
                foreach (string nombre in permisos.OrderBy(n => n))
                    blEfectivos.Items.Add(Textos.Valor(nombre));
                lblSinPermisos.Visible = permisos.Count == 0;
            }
            catch (InvalidOperationException ex)
            {
                lblMsg.CssClass = "alert alert-danger d-block mb-3";
                lblMsg.Text = Textos.MH(ex.Message);
                lblMsg.Visible = true;
                blEfectivos.Items.Clear();
                lblSinPermisos.Visible = false;
                foreach (ListItem item in cblRoles.Items) item.Selected = false;

                ddlUsuario.Items.Clear();
                try
                {
                    foreach (BE.USUARIO u in _bll.ListarUsuariosVisiblesPor(UsuarioActual))
                        ddlUsuario.Items.Add(new ListItem(u.Usuario, u.Id.ToString()));
                }
                catch (InvalidOperationException) { }
                if (reintentar) CargarUsuario(false);
            }
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
                lblMsg.Text = Textos.TH("AsignarPerfiles.msgGuardada", "Asignación guardada.");
            }
            catch (InvalidOperationException ex)
            {
                lblMsg.CssClass = "alert alert-danger d-block mb-3";
                lblMsg.Text = Textos.MH(ex.Message);
            }
            catch (Exception)
            {
                lblMsg.CssClass = "alert alert-danger d-block mb-3";
                lblMsg.Text = Textos.TH("AsignarPerfiles.msgInesperado", "Ocurrió un error inesperado al guardar. Revisá el estado y volvé a intentar.");
                try { new BLL.BitacoraBLL().RegistrarAccion(UsuarioActual.Usuario, "ERROR_GESTION_PERFILES"); }
                catch { }
            }
            lblMsg.Visible = true;
            CargarUsuario();
        }
    }
}
