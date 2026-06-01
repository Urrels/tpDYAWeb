using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class UsuarioDAL
    {
        private readonly Acceso _acceso = new Acceso();

        public BE.USUARIO ObtenerPorCredenciales(string usuario, string passHash)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@usuario", usuario),
                _acceso.CrearParametro("@pass",    passHash)
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
                    Usuario = r["USUARIO"].ToString(),
                    Rol     = r["ROL"].ToString()
                };
            }
            finally { _acceso.Cerrar(); }
        }

        public bool CambiarContrasena(string usuario, string nuevaPassHash)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@usuario", usuario),
                _acceso.CrearParametro("@pass",    nuevaPassHash)
            };
            try
            {
                _acceso.Abrir();
                return _acceso.Escribir("USUARIO_CAMBIAR_PASS", p) > 0;
            }
            finally { _acceso.Cerrar(); }
        }

        public BE.USUARIO ObtenerPorNombre(string usuario)
        {
            var p = new List<SqlParameter> { _acceso.CrearParametro("@usuario", usuario) };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("USUARIO_OBTENER", p);
                if (dt.Rows.Count == 0) return null;
                DataRow r = dt.Rows[0];
                var u = new BE.USUARIO
                {
                    Id      = System.Convert.ToInt32(r["ID"]),
                    Usuario = r["USUARIO"].ToString(),
                    Rol     = r["ROL"].ToString()
                };
                if (r["DIRECCION"] != System.DBNull.Value)
                {
                    byte[] encriptado = (byte[])r["DIRECCION"];
                    u.DireccionVisible = BE.AESHelper.Desencriptar(encriptado);
                }
                return u;
            }
            finally { _acceso.Cerrar(); }
        }

        public List<BE.USUARIO> ListarAlumnos()
        {
            var lista = new List<BE.USUARIO>();
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("USUARIO_LISTAR_ALUMNOS");
                foreach (DataRow r in dt.Rows)
                    lista.Add(new BE.USUARIO
                    {
                        Id      = System.Convert.ToInt32(r["ID"]),
                        Usuario = r["USUARIO"].ToString(),
                        Rol     = "Alumno"
                    });
            }
            finally { _acceso.Cerrar(); }
            return lista;
        }

        public bool ActualizarDireccion(string usuario, string direccion)
        {
            byte[] encriptado = BE.AESHelper.Encriptar(direccion);
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@usuario",   usuario),
                _acceso.CrearParametro("@direccion", encriptado)
            };
            try
            {
                _acceso.Abrir();
                return _acceso.Escribir("USUARIO_ACTUALIZAR_DIRECCION", p) > 0;
            }
            finally { _acceso.Cerrar(); }
        }
    }
}
