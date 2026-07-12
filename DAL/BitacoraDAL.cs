using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class BitacoraDAL
    {
        private readonly Acceso _acceso = new Acceso();

        public void Registrar(string usuario, string accion, string criticidad)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@usuario",    usuario),
                _acceso.CrearParametro("@accion",     accion),
                _acceso.CrearParametro("@criticidad", criticidad)
            };
            try { _acceso.Abrir(); _acceso.Escribir("BITACORA_INSERTAR", p); }
            finally { _acceso.Cerrar(); }
        }

        public List<BE.BITACORA> Listar()
        {
            var lista = new List<BE.BITACORA>();
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("BITACORA_LISTAR");
                foreach (DataRow r in dt.Rows)
                    lista.Add(new BE.BITACORA
                    {
                        Id = System.Convert.ToInt32(r["ID"]),
                        Usuario = r["USUARIO"].ToString(),
                        Accion = r["ACCION"].ToString(),
                        Fecha = System.Convert.ToDateTime(r["FECHA"])
                    });
            }
            finally { _acceso.Cerrar(); }
            return lista;
        }

        public List<string> ListarUsuariosDistinct()
        {
            var lista = new List<string>();
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("BITACORA_LISTAR_USUARIOS_DISTINCT");
                foreach (DataRow r in dt.Rows) lista.Add(r["USUARIO"].ToString());
            }
            finally { _acceso.Cerrar(); }
            return lista;
        }

        public BE.BitacoraPaginaResultado ListarPaginado(string usuario, string criticidad, System.DateTime? fechaDesde, System.DateTime? fechaHasta, int pagina, int tamanio)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@usuario", usuario),       // puede ser null -> sin filtro
                _acceso.CrearParametro("@criticidad", criticidad), // puede ser null -> sin filtro
                _acceso.CrearParametro("@fecha_desde", fechaDesde),
                _acceso.CrearParametro("@fecha_hasta", fechaHasta),
                _acceso.CrearParametro("@pagina", pagina),
                _acceso.CrearParametro("@tamanio", tamanio)
            };
            var resultado = new BE.BitacoraPaginaResultado { Filas = new List<BE.BITACORA>() };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("BITACORA_LISTAR_PAGINADO", p);
                foreach (DataRow r in dt.Rows)
                {
                    resultado.Filas.Add(new BE.BITACORA
                    {
                        Id = System.Convert.ToInt32(r["ID"]),
                        Usuario = r["USUARIO"].ToString(),
                        Accion = r["ACCION"].ToString(),
                        Fecha = System.Convert.ToDateTime(r["FECHA"]),
                        Criticidad = r["CRITICIDAD"].ToString()
                    });
                    resultado.TotalFilas = System.Convert.ToInt32(r["TOTAL_FILAS"]);
                }
            }
            finally { _acceso.Cerrar(); }
            return resultado;
        }
    }
}
