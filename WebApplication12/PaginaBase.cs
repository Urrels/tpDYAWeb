using System;
using System.Web;

namespace CAPAS_Web
{
    public class PaginaBase : PaginaConIdioma
    {
        private static readonly string[,] PaginasInicio =
        {
            { BE.Permisos.VerificarIntegridad, "~/Integridad.aspx" },
            { BE.Permisos.GestionarMaterias,   "~/Materias.aspx" },
            { BE.Permisos.VerMenuAlumno,       "~/Menu.aspx" },
            { BE.Permisos.EditarPerfil,        "~/Perfil.aspx" }
        };

        protected virtual string PermisoRequerido => null;

        protected BE.USUARIO UsuarioActual => Session["Usuario"] as BE.USUARIO;

        protected virtual bool TieneAcceso(BE.USUARIO usuario)
        {
            return PermisoRequerido == null || usuario.TienePermiso(PermisoRequerido);
        }

        protected override void OnInit(EventArgs e)
        {
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

            if (BLL.PerfilBLL.SesionRevocada(usuario))
            {
                Session.Abandon();
                Response.Redirect("~/Login.aspx", true);
                return;
            }

            if (!new BLL.PerfilBLL().RefrescarPermisosSiHaceFalta(usuario))
            {
                Session.Abandon();
                Response.Redirect("~/Login.aspx", true);
                return;
            }

            if (!TieneAcceso(usuario))
            {
                string inicio = ObtenerPaginaInicio(usuario);
                if (inicio == null || Request.AppRelativeCurrentExecutionFilePath
                        .Equals(inicio, StringComparison.OrdinalIgnoreCase))
                    throw new HttpException(403, "No tenés permiso para acceder a esta página.");
                Response.Redirect(inicio, true);
                return;
            }

            base.OnLoad(e);
        }

        public static string ObtenerPaginaInicio(BE.USUARIO usuario)
        {
            for (int i = 0; i < PaginasInicio.GetLength(0); i++)
                if (usuario.TienePermiso(PaginasInicio[i, 0]))
                    return PaginasInicio[i, 1];
            return null;
        }
    }
}
