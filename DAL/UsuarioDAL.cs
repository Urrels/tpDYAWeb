using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace DAL
{
    public class UsuarioDAL
    {
        private readonly Acceso _acceso = new Acceso();

        private static byte[] CifrarUsuario(string usuario) =>
            BE.AESHelper.Encriptar(BE.Normalizador.Usuario(usuario));

        internal static List<SqlParameter> ParametrosAlta(Acceso acceso, string usuario, string passHash, byte[] direccionCifrada) =>
            new List<SqlParameter>
            {
                acceso.CrearParametro("@usuario",   CifrarUsuario(usuario)),
                acceso.CrearParametro("@pass",      BE.AESHelper.Encriptar(passHash)),
                acceso.CrearParametro("@direccion", direccionCifrada),
                acceso.CrearParametro("@activo",    BE.AESHelper.EncriptarBool(true)),
                acceso.CrearParametro("@intentos",  BE.AESHelper.EncriptarEntero(0)),
                acceso.CrearParametro("@bloqueado", BE.AESHelper.EncriptarBool(false))
            };

        public BE.USUARIO ObtenerPorCredenciales(string usuario, string passHash)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@usuario", CifrarUsuario(usuario)),
                _acceso.CrearParametro("@pass",    BE.AESHelper.Encriptar(passHash))
            };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("USUARIO_LOGIN", p);
                if (dt.Rows.Count == 0) return null;
                DataRow r = dt.Rows[0];
                return new BE.USUARIO
                {
                    Id      = System.Convert.ToInt32(r["ID"]),
                    Usuario = BE.AESHelper.DesencriptarTexto(r["USUARIO"]),
                    Activo  = BE.AESHelper.DesencriptarBool(r["ACTIVO"])
                };
            }
            finally { _acceso.Cerrar(); }
        }

        public bool CambiarContrasena(string usuario, string nuevaPassHash)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@usuario", CifrarUsuario(usuario)),
                _acceso.CrearParametro("@pass",    BE.AESHelper.Encriptar(nuevaPassHash))
            };
            try
            {
                _acceso.Abrir();
                object resultado = _acceso.EscribirConRetorno("USUARIO_CAMBIAR_PASS", p);
                return System.Convert.ToInt32(resultado) > 0;
            }
            finally { _acceso.Cerrar(); }
        }

        public BE.USUARIO ObtenerPorNombre(string usuario)
        {
            var p = new List<SqlParameter> { _acceso.CrearParametro("@usuario", CifrarUsuario(usuario)) };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("USUARIO_OBTENER", p);
                if (dt.Rows.Count == 0) return null;
                DataRow r = dt.Rows[0];
                var u = new BE.USUARIO
                {
                    Id      = System.Convert.ToInt32(r["ID"]),
                    Usuario = BE.AESHelper.DesencriptarTexto(r["USUARIO"])
                };
                if (r["DIRECCION"] != System.DBNull.Value)
                    u.DireccionVisible = BE.AESHelper.DesencriptarTexto(r["DIRECCION"]);
                return u;
            }
            finally { _acceso.Cerrar(); }
        }

        public List<BE.USUARIO> ListarTodos()
        {
            var lista = new List<BE.USUARIO>();
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("USUARIO_LISTAR_TODOS");
                foreach (DataRow r in dt.Rows)
                    lista.Add(new BE.USUARIO
                    {
                        Id        = System.Convert.ToInt32(r["ID"]),
                        Usuario   = BE.AESHelper.DesencriptarTexto(r["USUARIO"]),
                        Bloqueado = BE.AESHelper.DesencriptarBool(r["BLOQUEADO"]),
                        Activo    = BE.AESHelper.DesencriptarBool(r["ACTIVO"])
                    });
            }
            finally { _acceso.Cerrar(); }
            return lista.OrderBy(u => u.Usuario, System.StringComparer.OrdinalIgnoreCase).ToList();
        }

        public int InsertarConPerfiles(string usuario, string passHash, string direccion, List<int> rolIds)
        {
            byte[] encriptado = string.IsNullOrEmpty(direccion) ? null : BE.AESHelper.Encriptar(direccion);
            try
            {
                _acceso.Abrir();
                _acceso.IniciarTx();
                try
                {
                    object id = _acceso.EscribirConRetorno("USUARIO_INSERTAR", ParametrosAlta(_acceso, usuario, passHash, encriptado));
                    int usuarioId = System.Convert.ToInt32(id);
                    foreach (int rolId in rolIds)
                        _acceso.Escribir("USUARIO_PERFIL_ASIGNAR", new List<SqlParameter>
                        {
                            _acceso.CrearParametro("@usuario_id", usuarioId),
                            _acceso.CrearParametro("@rol_id",     rolId)
                        });
                    _acceso.ConfirmarTX();
                    return usuarioId;
                }
                catch
                {
                    _acceso.DeshacerTX();
                    throw;
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                throw new System.InvalidOperationException("Ya existe un usuario con ese nombre.", ex);
            }
            finally { _acceso.Cerrar(); }
        }

        public void ActualizarEstado(int id, bool activo)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id", id),
                _acceso.CrearParametro("@activo", BE.AESHelper.EncriptarBool(activo))
            };
            try { _acceso.Abrir(); _acceso.Escribir("USUARIO_ACTUALIZAR_ESTADO", p); }
            finally { _acceso.Cerrar(); }
        }

        public bool ActualizarDireccion(string usuario, string direccion)
        {
            byte[] encriptado = BE.AESHelper.Encriptar(direccion);
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@usuario",   CifrarUsuario(usuario)),
                _acceso.CrearParametro("@direccion", encriptado)
            };
            try
            {
                _acceso.Abrir();
                object resultado = _acceso.EscribirConRetorno("USUARIO_ACTUALIZAR_DIRECCION", p);
                return System.Convert.ToInt32(resultado) > 0;
            }
            finally { _acceso.Cerrar(); }
        }

        public bool EstaBloqueado(string usuario)
        {
            var p = new List<SqlParameter> { _acceso.CrearParametro("@usuario", CifrarUsuario(usuario)) };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("USUARIO_VERIFICAR_BLOQUEO", p);
                return dt.Rows.Count == 0 ? false : BE.AESHelper.DesencriptarBool(dt.Rows[0]["BLOQUEADO"]);
            }
            finally { _acceso.Cerrar(); }
        }

        public bool IncrementarIntentos(string usuario)
        {
            var pLectura = new List<SqlParameter> { _acceso.CrearParametro("@usuario", CifrarUsuario(usuario)) };
            try
            {
                _acceso.Abrir();
                _acceso.IniciarTx();
                try
                {
                    DataTable dt = _acceso.Leer("USUARIO_OBTENER_ESTADO_LOGIN", pLectura);
                    if (dt.Rows.Count == 0)
                    {
                        _acceso.ConfirmarTX();
                        return false;
                    }
                    BE.PoliticaBloqueo.Siguiente(
                        BE.AESHelper.DesencriptarEntero(dt.Rows[0]["INTENTOS_FALLIDOS"]),
                        BE.AESHelper.DesencriptarBool(dt.Rows[0]["BLOQUEADO"]),
                        out int intentos, out bool bloqueado);
                    _acceso.Escribir("USUARIO_GUARDAR_ESTADO_LOGIN", new List<SqlParameter>
                    {
                        _acceso.CrearParametro("@usuario",   CifrarUsuario(usuario)),
                        _acceso.CrearParametro("@intentos",  BE.AESHelper.EncriptarEntero(intentos)),
                        _acceso.CrearParametro("@bloqueado", BE.AESHelper.EncriptarBool(bloqueado))
                    });
                    _acceso.ConfirmarTX();
                    return bloqueado;
                }
                catch
                {
                    _acceso.DeshacerTX();
                    throw;
                }
            }
            finally { _acceso.Cerrar(); }
        }

        public void ResetearIntentos(string usuario)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@usuario",  CifrarUsuario(usuario)),
                _acceso.CrearParametro("@intentos", BE.AESHelper.EncriptarEntero(0))
            };
            try { _acceso.Abrir(); _acceso.Escribir("USUARIO_RESETEAR_INTENTOS", p); }
            finally { _acceso.Cerrar(); }
        }

        public void Desbloquear(string usuario)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@usuario",   CifrarUsuario(usuario)),
                _acceso.CrearParametro("@bloqueado", BE.AESHelper.EncriptarBool(false)),
                _acceso.CrearParametro("@intentos",  BE.AESHelper.EncriptarEntero(0))
            };
            try { _acceso.Abrir(); _acceso.Escribir("USUARIO_DESBLOQUEAR", p); }
            finally { _acceso.Cerrar(); }
        }

        public List<BE.USUARIO> ListarBloqueados()
        {
            var lista = new List<BE.USUARIO>();
            var p = new List<SqlParameter> { _acceso.CrearParametro("@bloqueado", BE.AESHelper.EncriptarBool(true)) };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("USUARIO_LISTAR_BLOQUEADOS", p);
                foreach (DataRow r in dt.Rows)
                    lista.Add(new BE.USUARIO
                    {
                        Id               = System.Convert.ToInt32(r["ID"]),
                        Usuario          = BE.AESHelper.DesencriptarTexto(r["USUARIO"]),
                        IntentosFallidos = BE.AESHelper.DesencriptarEntero(r["INTENTOS_FALLIDOS"])
                    });
            }
            finally { _acceso.Cerrar(); }
            return lista.OrderBy(u => u.Usuario, System.StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
