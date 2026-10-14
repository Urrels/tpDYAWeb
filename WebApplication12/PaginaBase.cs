using System;
using System.Web;

namespace CAPAS_Web
{
    /// <summary>
    /// Página base con control de acceso declarativo: cada página indica el
    /// permiso que exige y la base se ocupa de redirigir a quien no inició
    /// sesión o no lo tiene.
    /// </summary>
    public class PaginaBase : System.Web.UI.Page
    {
        // Orden de prioridad para elegir la página de inicio de un usuario
        // según sus permisos.
        private static readonly string[,] PaginasInicio =
        {
            { BE.Permisos.VerificarIntegridad, "~/Integridad.aspx" },
            { BE.Permisos.GestionarMaterias,   "~/Materias.aspx" },
            { BE.Permisos.VerMenuAlumno,       "~/Menu.aspx" },
            { BE.Permisos.EditarPerfil,        "~/Perfil.aspx" }
        };

        /// <summary>
        /// Permiso que exige la página, o null si alcanza con haber iniciado
        /// sesión. Las páginas con una regla más compleja sobrescriben
        /// <see cref="TieneAcceso"/>.
        /// </summary>
        protected virtual string PermisoRequerido => null;

        protected BE.USUARIO UsuarioActual => Session["Usuario"] as BE.USUARIO;

        protected virtual bool TieneAcceso(BE.USUARIO usuario)
        {
            return PermisoRequerido == null || usuario.TienePermiso(PermisoRequerido);
        }

        protected override void OnInit(EventArgs e)
        {
            // Protege el ViewState contra CSRF atándolo a la sesión. El SessionID es
            // estable porque Login guarda Session["Usuario"] (una sesión con datos
            // no regenera su ID); Login no hereda de esta clase.
            ViewStateUserKey = Session.SessionID;
            base.OnInit(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            BE.USUARIO usuario = UsuarioActual;
            if (usuario == null)
            {
                Response.Redirect("~/Login.aspx", true);
                return;
            }

            // Sesión revocada desde el servidor (baja o restablecimiento de contraseña).
            if (BLL.PerfilBLL.SesionRevocada(usuario))
            {
                Session.Abandon();
                Response.Redirect("~/Login.aspx", true);
                return;
            }

            // Si algún perfil cambió desde que se calcularon los permisos de la sesión, se recalculan.
            if (!new BLL.PerfilBLL().RefrescarPermisosSiHaceFalta(usuario))
            {
                // El usuario fue dado de baja mientras tenía la sesión abierta.
                Session.Abandon();
                Response.Redirect("~/Login.aspx", true);
                return;
            }

            if (!TieneAcceso(usuario))
            {
                string inicio = ObtenerPaginaInicio(usuario);
                // Si el usuario no tiene ninguna página a la que volver (o la
                // página denegada es justo su inicio) se corta con 403 en vez
                // de redirigir en bucle.
                if (inicio == null || Request.AppRelativeCurrentExecutionFilePath
                        .Equals(inicio, StringComparison.OrdinalIgnoreCase))
                    throw new HttpException(403, "No tenés permiso para acceder a esta página.");
                Response.Redirect(inicio, true);
                return;
            }

            base.OnLoad(e);
        }

        /// <summary>
        /// Página de inicio del usuario: la primera de la lista de prioridad
        /// cuyo permiso tenga, o null si no tiene ninguna.
        /// </summary>
        public static string ObtenerPaginaInicio(BE.USUARIO usuario)
        {
            for (int i = 0; i < PaginasInicio.GetLength(0); i++)
                if (usuario.TienePermiso(PaginasInicio[i, 0]))
                    return PaginasInicio[i, 1];
            return null;
        }
    }
}
