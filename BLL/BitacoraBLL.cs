using System.Collections.Generic;

namespace BLL
{
    public class BitacoraBLL
    {
        private readonly DAL.BitacoraDAL _dal = new DAL.BitacoraDAL();
        private readonly IntegridadBLL _integridad = new IntegridadBLL();

        // BitacoraDAL.Registrar() no devuelve resultado (siempre se asume
        // exitoso si no lanza excepción), por eso el recálculo se dispara
        // incondicionalmente después de cada escritura, igual que el resto
        // del método ya hacía antes de este cambio.
        public void RegistrarLogin(string usuario)
        {
            _dal.Registrar(usuario, "LOGIN");
            _integridad.RecalcularBitacora();
        }

        public void RegistrarLogout(string usuario)
        {
            _dal.Registrar(usuario, "LOGOUT");
            _integridad.RecalcularBitacora();
        }

        public void RegistrarAccion(string usuario, string accion)
        {
            _dal.Registrar(usuario, accion);
            _integridad.RecalcularBitacora();
        }

        public List<BE.BITACORA> Listar() => _dal.Listar();
    }
}
