using System;
using System.Collections.Generic;

namespace BLL
{
    public class BitacoraBLL
    {
        private readonly DAL.BitacoraDAL _dal = new DAL.BitacoraDAL();
        private readonly IntegridadBLL _integridad = new IntegridadBLL();

        // Deriva la criticidad únicamente a partir del string de acción, así
        // ningún caller existente de RegistrarAccion(usuario, accion) necesita
        // cambiar su firma.
        private static string ClasificarCriticidad(string accion)
        {
            if (accion.StartsWith("BAJA_") || accion == "USUARIO_BLOQUEADO" || accion == "LOGIN_FALLIDO"
                || accion.StartsWith("DESBLOQUEO_USUARIO") || accion == "CAMBIO_CONTRASENA"
                || accion.StartsWith("RESET_CONTRASENA") || accion.StartsWith("REACTIVAR_USUARIO")
                || accion == "LOGIN_USUARIO_INACTIVO"
                || accion.StartsWith("ASIGNACION_PERFIL") || accion.StartsWith("CAMBIO_JERARQUIA_PERFIL")
                || accion.StartsWith("RESTAURACION_") || accion == "RECALCULO_INTEGRIDAD")
                return "Alta";
            if (accion == "LOGIN" || accion == "LOGOUT")
                return "Baja";
            return "Media"; // ALTA_*, EDICION_*, MODIF_*, ACTUALIZACION_*, INSCRIPCION_*, y cualquier acción futura no clasificada
        }

        // BitacoraDAL.Registrar() no devuelve resultado (siempre se asume
        // exitoso si no lanza excepción), por eso el recálculo se dispara
        // incondicionalmente después de cada escritura, igual que el resto
        // del método ya hacía antes de este cambio.
        public void RegistrarLogin(string usuario)
        {
            _dal.Registrar(usuario, "LOGIN", ClasificarCriticidad("LOGIN"));
            _integridad.RecalcularBitacora();
        }

        public void RegistrarLogout(string usuario)
        {
            _dal.Registrar(usuario, "LOGOUT", ClasificarCriticidad("LOGOUT"));
            _integridad.RecalcularBitacora();
        }

        public void RegistrarAccion(string usuario, string accion)
        {
            _dal.Registrar(usuario, accion, ClasificarCriticidad(accion));
            _integridad.RecalcularBitacora();
        }

        public BE.PaginaResultado<BE.BITACORA> ListarPaginado(string usuario, string criticidad, DateTime? fechaDesde, DateTime? fechaHasta, int pagina, int tamanio)
            => Paginacion.ConsultarAjustando(pagina,
                p => _dal.ListarPaginado(usuario, criticidad, fechaDesde, fechaHasta, p, tamanio));

        public List<string> ListarUsuariosDistinct() => _dal.ListarUsuariosDistinct();
    }
}
