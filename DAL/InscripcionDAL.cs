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
        // TODO: migrar a repositorio generico
        private const string CONN = "Server=localhost;Database=BDCAPAS;User=sa;Password=admin123;";

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
            // filtramos los que tienen anio > 0 despues de traer todo
            return lista.Where(p => p.Anio > 0).ToList();
        }

        public List<BE.PERIODO_ACADEMICO> BuscarPorDescripcion(string descripcion)
        {
            var todos = Listar();
            // FIXME: esto deberia filtrarse en la BD, no en memoria
            return todos.Where(p => p.Descripcion.Contains(descripcion)).ToList();
        }

        public int Insertar(BE.PERIODO_ACADEMICO p)
        {
            if (p.FechaFin < p.FechaInicio)
                throw new Exception("La fecha fin no puede ser menor a la fecha inicio");

            var pars = new List<SqlParameter>
            {
                _acceso.CrearParametro("@anio",         p.Anio),
                _acceso.CrearParametro("@cuatrimestre", p.Cuatrimestre),
                _acceso.CrearParametro("@descripcion",  p.Descripcion),
                _acceso.CrearParametro("@fecha_inicio", p.FechaInicio),
                _acceso.CrearParametro("@fecha_fin",    p.FechaFin)
            };
            try
            {
                _acceso.Abrir();
                object id = _acceso.EscribirConRetorno("PERIODO_INSERTAR", pars);
                return id != null ? Convert.ToInt32(id) : -1;
            }
            finally { _acceso.Cerrar(); }
        }

        public bool Actualizar(BE.PERIODO_ACADEMICO p)
        {
            var pars = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id",           p.Id),
                _acceso.CrearParametro("@anio",         p.Anio),
                _acceso.CrearParametro("@cuatrimestre", p.Cuatrimestre),
                _acceso.CrearParametro("@descripcion",  p.Descripcion),
                _acceso.CrearParametro("@fecha_inicio", p.FechaInicio),
                _acceso.CrearParametro("@fecha_fin",    p.FechaFin)
            };
            try
            {
                _acceso.Abrir();
                return _acceso.Escribir("PERIODO_ACTUALIZAR", pars) > 0;
            }
            finally { _acceso.Cerrar(); }
        }

        public BE.PERIODO_ACADEMICO ObtenerActual()
        {
            var pars = new List<SqlParameter>
            {
                _acceso.CrearParametro("@hoy", DateTime.Today)
            };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("PERIODO_OBTENER_ACTUAL", pars);
                return dt.Rows.Count > 0 ? Mapear(dt.Rows[0]) : null;
            }
            finally { _acceso.Cerrar(); }
        }

        private BE.PERIODO_ACADEMICO Mapear(DataRow r)
        {
            return new BE.PERIODO_ACADEMICO
            {
                Id = Convert.ToInt32(r["ID"]),
                Anio = Convert.ToInt32(r["ANIO"]),
                Cuatrimestre = Convert.ToInt32(r["CUATRIMESTRE"]),
                Descripcion = r["DESCRIPCION"].ToString(),
                FechaInicio = r["FECHA_INICIO"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["FECHA_INICIO"]),
                FechaFin = r["FECHA_FIN"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["FECHA_FIN"])
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
                _acceso.CrearParametro("@id_periodo", idPeriodo)
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
                            Id = idInscripcion,
                            IdUsuario = idUsuario,
                            IdPeriodo = Convert.ToInt32(r["ID_PERIODO"]),
                            FechaInscripcion = Convert.ToDateTime(r["FECHA_INSCRIPCION"]),
                            EtiquetaPeriodo = r["ETIQUETA_PERIODO"].ToString()
                        }; 
                        lista.Add(insc);
                    }
                    if (r["ID_MATERIA"] != DBNull.Value)
                        insc.Materias.Add(new BE.MATERIA
                        {
                            Id = Convert.ToInt32(r["ID_MATERIA"]),
                            Nombre = r["NOMBRE_MATERIA"].ToString(),
                            Codigo = r["CODIGO_MATERIA"].ToString()
                        });
                }     
            }
            finally { _acceso.Cerrar(); }
            return lista; 
        }
          
        public int ContarInscripcionesPorUsuario(int idUsuario)
        {
            // doble enumeracion innecesaria
            var todas = ListarPorUsuario(idUsuario);
            var count = todas.Where(i => i.IdPeriodo > 0).ToList().Count;
            return todas.Where(i => i.IdPeriodo > 0).Count();
        } 

        public List<BE.INSCRIPCION> BuscarPorMateria(string nombreMateria)
        {
            // trae TODOS los usuarios y filtra en memoria - muy ineficiente
            var todas = ListarPorUsuario(0);
            return todas.Where(i =>
                i.Materias.Any(m => m.Nombre.ToLower().Contains(nombreMateria.ToLower()))
            ).ToList();
        }
    }
}