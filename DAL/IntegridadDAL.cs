using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Capa fina sobre <see cref="Acceso"/> para el mecanismo genérico de
    /// integridad (DVH por fila / DVV por columna) de las 9 tablas de negocio.
    /// Los nombres de tabla usados como clave (@tabla) son los mismos nombres
    /// físicos de la tabla en SQL Server (USUARIO, ALUMNO_MATERIA, etc.).
    /// </summary>
    public class IntegridadDAL
    {
        private readonly Acceso _acceso = new Acceso();

        private static readonly Dictionary<string, string> SpListar = new Dictionary<string, string>
        {
            { "USUARIO",              "USUARIO_LISTAR_PARA_INTEGRIDAD" },
            { "BITACORA",             "BITACORA_LISTAR_PARA_INTEGRIDAD" },
            { "MATERIA",              "MATERIA_LISTAR_PARA_INTEGRIDAD" },
            { "CORRELATIVA",          "CORRELATIVA_LISTAR_PARA_INTEGRIDAD" },
            { "ALUMNO_MATERIA",       "ALUMNO_MATERIA_LISTAR_PARA_INTEGRIDAD" },
            { "EVENTO_ACADEMICO",     "EVENTO_ACADEMICO_LISTAR_PARA_INTEGRIDAD" },
            { "PERIODO_ACADEMICO",    "PERIODO_ACADEMICO_LISTAR_PARA_INTEGRIDAD" },
            { "INSCRIPCION",          "INSCRIPCION_LISTAR_PARA_INTEGRIDAD" },
            { "INSCRIPCION_DETALLE",  "INSCRIPCION_DETALLE_LISTAR_PARA_INTEGRIDAD" }
        };

        private static readonly Dictionary<string, string> SpActualizarDvh = new Dictionary<string, string>
        {
            { "USUARIO",              "USUARIO_ACTUALIZAR_DVH" },
            { "BITACORA",             "BITACORA_ACTUALIZAR_DVH" },
            { "MATERIA",              "MATERIA_ACTUALIZAR_DVH" },
            { "CORRELATIVA",          "CORRELATIVA_ACTUALIZAR_DVH" },
            { "ALUMNO_MATERIA",       "ALUMNO_MATERIA_ACTUALIZAR_DVH" },
            { "EVENTO_ACADEMICO",     "EVENTO_ACADEMICO_ACTUALIZAR_DVH" },
            { "PERIODO_ACADEMICO",    "PERIODO_ACADEMICO_ACTUALIZAR_DVH" },
            { "INSCRIPCION",          "INSCRIPCION_ACTUALIZAR_DVH" },
            { "INSCRIPCION_DETALLE",  "INSCRIPCION_DETALLE_ACTUALIZAR_DVH" }
        };

        private static string SpDe(Dictionary<string, string> mapa, string tabla)
        {
            if (!mapa.TryGetValue(tabla, out string sp))
                throw new ArgumentException($"Tabla no soportada por el mecanismo de integridad: {tabla}", nameof(tabla));
            return sp;
        }

        public DataTable ListarParaIntegridad(string tabla)
        {
            string sp = SpDe(SpListar, tabla);
            try
            {
                _acceso.Abrir();
                return _acceso.Leer(sp);
            }
            finally { _acceso.Cerrar(); }
        }

        public void ActualizarDVH(string tabla, int id, int dvh)
        {
            string sp = SpDe(SpActualizarDvh, tabla);
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id",  id),
                _acceso.CrearParametro("@dvh", dvh)
            };
            try { _acceso.Abrir(); _acceso.Escribir(sp, p); }
            finally { _acceso.Cerrar(); }
        }

        public Dictionary<string, int> ObtenerDVV(string tabla)
        {
            var resultado = new Dictionary<string, int>();
            var p = new List<SqlParameter> { _acceso.CrearParametro("@tabla", tabla) };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("DVV_LISTAR", p);
                foreach (DataRow r in dt.Rows)
                    resultado[r["COLUMNA"].ToString()] = Convert.ToInt32(r["DVV"]);
            }
            finally { _acceso.Cerrar(); }
            return resultado;
        }

        public void ActualizarDVV(string tabla, string columna, int dvv)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@tabla",   tabla),
                _acceso.CrearParametro("@columna", columna),
                _acceso.CrearParametro("@dvv",     dvv)
            };
            try { _acceso.Abrir(); _acceso.Escribir("DVV_ACTUALIZAR", p); }
            finally { _acceso.Cerrar(); }
        }
    }
}
