using System.Collections.Generic;
using System.Linq;
using BE;
using DAL;

namespace BLL
{
    public enum LoginResultado
    {
        Exito,
        CredencialesInvalidas,
        UsuarioBloqueado,
        SesionYaActiva,
        UsuarioInactivo
    }

    public class LoginBLL
    {
        private readonly UsuarioDAL _dal = new UsuarioDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();
        private readonly IntegridadBLL _integridad = new IntegridadBLL();

        public LoginResultado AutenticarUsuario(string usuario, string contrasena, string sessionId, out BE.USUARIO usuarioAutenticado)
        {
            usuarioAutenticado = null;
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(contrasena))
                return LoginResultado.CredencialesInvalidas;

            IntegridadBLL.IntentarInicializarDatosBase();

            if (_dal.EstaBloqueado(usuario))
                return LoginResultado.UsuarioBloqueado;

            string hash = HashHelper.HashSHA256(contrasena);
            BE.USUARIO u = _dal.ObtenerPorCredenciales(usuario, hash);

            if (u != null)
            {
                if (!u.Activo)
                {
                    _bitacora.RegistrarAccion(usuario, "LOGIN_USUARIO_INACTIVO");
                    return LoginResultado.UsuarioInactivo;
                }

                _dal.ResetearIntentos(usuario);

                _integridad.RecalcularUsuario();

                if (BE.SessionRegistry.Instancia.TieneSesionActiva(usuario))
                    return LoginResultado.SesionYaActiva;

                u.EpocaSesion = PerfilBLL.NuevaEpocaDeSesion();
                u.VersionPermisos = PerfilBLL.VersionPermisos;
                u.Permisos = new PerfilBLL().ObtenerPermisosEfectivos(u.Id);

                BE.SessionRegistry.Instancia.RegistrarSesion(usuario, sessionId);
                try
                {
                    _bitacora.RegistrarLogin(usuario);
                }
                catch
                {
                    BE.SessionRegistry.Instancia.CerrarSesion(usuario, sessionId);
                    throw;
                }
                usuarioAutenticado = u;
                return LoginResultado.Exito;
            }

            bool quedoBloqueado = _dal.IncrementarIntentos(usuario);
            _integridad.RecalcularUsuario();
            _bitacora.RegistrarAccion(usuario, "LOGIN_FALLIDO");
            if (quedoBloqueado)
            {
                _bitacora.RegistrarAccion(usuario, "USUARIO_BLOQUEADO");
                return LoginResultado.UsuarioBloqueado;
            }
            return LoginResultado.CredencialesInvalidas;
        }
    }

    public class UsuarioBLL
    {
        private readonly UsuarioDAL _dal = new UsuarioDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();
        private readonly IntegridadBLL _integridad = new IntegridadBLL();

        public bool VerificarContrasena(string usuario, string contrasena)
        {
            string hash = HashHelper.HashSHA256(contrasena);
            return _dal.ObtenerPorCredenciales(usuario, hash) != null;
        }

        public bool CambiarContrasena(string usuario, string nuevaContrasena)
        {
            string hash = HashHelper.HashSHA256(nuevaContrasena);
            bool ok = _dal.CambiarContrasena(usuario, hash);
            if (ok)
            {
                _bitacora.RegistrarAccion(usuario, "CAMBIO_CONTRASENA");
                _integridad.RecalcularUsuario();
            }
            return ok;
        }

        public BE.USUARIO ObtenerPerfil(string usuario)
        {
            return _dal.ObtenerPorNombre(usuario);
        }

        public bool ActualizarDireccion(string usuario, string direccion)
        {
            bool ok = _dal.ActualizarDireccion(usuario, direccion);
            if (ok)
            {
                _bitacora.RegistrarAccion(usuario, "ACTUALIZACION_DIRECCION");
                _integridad.RecalcularUsuario();
            }
            return ok;
        }

        public List<BE.USUARIO> ListarTodos() => _dal.ListarTodos();

        public List<BE.USUARIO> ListarAlumnos()
        {
            var ids = new HashSet<int>(new PerfilBLL().ListarUsuariosConPermiso(BE.Permisos.Inscribirse));
            return _dal.ListarTodos().Where(u => ids.Contains(u.Id)).ToList();
        }
    }
}
