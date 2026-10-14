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

        // El menú se arma al final del ciclo para reflejar cambios de perfiles
        // hechos durante este mismo pedido (por ejemplo desde Perfiles.aspx).
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
            phNavPerfiles.Visible    = u.TienePermiso(BE.Permisos.GestionarPerfiles);
            phNavAsignarPerfiles.Visible = phNavPerfiles.Visible;
            phNavInscripcion.Visible = u.TienePermiso(BE.Permisos.Inscribirse);
            phNavMisCursadas.Visible = u.TienePermiso(BE.Permisos.VerMisCursadas);
            phNavCalendario.Visible  = u.TienePermiso(BE.Permisos.VerCalendario);
            phNavDashboard.Visible   = u.TienePermiso(BE.Permisos.VerDashboard);
            phNavIntegridad.Visible  = u.TienePermiso(BE.Permisos.VerificarIntegridad);
            phNavPerfil.Visible      = u.TienePermiso(BE.Permisos.EditarPerfil);
            phNavContrasena.Visible  = u.TienePermiso(BE.Permisos.CambiarContrasena);

            // Cada sección solo se muestra si alguno de sus enlaces es visible.
            phSeccionGestion.Visible       = phNavMaterias.Visible || phNavNotas.Visible || phNavPeriodos.Visible
                                             || phNavBitacora.Visible || phNavBloqueados.Visible || phNavPerfiles.Visible;
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
                // El logueo de logout se centraliza en Global.asax.cs (Session_End),
                // que Session.Abandon() dispara siempre (modo InProc) — cubre tanto
                // este botón como el timeout por inactividad, de forma uniforme.
                BE.SessionRegistry.Instancia.CerrarSesion(u.Usuario);
            }
            Session.Abandon();
            Response.Redirect("~/Login.aspx");
        }
    }
}