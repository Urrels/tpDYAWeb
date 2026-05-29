using System.Collections.Generic;

namespace BLL
{
    public class BitacoraBLL
    {
        private readonly DAL.BitacoraDAL _dal = new DAL.BitacoraDAL();

        public void RegistrarLogin(string usuario) => _dal.Registrar(usuario, "LOGIN");
        public void RegistrarLogout(string usuario) => _dal.Registrar(usuario, "LOGOUT");
        public void RegistrarAccion(string usuario, string accion) => _dal.Registrar(usuario, accion);
        public List<BE.BITACORA> Listar() => _dal.Listar();
    }
}
