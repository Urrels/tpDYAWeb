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

        // 2627 / 2601: violación de restricción o índice único (UQ_ROL_NOMBRE).
        // La base decide qué nombres son iguales según su collation.
        private static bool EsNombreDuplicado(SqlException ex) => ex.Number == 2627 || ex.Number == 2601;

        public List<BE.Permiso> ListarPermisosDisponibles()
        {
            var lista = new List<BE.Permiso>();
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("PERMISO_LISTAR_TODOS");
                foreach (DataRow r in dt.Rows)
                    lista.Add(new BE.Permiso { Id = Convert.ToInt32(r["ID"]), Nombre = r["NOMBRE"].ToString() });
            }
            finally { _acceso.Cerrar(); }
            return lista;
        }

        /// <summary>Todas las asignaciones usuario-perfil como pares (usuarioId, rolId).</summary>
        public List<KeyValuePair<int, int>> ListarAsignaciones()
        {
            var lista = new List<KeyValuePair<int, int>>();
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("USUARIO_PERFIL_LISTAR_TODOS");
                foreach (DataRow r in dt.Rows)
                    lista.Add(new KeyValuePair<int, int>(Convert.ToInt32(r["USUARIO_ID"]), Convert.ToInt32(r["ROL_ID"])));
            }
            finally { _acceso.Cerrar(); }
            return lista;
        }

        public int InsertarRol(string nombre, int? padreId)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@nombre",   nombre),
                _acceso.CrearParametro("@padre_id", padreId)
            };
            try
            {
                _acceso.Abrir();
                return Convert.ToInt32(_acceso.EscribirConRetorno("ROL_INSERTAR", p));
            }
            catch (SqlException ex) when (EsNombreDuplicado(ex))
            {
                throw new InvalidOperationException("Ya existe un perfil con ese nombre.", ex);
            }
            finally { _acceso.Cerrar(); }
        }

        public void RenombrarRol(int rolId, string nombre)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id",     rolId),
                _acceso.CrearParametro("@nombre", nombre)
            };
            try { _acceso.Abrir(); _acceso.Escribir("ROL_RENOMBRAR", p); }
            catch (SqlException ex) when (EsNombreDuplicado(ex))
            {
                throw new InvalidOperationException("Ya existe un perfil con ese nombre.", ex);
            }
            finally { _acceso.Cerrar(); }
        }

        public void EliminarRol(int rolId)
        {
            var p = new List<SqlParameter> { _acceso.CrearParametro("@id", rolId) };
            try { _acceso.Abrir(); _acceso.Escribir("ROL_ELIMINAR", p); }
            finally { _acceso.Cerrar(); }
        }

        /// <summary>
        /// Reemplaza los permisos directos del rol (borra los actuales e inserta
        /// los nuevos) dentro de una única transacción.
        /// </summary>
        public void GuardarPermisosDeRol(int rolId, List<int> permisoIds)
        {
            try
            {
                _acceso.Abrir();
                _acceso.IniciarTx();
                try
                {
                    _acceso.Escribir("ROL_PERMISO_ELIMINAR_POR_ROL",
                        new List<SqlParameter> { _acceso.CrearParametro("@rol_id", rolId) });
                    foreach (int permisoId in permisoIds)
                        _acceso.Escribir("ROL_PERMISO_INSERTAR", new List<SqlParameter>
                        {
                            _acceso.CrearParametro("@rol_id",     rolId),
                            _acceso.CrearParametro("@permiso_id", permisoId)
                        });
                    _acceso.ConfirmarTX();
                }
                catch
                {
                    _acceso.DeshacerTX();
                    throw;
                }
            }
            finally { _acceso.Cerrar(); }
        }

        /// <summary>
        /// Reemplaza los perfiles asignados al usuario dentro de una única transacción.
        /// </summary>
        public void ReemplazarPerfilesDeUsuario(int usuarioId, List<int> rolIds)
        {
            try
            {
                _acceso.Abrir();
                _acceso.IniciarTx();
                try
                {
                    _acceso.Escribir("USUARIO_PERFIL_ELIMINAR_POR_USUARIO",
                        new List<SqlParameter> { _acceso.CrearParametro("@usuario_id", usuarioId) });
                    foreach (int rolId in rolIds)
                        _acceso.Escribir("USUARIO_PERFIL_ASIGNAR", new List<SqlParameter>
                        {
                            _acceso.CrearParametro("@usuario_id", usuarioId),
                            _acceso.CrearParametro("@rol_id",     rolId)
                        });
                    _acceso.ConfirmarTX();
                }
                catch
                {
                    _acceso.DeshacerTX();
                    throw;
                }
            }
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
