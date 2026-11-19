using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class PlanSiembra
    {
        public List<string> Permisos = new List<string>();
        public List<KeyValuePair<string, bool>> Roles = new List<KeyValuePair<string, bool>>();
        public List<KeyValuePair<string, string>> RolPermisos = new List<KeyValuePair<string, string>>();
        public List<UsuarioSembrado> Usuarios = new List<UsuarioSembrado>();
        public List<BE.MATERIA> Materias = new List<BE.MATERIA>();
        public List<KeyValuePair<string, string>> Correlativas = new List<KeyValuePair<string, string>>();
        public bool EscribirMarcador;
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
        public int Permisos, Roles, RolPermisos, Usuarios, UsuarioPerfiles, Materias, Correlativas;
        public bool Marcador;
        public bool Algo => Permisos + Roles + RolPermisos + Usuarios + UsuarioPerfiles + Materias + Correlativas > 0;
    }

    public class SiembraDAL
    {
        public const string AccionMarcador = "SIEMBRA_DATOS_BASE";

        private readonly Acceso _acceso = new Acceso();

        public bool ExisteMarcadorSiembra()
        {
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("BITACORA_EXISTE_ACCION",
                    new List<SqlParameter> { _acceso.CrearParametro("@accion", BE.AESHelper.Encriptar(AccionMarcador)) });
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
                            new List<SqlParameter> { _acceso.CrearParametro("@nombre", BE.AESHelper.Encriptar(nombre)) });
                        resultado.Permisos++;
                    }
                    foreach (var rol in plan.Roles)
                    {
                        _acceso.EscribirConRetorno("ROL_INSERTAR", PerfilDAL.ParametrosRol(_acceso, rol.Key, null, rol.Value));
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
                        int usuarioId = Convert.ToInt32(_acceso.EscribirConRetorno("USUARIO_INSERTAR",
                            UsuarioDAL.ParametrosAlta(_acceso, u.Usuario, u.PassHash, null)));
                        AsignarPerfil(usuarioId, rolId);
                        resultado.Usuarios++;
                        resultado.UsuarioPerfiles++;
                    }

                    var idPorCodigo = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    foreach (BE.MATERIA m in plan.Materias)
                    {
                        idPorCodigo[m.Codigo] = Convert.ToInt32(
                            _acceso.EscribirConRetorno("MATERIA_INSERTAR", MateriaDAL.ParametrosAlta(_acceso, m)));
                        resultado.Materias++;
                    }
                    foreach (var par in plan.Correlativas)
                    {
                        _acceso.Escribir("CORRELATIVA_INSERTAR", new List<SqlParameter>
                        {
                            _acceso.CrearParametro("@id_materia", idPorCodigo[par.Key]),
                            _acceso.CrearParametro("@id_correlativa", idPorCodigo[par.Value])
                        });
                        resultado.Correlativas++;
                    }

                    if (plan.EscribirMarcador)
                    {
                        _acceso.Escribir("BITACORA_INSERTAR", BitacoraDAL.ParametrosAlta(_acceso, "SISTEMA", AccionMarcador, "Media"));
                        resultado.Marcador = true;
                    }

                    InvalidarSiInserto(plan, "PERMISO", resultado.Permisos);
                    InvalidarSiInserto(plan, "ROL", resultado.Roles);
                    InvalidarSiInserto(plan, "ROL_PERMISO", resultado.RolPermisos);
                    InvalidarSiInserto(plan, "USUARIO", resultado.Usuarios);
                    InvalidarSiInserto(plan, "USUARIO_PERFIL", resultado.UsuarioPerfiles);
                    InvalidarSiInserto(plan, "BITACORA", resultado.Marcador ? 1 : 0);
                    InvalidarSiInserto(plan, "MATERIA", resultado.Materias);
                    InvalidarSiInserto(plan, "CORRELATIVA", resultado.Correlativas);

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

        private Dictionary<string, int> MapaIds(string sp, string columnaNombre)
        {
            var mapa = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in _acceso.Leer(sp).Rows)
                mapa[BE.AESHelper.DesencriptarTexto(r[columnaNombre])] = Convert.ToInt32(r["ID"]);
            return mapa;
        }
    }
}
