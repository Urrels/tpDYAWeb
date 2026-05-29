using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class BitacoraDAL
    {
        private readonly Acceso _acceso = new Acceso();

        public void Registrar(string usuario, string accion)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@usuario", usuario),
                _acceso.CrearParametro("@accion",  accion)
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
    }
}
