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

        /// <summary>
        /// Valida las credenciales. El usuario autenticado se devuelve por
        /// <paramref name="usuarioAutenticado"/> (solo con <see cref="LoginResultado.Exito"/>):
        /// no hay estado compartido entre pedidos, así dos ingresos simultáneos
        /// nunca pueden cruzarse el usuario.
        /// </summary>
        public LoginResultado AutenticarUsuario(string usuario, string contrasena, string sessionId, out BE.USUARIO usuarioAutenticado)
        {
            usuarioAutenticado = null;
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(contrasena))
                return LoginResultado.CredencialesInvalidas;

            // Reintenta (si hace falta) la siembra de datos base. Va primero: en una
            // base nueva donde falló al iniciar la aplicación todavía no existen los
            // usuarios iniciales, y sin este reintento nadie podría ingresar.
            IntegridadBLL.IntentarInicializarDatosBase();

            // Se chequea el bloqueo ANTES de validar credenciales: no gasta el
            // hash ni le da al atacante ninguna pista sobre si la contraseña
            // ingresada era correcta.
            if (_dal.EstaBloqueado(usuario))
                return LoginResultado.UsuarioBloqueado;

            string hash = HashHelper.HashSHA256(contrasena);
            BE.USUARIO u = _dal.ObtenerPorCredenciales(usuario, hash);

            if (u != null)
            {
                // Baja lógica: credenciales correctas pero cuenta dada de baja.
                if (!u.Activo)
                {
                    _bitacora.RegistrarAccion(usuario, "LOGIN_USUARIO_INACTIVO");
                    return LoginResultado.UsuarioInactivo;
                }

                _dal.ResetearIntentos(usuario);

                _integridad.RecalcularUsuario();

                if (BE.SessionRegistry.Instancia.TieneSesionActiva(usuario))
                    return LoginResultado.SesionYaActiva;

                // Los permisos efectivos se resuelven una sola vez por sesión,
                // recorriendo el árbol de perfiles (Composite) en memoria. Se
                // cargan ANTES de registrar la sesión: si fallan, el usuario no
                // queda bloqueado con "sesión ya activa".
                // La versión se lee antes de calcular: si un perfil cambia en el medio,
                // la sesión queda desactualizada y se refresca en el próximo pedido.
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

        /// <summary>
        /// Alumno es una identidad de dominio, no un perfil puntual: se
        /// considera alumno a quien tiene el permiso de inscribirse a
        /// materias (es quien genera cursadas), sea por el perfil Alumno o
        /// por cualquier perfil que lo contenga.
        /// </summary>
        public List<BE.USUARIO> ListarAlumnos()
        {
            var ids = new HashSet<int>(new PerfilBLL().ListarUsuariosConPermiso(BE.Permisos.Inscribirse));
            return _dal.ListarTodos().Where(u => ids.Contains(u.Id)).ToList();
        }
    }
}
