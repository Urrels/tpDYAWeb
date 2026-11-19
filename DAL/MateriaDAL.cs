using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class MateriaDAL
    {
        private readonly Acceso _acceso = new Acceso();

        public const string MensajeCodigoDuplicado = "El código de materia ya existe.";

        private static bool EsClaveDuplicada(SqlException ex) => ex.Number == 2627 || ex.Number == 2601;

        private static byte[] Activa => BE.AESHelper.EncriptarBool(true);

        internal static List<SqlParameter> ParametrosAlta(Acceso acceso, BE.MATERIA m) => new List<SqlParameter>
        {
            acceso.CrearParametro("@nombre",    BE.AESHelper.Encriptar(m.Nombre)),
            acceso.CrearParametro("@codigo",    BE.AESHelper.Encriptar(BE.Normalizador.CodigoMateria(m.Codigo))),
            acceso.CrearParametro("@modalidad", BE.AESHelper.Encriptar(m.Modalidad)),
            acceso.CrearParametro("@peso",      BE.AESHelper.EncriptarEntero(m.Peso)),
            acceso.CrearParametro("@activa",    Activa)
        };

        internal static List<SqlParameter> ParametrosCambio(Acceso acceso, BE.MATERIA m) => new List<SqlParameter>
        {
            acceso.CrearParametro("@id",        m.Id),
            acceso.CrearParametro("@nombre",    BE.AESHelper.Encriptar(m.Nombre)),
            acceso.CrearParametro("@codigo",    BE.AESHelper.Encriptar(BE.Normalizador.CodigoMateria(m.Codigo))),
            acceso.CrearParametro("@modalidad", BE.AESHelper.Encriptar(m.Modalidad)),
            acceso.CrearParametro("@peso",      BE.AESHelper.EncriptarEntero(m.Peso))
        };

        private List<SqlParameter> SoloActivas() => new List<SqlParameter> { _acceso.CrearParametro("@activa", Activa) };

        public List<BE.MATERIA> ListarTodasConInactivas()
        {
            var lista = new List<BE.MATERIA>();
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("MATERIA_LISTAR_PARA_INTEGRIDAD");
                foreach (DataRow r in dt.Rows)
                    lista.Add(MapearFila(r));
            }
            finally { _acceso.Cerrar(); }
            return lista;
        }

        public int Insertar(BE.MATERIA m)
        {
            var p = ParametrosAlta(_acceso, m);
            try
            {
                _acceso.Abrir();
                object id = _acceso.EscribirConRetorno("MATERIA_INSERTAR", p);
                return id != null ? System.Convert.ToInt32(id) : -1;
            }
            catch (SqlException ex) when (EsClaveDuplicada(ex))
            {
                throw new System.InvalidOperationException(MensajeCodigoDuplicado);
            }
            finally { _acceso.Cerrar(); }
        }

        public bool Actualizar(BE.MATERIA m)
        {
            var p = ParametrosCambio(_acceso, m);
            try
            {
                _acceso.Abrir();
                object resultado = _acceso.EscribirConRetorno("MATERIA_ACTUALIZAR", p);
                return System.Convert.ToInt32(resultado) > 0;
            }
            catch (SqlException ex) when (EsClaveDuplicada(ex))
            {
                throw new System.InvalidOperationException(MensajeCodigoDuplicado);
            }
            finally { _acceso.Cerrar(); }
        }

        public bool Eliminar(int id)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id", id),
                _acceso.CrearParametro("@activa", BE.AESHelper.EncriptarBool(false))
            };
            try
            {
                _acceso.Abrir();
                object resultado = _acceso.EscribirConRetorno("MATERIA_ELIMINAR", p);
                return System.Convert.ToInt32(resultado) > 0;
            }
            finally { _acceso.Cerrar(); }
        }

        public List<BE.MATERIA> Listar()
        {
            var lista = new List<BE.MATERIA>();
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("MATERIA_LISTAR", SoloActivas());
                foreach (DataRow r in dt.Rows)
                    lista.Add(MapearFila(r));
            }
            finally { _acceso.Cerrar(); }
            return lista.OrderBy(m => m.Id).ToList();
        }

        public BE.PaginaResultado<BE.MATERIA> ListarPaginado(int pagina, int tamanio)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@pagina", pagina),
                _acceso.CrearParametro("@tamanio", tamanio),
                _acceso.CrearParametro("@activa", Activa)
            };
            var resultado = new BE.PaginaResultado<BE.MATERIA> { Pagina = pagina, Tamanio = tamanio };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("MATERIA_LISTAR_PAGINADO", p);
                foreach (DataRow r in dt.Rows)
                {
                    resultado.Filas.Add(MapearFila(r));
                    resultado.TotalFilas = System.Convert.ToInt32(r["TOTAL_FILAS"]);
                }
            }
            finally { _acceso.Cerrar(); }
            return resultado;
        }

        public BE.MATERIA Obtener(int id)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id", id),
                _acceso.CrearParametro("@activa", Activa)
            };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("MATERIA_OBTENER", p);
                return dt.Rows.Count > 0 ? MapearFila(dt.Rows[0]) : null;
            }
            finally { _acceso.Cerrar(); }
        }

        public List<BE.MATERIA> ListarCorrelativas(int idMateria)
        {
            var lista = new List<BE.MATERIA>();
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id_materia", idMateria),
                _acceso.CrearParametro("@activa", Activa)
            };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("CORRELATIVA_LISTAR", p);
                foreach (DataRow r in dt.Rows)
                    lista.Add(new BE.MATERIA
                    {
                        Id = System.Convert.ToInt32(r["ID"]),
                        Nombre = BE.AESHelper.DesencriptarTexto(r["NOMBRE"]),
                        Codigo = BE.AESHelper.DesencriptarTexto(r["CODIGO"])
                    });
            }
            finally { _acceso.Cerrar(); }
            return lista;
        }

        public void Importar(List<BE.MATERIA> materias, Dictionary<string, int> idPorCodigo,
                             List<KeyValuePair<string, string>> correlativas)
        {
            try
            {
                _acceso.Abrir();
                _acceso.IniciarTx();
                try
                {
                    foreach (var m in materias)
                    {
                        if (m.Id == 0)
                        {
                            idPorCodigo[m.Codigo] = System.Convert.ToInt32(
                                _acceso.EscribirConRetorno("MATERIA_INSERTAR", ParametrosAlta(_acceso, m)));
                        }
                        else
                        {
                            _acceso.EscribirConRetorno("MATERIA_ACTUALIZAR", ParametrosCambio(_acceso, m));
                        }
                    }

                    foreach (var par in correlativas)
                    {
                        var pc = new List<SqlParameter>
                        {
                            _acceso.CrearParametro("@id_materia",     idPorCodigo[par.Key]),
                            _acceso.CrearParametro("@id_correlativa", idPorCodigo[par.Value])
                        };
                        _acceso.Escribir("CORRELATIVA_INSERTAR", pc);
                    }
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

        private BE.MATERIA MapearFila(DataRow r)
        {
            return new BE.MATERIA
            {
                Id = System.Convert.ToInt32(r["ID"]),
                Nombre = BE.AESHelper.DesencriptarTexto(r["NOMBRE"]),
                Codigo = BE.AESHelper.DesencriptarTexto(r["CODIGO"]),
                Modalidad = BE.AESHelper.DesencriptarTexto(r["MODALIDAD"]),
                Peso = BE.AESHelper.DesencriptarEntero(r["PESO"])
            };
        }
    }
}
