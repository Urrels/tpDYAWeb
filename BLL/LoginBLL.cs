using BE;
using DAL;

namespace BLL
{
    public class LoginBLL
    {
        private readonly UsuarioDAL _dal = new UsuarioDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();

        public bool AutenticarUsuario(string usuario, string contrasena)
        {
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(contrasena))
                return false;

            string hash = HashHelper.HashSHA256(contrasena);
            BE.USUARIO u = _dal.ObtenerPorCredenciales(usuario, hash);

            if (u != null)
            {
                BE.SessionManager.getInstane().setUsuario(u);
                _bitacora.RegistrarLogin(usuario);
                return true;
            }
            return false;
        }
    }

    public class UsuarioBLL
    {
        private readonly UsuarioDAL _dal = new UsuarioDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();

        public bool VerificarContrasena(string usuario, string contrasena)
        {
            string hash = HashHelper.HashSHA256(contrasena);
            return _dal.ObtenerPorCredenciales(usuario, hash) != null;
        }

        public bool CambiarContrasena(string usuario, string nuevaContrasena)
        {
            string hash = HashHelper.HashSHA256(nuevaContrasena);
            bool ok = _dal.CambiarContrasena(usuario, hash);
            if (ok) _bitacora.RegistrarAccion(usuario, "CAMBIO_CONTRASENA");
            return ok;
        }

        public BE.USUARIO ObtenerPerfil(string usuario)
        {
            return _dal.ObtenerPorNombre(usuario);
        }

        /// <summary>RF11 - guarda dirección encriptada con AES</summary>
        public bool ActualizarDireccion(string usuario, string direccion)
        {
            bool ok = _dal.ActualizarDireccion(usuario, direccion);
            if (ok) _bitacora.RegistrarAccion(usuario, "ACTUALIZACION_DIRECCION");
            return ok;
        }
    }
}
