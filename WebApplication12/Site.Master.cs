using System;

namespace CAPAS_Web
{
    public partial class Site : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            var u = Session["Usuario"] as BE.USUARIO;
            if (u == null) return;
            new BLL.PerfilBLL().RefrescarPermisosSiHaceFalta(u);
            lblUsuarioNav.Text  = u.Usuario;
            lnkInicio.NavigateUrl = PaginaBase.ObtenerPaginaInicio(u) ?? "~/Login.aspx";

            phNavMaterias.Visible    = u.TienePermiso(BE.Permisos.GestionarMaterias);
            phNavNotas.Visible       = u.TienePermiso(BE.Permisos.CargarNotas);
            phNavPeriodos.Visible    = u.TienePermiso(BE.Permisos.GestionarPeriodos);
            phNavBitacora.Visible    = u.TienePermiso(BE.Permisos.VerBitacora);
            phNavBloqueados.Visible  = u.TienePermiso(BE.Permisos.DesbloquearUsuarios);
            phNavUsuarios.Visible    = u.TienePermiso(BE.Permisos.GestionarUsuarios);
            phNavPerfiles.Visible    = u.TienePermiso(BE.Permisos.GestionarPerfiles);
            phNavAsignarPerfiles.Visible = phNavPerfiles.Visible;
            phNavIdiomas.Visible     = u.TienePermiso(BE.Permisos.GestionarIdiomas);
            phNavInscripcion.Visible = u.TienePermiso(BE.Permisos.Inscribirse);
            phNavMisCursadas.Visible = u.TienePermiso(BE.Permisos.VerMisCursadas);
            phNavCalendario.Visible  = u.TienePermiso(BE.Permisos.VerCalendario);
            phNavDashboard.Visible   = u.TienePermiso(BE.Permisos.VerDashboard);
            phNavIntegridad.Visible  = u.TienePermiso(BE.Permisos.VerificarIntegridad);
            phNavPerfil.Visible      = u.TienePermiso(BE.Permisos.EditarPerfil);
            phNavContrasena.Visible  = u.TienePermiso(BE.Permisos.CambiarContrasena);

            phSeccionGestion.Visible       = phNavMaterias.Visible || phNavNotas.Visible || phNavPeriodos.Visible
                                             || phNavBitacora.Visible || phNavBloqueados.Visible || phNavPerfiles.Visible
                                             || phNavUsuarios.Visible || phNavIdiomas.Visible;
            phSeccionAcademico.Visible     = phNavInscripcion.Visible || phNavMisCursadas.Visible || phNavCalendario.Visible;
            phSeccionHerramientas.Visible  = phNavDashboard.Visible;
            phSeccionMantenimiento.Visible = phNavIntegridad.Visible;
            phSeccionPersonal.Visible      = phNavPerfil.Visible || phNavContrasena.Visible;
        }

        protected void btnCerrarSesion_Click(object sender, EventArgs e)
        {
            var u = Session["Usuario"] as BE.USUARIO;
            if (u != null)
            {
                BE.SessionRegistry.Instancia.CerrarSesion(u.Usuario, Session.SessionID);
            }
            Session.Abandon();
            Response.Redirect("~/Login.aspx");
        }
    }
}