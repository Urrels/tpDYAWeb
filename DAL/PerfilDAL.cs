using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class PerfilDAL
    {
        private readonly Acceso _acceso = new Acceso();

        /// <summary>Todos los roles, sin hijos cargados (el árbol se arma en la BLL).</summary>
        public List<BE.Rol> ListarRoles()
        {
            var lista = new List<BE.Rol>();
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("ROL_LISTAR_TODOS");
                foreach (DataRow r in dt.Rows)
                    lista.Add(new BE.Rol
                    {
                        Id        = Convert.ToInt32(r["ID"]),
                        Nombre    = r["NOMBRE"].ToString(),
                        PadreId   = r["PADRE_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["PADRE_ID"]),
                        Protegido = Convert.ToBoolean(r["PROTEGIDO"])
                    });
            }
            finally { _acceso.Cerrar(); }
            return lista;
        }

        /// <summary>
        /// Asignaciones rol-permiso como hojas del Composite: cada Permiso
        /// devuelto lleva en PadreId el rol al que pertenece.
        /// </summary>
        public List<BE.Permiso> ListarPermisosPorRol()
        {
            var lista = new List<BE.Permiso>();
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("ROL_PERMISO_LISTAR_TODOS");
                foreach (DataRow r in dt.Rows)
                    lista.Add(new BE.Permiso
                    {
                        Id      = Convert.ToInt32(r["PERMISO_ID"]),
                        Nombre  = r["NOMBRE"].ToString(),
                        PadreId = Convert.ToInt32(r["ROL_ID"])
                    });
            }
            finally { _acceso.Cerrar(); }
            return lista;
        }

        public List<int> ListarRolesDeUsuario(int usuarioId)
        {
            var ids = new List<int>();
            var p = new List<SqlParameter> { _acceso.CrearParametro("@usuario_id", usuarioId) };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("USUARIO_PERFIL_LISTAR_POR_USUARIO", p);
                foreach (DataRow r in dt.Rows)
                    ids.Add(Convert.ToInt32(r["ROL_ID"]));
            }
            finally { _acceso.Cerrar(); }
            return ids;
        }

        public void AsignarPerfil(int usuarioId, int rolId)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@usuario_id", usuarioId),
                _acceso.CrearParametro("@rol_id",     rolId)
            };
            try { _acceso.Abrir(); _acceso.Escribir("USUARIO_PERFIL_ASIGNAR", p); }
            finally { _acceso.Cerrar(); }
        }

        public void CambiarPadre(int rolId, int? padreId)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id",       rolId),
                _acceso.CrearParametro("@padre_id", padreId)
            };
            try { _acceso.Abrir(); _acceso.Escribir("ROL_CAMBIAR_PADRE", p); }
            finally { _acceso.Cerrar(); }
        }
    }
}
