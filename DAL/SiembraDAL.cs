using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>Lo que falta sembrar: solo filas que todavía no existen.</summary>
    public class PlanSiembra
    {
        public List<string> Permisos = new List<string>();
        // Nombre del perfil y si es protegido.
        public List<KeyValuePair<string, bool>> Roles = new List<KeyValuePair<string, bool>>();
        // Pares (perfil, permiso) por nombre.
        public List<KeyValuePair<string, string>> RolPermisos = new List<KeyValuePair<string, string>>();
        public List<UsuarioSembrado> Usuarios = new List<UsuarioSembrado>();
        // Si se aplican los perfiles pendientes de la migración USUARIO.ROL -> USUARIO_PERFIL.
        public bool IncluirMigracionDePerfiles;
        // Si se registra el marcador de siembra en la bitácora, dentro de la misma transacción.
        public bool EscribirMarcador;
        // Tablas que estaban íntegras antes de sembrar: si reciben filas se les descarta
        // el DVV y el snapshot dentro de la misma transacción (la BLL las recalcula luego).
        public HashSet<string> TablasIntegras = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public class UsuarioSembrado
    {
        public string Usuario;
        public string PassHash;
        public string Rol;
    }

    public class ResultadoSiembra
    {
        public int Permisos, Roles, RolPermisos, Usuarios, UsuarioPerfiles;
        public bool Marcador;
        public bool Algo => Permisos + Roles + RolPermisos + Usuarios + UsuarioPerfiles > 0;
    }

    /// <summary>
    /// Aplica la siembra de datos base en una única transacción, solo con
    /// procedimientos almacenados. La decisión de qué falta la toma la BLL.
    /// </summary>
    public class SiembraDAL
    {
        // Acción de bitácora que marca que la siembra inicial ya se realizó.
        public const string AccionMarcador = "SIEMBRA_DATOS_BASE";

        private readonly Acceso _acceso = new Acceso();

        /// <summary>true si la bitácora ya tiene la entrada que marca la siembra inicial.</summary>
        public bool ExisteMarcadorSiembra()
        {
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("BITACORA_EXISTE_ACCION",
                    new List<SqlParameter> { _acceso.CrearParametro("@accion", AccionMarcador) });
                return Convert.ToInt32(dt.Rows[0]["EXISTE"]) == 1;
            }
            finally { _acceso.Cerrar(); }
        }

        public ResultadoSiembra Aplicar(PlanSiembra plan)
        {
            var resultado = new ResultadoSiembra();
            try
            {
                _acceso.Abrir();
                _acceso.IniciarTx();
                try
                {
                    foreach (string nombre in plan.Permisos)
                    {
                        _acceso.EscribirConRetorno("PERMISO_INSERTAR",
                            new List<SqlParameter> { _acceso.CrearParametro("@nombre", nombre) });
                        resultado.Permisos++;
                    }
                    foreach (var rol in plan.Roles)
                    {
                        _acceso.EscribirConRetorno("ROL_INSERTAR", new List<SqlParameter>
                        {
                            _acceso.CrearParametro("@nombre", rol.Key),
                            _acceso.CrearParametro("@padre_id", (int?)null),
                            new SqlParameter("@protegido", SqlDbType.Bit) { Value = rol.Value }
                        });
                        resultado.Roles++;
                    }

                    Dictionary<string, int> idsPermiso = MapaIds("PERMISO_LISTAR_TODOS", "NOMBRE");
                    Dictionary<string, int> idsRol = MapaIds("ROL_LISTAR_TODOS", "NOMBRE");

                    foreach (var par in plan.RolPermisos)
                    {
                        if (!idsRol.TryGetValue(par.Key, out int rolId) || !idsPermiso.TryGetValue(par.Value, out int permisoId))
                            continue;
                        _acceso.Escribir("ROL_PERMISO_INSERTAR", new List<SqlParameter>
                        {
                            _acceso.CrearParametro("@rol_id", rolId),
                            _acceso.CrearParametro("@permiso_id", permisoId)
                        });
                        resultado.RolPermisos++;
                    }

                    foreach (UsuarioSembrado u in plan.Usuarios)
                    {
                        if (!idsRol.TryGetValue(u.Rol, out int rolId)) continue;
                        int usuarioId = Convert.ToInt32(_acceso.EscribirConRetorno("USUARIO_INSERTAR", new List<SqlParameter>
                        {
                            _acceso.CrearParametro("@usuario", u.Usuario),
                            _acceso.CrearParametro("@pass", u.PassHash),
                            _acceso.CrearParametro("@direccion", (byte[])null)
                        }));
                        AsignarPerfil(usuarioId, rolId);
                        resultado.Usuarios++;
                        resultado.UsuarioPerfiles++;
                    }

                    // Perfiles pendientes de la migración USUARIO.ROL -> USUARIO_PERFIL
                    // (los anotó el script); se asignan solo a quien todavía no tiene ninguno.
                    DataTable pendientes = plan.IncluirMigracionDePerfiles
                        ? _acceso.Leer("MIGRACION_USUARIO_ROL_LISTAR")
                        : new DataTable();
                    foreach (DataRow fila in pendientes.Rows)
                    {
                        int usuarioId = Convert.ToInt32(fila["USUARIO_ID"]);
                        string rolNombre = fila["ROL_NOMBRE"].ToString();
                        if (!idsRol.TryGetValue(rolNombre, out int rolId))
                        {
                            // El perfil anotado ya no existe: la fila no se puede resolver nunca,
                            // se descarta para que no quede pendiente para siempre.
                            _acceso.Escribir("MIGRACION_USUARIO_ROL_ELIMINAR",
                                new List<SqlParameter> { _acceso.CrearParametro("@usuario_id", usuarioId) });
                            continue;
                        }

                        DataTable actuales = _acceso.Leer("USUARIO_PERFIL_LISTAR_POR_USUARIO",
                            new List<SqlParameter> { _acceso.CrearParametro("@usuario_id", usuarioId) });
                        if (actuales.Rows.Count == 0)
                        {
                            AsignarPerfil(usuarioId, rolId);
                            resultado.UsuarioPerfiles++;
                        }
                        _acceso.Escribir("MIGRACION_USUARIO_ROL_ELIMINAR",
                            new List<SqlParameter> { _acceso.CrearParametro("@usuario_id", usuarioId) });
                    }

                    // El marcador se escribe en la misma transacción: si un recálculo posterior
                    // falla, igual queda registrado. El DVH lo calcula la BLL después del commit.
                    if (plan.EscribirMarcador)
                    {
                        _acceso.Escribir("BITACORA_INSERTAR", new List<SqlParameter>
                        {
                            _acceso.CrearParametro("@usuario", "SISTEMA"),
                            _acceso.CrearParametro("@accion", AccionMarcador),
                            _acceso.CrearParametro("@criticidad", "Media")
                        });
                        resultado.Marcador = true;
                    }

                    // Si el recálculo posterior (fuera de esta transacción) fallara, la tabla
                    // queda sin DVV ni snapshot: la aplicación la recalcula en el próximo intento
                    // del mismo proceso; si el proceso se reinicia antes, queda inválida hasta que
                    // el Webmaster la recalcule desde Integridad.aspx.
                    InvalidarSiInserto(plan, "PERMISO", resultado.Permisos);
                    InvalidarSiInserto(plan, "ROL", resultado.Roles);
                    InvalidarSiInserto(plan, "ROL_PERMISO", resultado.RolPermisos);
                    InvalidarSiInserto(plan, "USUARIO", resultado.Usuarios);
                    InvalidarSiInserto(plan, "USUARIO_PERFIL", resultado.UsuarioPerfiles);
                    InvalidarSiInserto(plan, "BITACORA", resultado.Marcador ? 1 : 0);

                    _acceso.ConfirmarTX();
                }
                catch
                {
                    _acceso.DeshacerTX();
                    throw;
                }
            }
            finally { _acceso.Cerrar(); }
            return resultado;
        }

        private void InvalidarSiInserto(PlanSiembra plan, string tabla, int insertadas)
        {
            if (insertadas > 0 && plan.TablasIntegras.Contains(tabla))
                _acceso.Escribir("INTEGRIDAD_INVALIDAR_TABLA",
                    new List<SqlParameter> { _acceso.CrearParametro("@tabla", tabla) });
        }

        private void AsignarPerfil(int usuarioId, int rolId)
        {
            _acceso.Escribir("USUARIO_PERFIL_ASIGNAR", new List<SqlParameter>
            {
                _acceso.CrearParametro("@usuario_id", usuarioId),
                _acceso.CrearParametro("@rol_id", rolId)
            });
        }

        // nombre -> id, leído dentro de la transacción en curso (sin distinguir mayúsculas).
        private Dictionary<string, int> MapaIds(string sp, string columnaNombre)
        {
            var mapa = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in _acceso.Leer(sp).Rows)
                mapa[r[columnaNombre].ToString()] = Convert.ToInt32(r["ID"]);
            return mapa;
        }
    }
}
