using BE;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class IdiomaDAL
    {
        private readonly Acceso _acceso = new Acceso();

        public List<IDIOMA> ListarTodos()
        {
            var lista = new List<IDIOMA>();
            try
            {
                _acceso.Abrir();
                DataTable t = _acceso.Leer("IDIOMA_LISTAR_TODOS");
                foreach (DataRow f in t.Rows)
                    lista.Add(MapearIdioma(f));
            }
            finally { _acceso.Cerrar(); }
            return lista;
        }

        public List<IDIOMA> ListarHabilitados()
        {
            var lista = new List<IDIOMA>();
            try
            {
                _acceso.Abrir();
                DataTable t = _acceso.Leer("IDIOMA_LISTAR_HABILITADOS");
                foreach (DataRow f in t.Rows)
                    lista.Add(MapearIdioma(f));
            }
            finally { _acceso.Cerrar(); }
            return lista;
        }

        public IDIOMA ObtenerPorId(int id)
        {
            var p = new List<SqlParameter> { _acceso.CrearParametro("@id", id) };
            try
            {
                _acceso.Abrir();
                DataTable t = _acceso.Leer("IDIOMA_OBTENER_POR_ID", p);
                return t.Rows.Count > 0 ? MapearIdioma(t.Rows[0]) : null;
            }
            finally { _acceso.Cerrar(); }
        }

        public bool EstaEnUso(int id)
        {
            var p = new List<SqlParameter> { _acceso.CrearParametro("@id", id) };
            try
            {
                _acceso.Abrir();
                DataTable t = _acceso.Leer("IDIOMA_ESTA_EN_USO", p);
                return Convert.ToInt32(t.Rows[0]["TOTAL"]) > 0;
            }
            finally { _acceso.Cerrar(); }
        }

        public int Insertar(string nombre, bool habilitado)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@nombre",     nombre),
                _acceso.CrearParametro("@habilitado", habilitado ? 1 : 0)
            };
            try
            {
                _acceso.Abrir();
                DataTable t = _acceso.Leer("IDIOMA_INSERTAR", p);
                return Convert.ToInt32(t.Rows[0]["ID"]);
            }
            finally { _acceso.Cerrar(); }
        }

        public void Renombrar(int id, string nombre)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id",     id),
                _acceso.CrearParametro("@nombre", nombre)
            };
            try
            {
                _acceso.Abrir();
                _acceso.Escribir("IDIOMA_RENOMBRAR", p);
            }
            finally { _acceso.Cerrar(); }
        }

        public void ActualizarEstado(int id, bool habilitado)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id",         id),
                _acceso.CrearParametro("@habilitado", habilitado ? 1 : 0)
            };
            try
            {
                _acceso.Abrir();
                _acceso.Escribir("IDIOMA_ACTUALIZAR_ESTADO", p);
            }
            finally { _acceso.Cerrar(); }
        }

        public void Eliminar(int id)
        {
            var p = new List<SqlParameter> { _acceso.CrearParametro("@id", id) };
            try
            {
                _acceso.Abrir();
                _acceso.Escribir("IDIOMA_ELIMINAR", p);
            }
            finally { _acceso.Cerrar(); }
        }

        public void RegistrarControl(string clave, string textoDefault)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@clave",         clave),
                _acceso.CrearParametro("@texto_default", textoDefault)
            };
            try
            {
                _acceso.Abrir();
                _acceso.Escribir("CONTROL_REGISTRAR", p);
            }
            finally { _acceso.Cerrar(); }
        }

        public Dictionary<string, string> CargarTraducciones(int idiomaId)
        {
            var dic = new Dictionary<string, string>();
            foreach (CONTROL_IDIOMA c in ListarControlesConTraduccion(idiomaId))
            {
                string valor = !string.IsNullOrEmpty(c.TextoTraduccion)
                    ? c.TextoTraduccion
                    : c.TextoDefault;
                if (!string.IsNullOrEmpty(valor))
                    dic[c.Clave] = valor;
            }
            return dic;
        }

        public List<CONTROL_IDIOMA> ListarControlesConTraduccion(int idiomaId)
        {
            var lista = new List<CONTROL_IDIOMA>();
            var p = new List<SqlParameter> { _acceso.CrearParametro("@idioma_id", idiomaId) };
            try
            {
                _acceso.Abrir();
                DataTable t = _acceso.Leer("TRADUCCION_LISTAR_CONTROLES", p);
                foreach (DataRow f in t.Rows)
                    lista.Add(new CONTROL_IDIOMA
                    {
                        Id              = Convert.ToInt32(f["ID"]),
                        Clave           = f["CLAVE"].ToString(),
                        TextoDefault    = f["TEXTO_DEFAULT"].ToString(),
                        TextoTraduccion = f["TEXTO_TRADUCCION"].ToString()
                    });
            }
            finally { _acceso.Cerrar(); }
            return lista;
        }

        public void GuardarTraduccion(int idiomaId, int controlId, string texto)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@idioma_id",  idiomaId),
                _acceso.CrearParametro("@control_id", controlId),
                _acceso.CrearParametro("@texto",      texto)
            };
            try
            {
                _acceso.Abrir();
                _acceso.Escribir("TRADUCCION_GUARDAR", p);
            }
            finally { _acceso.Cerrar(); }
        }

        public IDIOMA ObtenerPredeterminado()
        {
            try
            {
                _acceso.Abrir();
                DataTable t = _acceso.Leer("IDIOMA_OBTENER_PREDETERMINADO");
                return t.Rows.Count > 0 ? MapearIdioma(t.Rows[0]) : null;
            }
            finally { _acceso.Cerrar(); }
        }

        public int? ObtenerIdiomaDeUsuario(int idUsuario)
        {
            var p = new List<SqlParameter> { _acceso.CrearParametro("@id", idUsuario) };
            try
            {
                _acceso.Abrir();
                DataTable t = _acceso.Leer("USUARIO_OBTENER_IDIOMA", p);
                if (t.Rows.Count == 0 || t.Rows[0]["IDIOMA_ID"] == DBNull.Value) return null;
                return Convert.ToInt32(t.Rows[0]["IDIOMA_ID"]);
            }
            finally { _acceso.Cerrar(); }
        }

        public void ActualizarIdiomaDeUsuario(int idUsuario, int idiomaId)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id",        idUsuario),
                _acceso.CrearParametro("@idioma_id", idiomaId)
            };
            try
            {
                _acceso.Abrir();
                _acceso.Escribir("USUARIO_ACTUALIZAR_IDIOMA", p);
            }
            finally { _acceso.Cerrar(); }
        }

        private IDIOMA MapearIdioma(DataRow f) => new IDIOMA
        {
            Id             = Convert.ToInt32(f["ID"]),
            Nombre         = f["NOMBRE"].ToString(),
            Habilitado     = Convert.ToBoolean(f["HABILITADO"]),
            Predeterminado = Convert.ToBoolean(f["PREDETERMINADO"])
        };
    }
}
