using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class MateriaDAL
    {
        private readonly Acceso _acceso = new Acceso();

        public int Insertar(BE.MATERIA m)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@nombre",    m.Nombre),
                _acceso.CrearParametro("@codigo",    m.Codigo),
                _acceso.CrearParametro("@modalidad", m.Modalidad),
                _acceso.CrearParametro("@peso",      m.Peso)
            };
            try
            {
                _acceso.Abrir();
                object id = _acceso.EscribirConRetorno("MATERIA_INSERTAR", p);
                return id != null ? System.Convert.ToInt32(id) : -1;
            }
            finally { _acceso.Cerrar(); }
        }

        public bool Actualizar(BE.MATERIA m)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id",        m.Id),
                _acceso.CrearParametro("@nombre",    m.Nombre),
                _acceso.CrearParametro("@codigo",    m.Codigo),
                _acceso.CrearParametro("@modalidad", m.Modalidad),
                _acceso.CrearParametro("@peso",      m.Peso)
            };
            try { _acceso.Abrir(); return _acceso.Escribir("MATERIA_ACTUALIZAR", p) > 0; }
            finally { _acceso.Cerrar(); }
        }

        public bool Eliminar(int id)
        {
            var p = new List<SqlParameter> { _acceso.CrearParametro("@id", id) };
            try { _acceso.Abrir(); return _acceso.Escribir("MATERIA_ELIMINAR", p) > 0; }
            finally { _acceso.Cerrar(); }
        }

        public List<BE.MATERIA> Listar()
        {
            var lista = new List<BE.MATERIA>();
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("MATERIA_LISTAR");
                foreach (DataRow r in dt.Rows)
                    lista.Add(MapearFila(r));
            }
            finally { _acceso.Cerrar(); }
            return lista;
        }

        public BE.MATERIA Obtener(int id)
        {
            var p = new List<SqlParameter> { _acceso.CrearParametro("@id", id) };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("MATERIA_OBTENER", p);
                return dt.Rows.Count > 0 ? MapearFila(dt.Rows[0]) : null;
            }
            finally { _acceso.Cerrar(); }
        }

        public void InsertarCorrelativa(int idMateria, int idCorrelativa)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id_materia",     idMateria),
                _acceso.CrearParametro("@id_correlativa", idCorrelativa)
            };
            try { _acceso.Abrir(); _acceso.Escribir("CORRELATIVA_INSERTAR", p); }
            finally { _acceso.Cerrar(); }
        }

        public void EliminarCorrelativa(int idMateria, int idCorrelativa)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id_materia",     idMateria),
                _acceso.CrearParametro("@id_correlativa", idCorrelativa)
            };
            try { _acceso.Abrir(); _acceso.Escribir("CORRELATIVA_ELIMINAR", p); }
            finally { _acceso.Cerrar(); }
        }

        public List<BE.MATERIA> ListarCorrelativas(int idMateria)
        {
            var lista = new List<BE.MATERIA>();
            var p = new List<SqlParameter> { _acceso.CrearParametro("@id_materia", idMateria) };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("CORRELATIVA_LISTAR", p);
                foreach (DataRow r in dt.Rows)
                    lista.Add(new BE.MATERIA
                    {
                        Id = System.Convert.ToInt32(r["ID"]),
                        Nombre = r["NOMBRE"].ToString(),
                        Codigo = r["CODIGO"].ToString()
                    });
            }
            finally { _acceso.Cerrar(); }
            return lista;
        }

        private BE.MATERIA MapearFila(DataRow r)
        {
            return new BE.MATERIA
            {
                Id = System.Convert.ToInt32(r["ID"]),
                Nombre = r["NOMBRE"].ToString(),
                Codigo = r["CODIGO"].ToString(),
                Modalidad = r["MODALIDAD"].ToString(),
                Peso = System.Convert.ToInt32(r["PESO"])
            };
        }
    }
}
