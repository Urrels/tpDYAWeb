using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace DAL
{
    public class PeriodoDAL
    {
        private readonly Acceso _acceso = new Acceso();

        public const string MensajePeriodoDuplicado = "Ya existe un período para ese año y cuatrimestre.";

        private static bool EsClaveDuplicada(SqlException ex) => ex.Number == 2627 || ex.Number == 2601;

        private List<SqlParameter> Parametros(BE.PERIODO_ACADEMICO p) => new List<SqlParameter>
        {
            _acceso.CrearParametro("@anio",         BE.AESHelper.EncriptarEntero(p.Anio)),
            _acceso.CrearParametro("@cuatrimestre", BE.AESHelper.EncriptarEntero(p.Cuatrimestre)),
            _acceso.CrearParametro("@descripcion",  BE.AESHelper.Encriptar(p.Descripcion)),
            _acceso.CrearParametro("@fecha_inicio", BE.AESHelper.EncriptarFecha(p.FechaInicio?.Date)),
            _acceso.CrearParametro("@fecha_fin",    BE.AESHelper.EncriptarFecha(p.FechaFin?.Date))
        };

        public List<BE.PERIODO_ACADEMICO> Listar()
        {
            var lista = new List<BE.PERIODO_ACADEMICO>();
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("PERIODO_LISTAR");
                foreach (DataRow r in dt.Rows)
                    lista.Add(Mapear(r));
            }
            finally { _acceso.Cerrar(); }
            return lista.OrderByDescending(x => x.Anio).ThenByDescending(x => x.Cuatrimestre).ToList();
        }

        public int Insertar(BE.PERIODO_ACADEMICO p)
        {
            var pars = Parametros(p);
            try
            {
                _acceso.Abrir();
                object id = _acceso.EscribirConRetorno("PERIODO_INSERTAR", pars);
                return id != null ? Convert.ToInt32(id) : -1;
            }
            catch (SqlException ex) when (EsClaveDuplicada(ex))
            {
                throw new InvalidOperationException(MensajePeriodoDuplicado);
            }
            finally { _acceso.Cerrar(); }
        }

        public bool Actualizar(BE.PERIODO_ACADEMICO p)
        {
            var pars = Parametros(p);
            pars.Insert(0, _acceso.CrearParametro("@id", p.Id));
            try
            {
                _acceso.Abrir();
                object resultado = _acceso.EscribirConRetorno("PERIODO_ACTUALIZAR", pars);
                return Convert.ToInt32(resultado) > 0;
            }
            catch (SqlException ex) when (EsClaveDuplicada(ex))
            {
                throw new InvalidOperationException(MensajePeriodoDuplicado);
            }
            finally { _acceso.Cerrar(); }
        }

        public BE.PERIODO_ACADEMICO ObtenerActual()
        {
            DateTime hoy = DateTime.Today;
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("PERIODO_OBTENER_ACTUAL");
                return dt.Rows.Cast<DataRow>().Select(Mapear)
                    .Where(x => x.FechaInicio.HasValue && x.FechaFin.HasValue
                                && x.FechaInicio.Value.Date <= hoy && hoy <= x.FechaFin.Value.Date)
                    .OrderByDescending(x => x.Anio).ThenByDescending(x => x.Cuatrimestre)
                    .FirstOrDefault();
            }
            finally { _acceso.Cerrar(); }
        }

        private BE.PERIODO_ACADEMICO Mapear(DataRow r)
        {
            return new BE.PERIODO_ACADEMICO
            {
                Id            = Convert.ToInt32(r["ID"]),
                Anio          = BE.AESHelper.DesencriptarEntero(r["ANIO"]),
                Cuatrimestre  = BE.AESHelper.DesencriptarEntero(r["CUATRIMESTRE"]),
                Descripcion   = BE.AESHelper.DesencriptarTexto(r["DESCRIPCION"]) ?? "",
                FechaInicio   = BE.AESHelper.DesencriptarFecha(r["FECHA_INICIO"]),
                FechaFin      = BE.AESHelper.DesencriptarFecha(r["FECHA_FIN"])
            };
        }
    }

    public class InscripcionDAL
    {
        private readonly Acceso _acceso = new Acceso();

        public int InsertarCabecera(int idUsuario, int idPeriodo)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id_usuario", idUsuario),
                _acceso.CrearParametro("@id_periodo", idPeriodo),
                _acceso.CrearParametro("@fecha_inscripcion", BE.AESHelper.EncriptarFecha(DateTime.Now))
            };
            try
            {
                _acceso.Abrir();
                object id = _acceso.EscribirConRetorno("INSCRIPCION_INSERTAR", p);
                return id != null ? Convert.ToInt32(id) : -1;
            }
            finally { _acceso.Cerrar(); }
        }

        public void InsertarDetalle(int idInscripcion, int idMateria)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id_inscripcion", idInscripcion),
                _acceso.CrearParametro("@id_materia",     idMateria)
            };
            try { _acceso.Abrir(); _acceso.Escribir("INSCRIPCION_DETALLE_INSERTAR", p); }
            finally { _acceso.Cerrar(); }
        }

        public int ObtenerIdPorPeriodo(int idUsuario, int idPeriodo)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id_usuario", idUsuario),
                _acceso.CrearParametro("@id_periodo", idPeriodo)
            };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("INSCRIPCION_OBTENER_ID", p);
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 0;
            }
            finally { _acceso.Cerrar(); }
        }

        public bool ExisteEnPeriodo(int idUsuario, int idPeriodo)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id_usuario", idUsuario),
                _acceso.CrearParametro("@id_periodo", idPeriodo)
            };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("INSCRIPCION_EXISTE", p);
                return dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0][0]) > 0;
            }
            finally { _acceso.Cerrar(); }
        }

        public List<BE.INSCRIPCION> ListarPorUsuario(int idUsuario)
        {
            var lista = new List<BE.INSCRIPCION>();
            var p = new List<SqlParameter> { _acceso.CrearParametro("@id_usuario", idUsuario) };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("INSCRIPCION_LISTAR_USUARIO", p);
                foreach (DataRow r in dt.Rows)
                {
                    int idInscripcion = Convert.ToInt32(r["ID_INSCRIPCION"]);
                    var insc = lista.Find(x => x.Id == idInscripcion);
                    if (insc == null)
                    {
                        insc = new BE.INSCRIPCION
                        {
                            Id               = idInscripcion,
                            IdUsuario        = idUsuario,
                            IdPeriodo        = Convert.ToInt32(r["ID_PERIODO"]),
                            FechaInscripcion = BE.AESHelper.DesencriptarFecha(r["FECHA_INSCRIPCION"]) ?? DateTime.MinValue,
                            EtiquetaPeriodo  = new BE.PERIODO_ACADEMICO
                            {
                                Anio         = BE.AESHelper.DesencriptarEntero(r["ANIO_PERIODO"]),
                                Cuatrimestre = BE.AESHelper.DesencriptarEntero(r["CUATRIMESTRE_PERIODO"])
                            }.Etiqueta
                        };
                        lista.Add(insc);
                    }
                    if (r["ID_MATERIA"] != DBNull.Value)
                        insc.Materias.Add(new BE.MATERIA
                        {
                            Id     = Convert.ToInt32(r["ID_MATERIA"]),
                            Nombre = BE.AESHelper.DesencriptarTexto(r["NOMBRE_MATERIA"]),
                            Codigo = BE.AESHelper.DesencriptarTexto(r["CODIGO_MATERIA"])
                        });
                }
            }
            finally { _acceso.Cerrar(); }
            foreach (var insc in lista)
                insc.Materias = insc.Materias.OrderBy(m => m.Nombre, StringComparer.CurrentCultureIgnoreCase).ToList();
            return lista;
        }
    }
}
