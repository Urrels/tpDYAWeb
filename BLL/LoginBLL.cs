using System.Collections.Generic;
using BE;
using DAL;

namespace BLL
{
    public enum LoginResultado
    {
        Exito,
        CredencialesInvalidas,
        UsuarioBloqueado,
        SesionYaActiva
    }

    public class LoginBLL
    {
        private readonly UsuarioDAL _dal = new UsuarioDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();
        private readonly IntegridadBLL _integridad = new IntegridadBLL();

        public LoginResultado AutenticarUsuario(string usuario, string contrasena, string sessionId)
        {
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(contrasena))
                return LoginResultado.CredencialesInvalidas;

            // Se chequea el bloqueo ANTES de validar credenciales: no gasta el
            // hash ni le da al atacante ninguna pista sobre si la contraseña
            // ingresada era correcta.
            if (_dal.EstaBloqueado(usuario))
                return LoginResultado.UsuarioBloqueado;

            string hash = HashHelper.HashSHA256(contrasena);
            BE.USUARIO u = _dal.ObtenerPorCredenciales(usuario, hash);

            if (u != null)
            {
                _dal.ResetearIntentos(usuario);
                _integridad.RecalcularUsuario();

                if (BE.SessionRegistry.Instancia.TieneSesionActiva(usuario))
                    return LoginResultado.SesionYaActiva;

                BE.SessionRegistry.Instancia.RegistrarSesion(usuario, sessionId);

                BE.SessionManager.getInstane().setUsuario(u);
                _bitacora.RegistrarLogin(usuario);
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

        /// <summary>
        /// Acción de Admin: desbloquea una cuenta bloqueada por intentos
        /// fallidos. El proyecto usa Session["Usuario"] de ASP.NET (no un
        /// SessionManager persistente entre requests) como mecanismo real de
        /// sesión, por eso el admin que ejecuta la acción se recibe como
        /// parámetro desde la página en vez de leerse acá.
        /// </summary>
        public void Desbloquear(string usuario, string admin)
        {
            _dal.Desbloquear(usuario);
            _bitacora.RegistrarAccion(admin, "DESBLOQUEO_USUARIO:" + usuario);
            _integridad.RecalcularUsuario();
        }

        public List<BE.USUARIO> ListarBloqueados() => _dal.ListarBloqueados();
    }
}
