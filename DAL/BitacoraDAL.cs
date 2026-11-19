using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace DAL
{
    public class BitacoraDAL
    {
        private readonly Acceso _acceso = new Acceso();

        internal static List<SqlParameter> ParametrosAlta(Acceso acceso, string usuario, string accion, string criticidad) =>
            new List<SqlParameter>
            {
                acceso.CrearParametro("@usuario",    BE.AESHelper.Encriptar(usuario)),
                acceso.CrearParametro("@accion",     BE.AESHelper.Encriptar(accion)),
                acceso.CrearParametro("@fecha",      BE.AESHelper.EncriptarFecha(DateTime.Now)),
                acceso.CrearParametro("@criticidad", BE.AESHelper.Encriptar(criticidad))
            };

        private static BE.BITACORA Mapear(DataRow r) => new BE.BITACORA
        {
            Id = Convert.ToInt32(r["ID"]),
            Usuario = BE.AESHelper.DesencriptarTexto(r["USUARIO"]),
            Accion = BE.AESHelper.DesencriptarTexto(r["ACCION"]),
            Fecha = BE.AESHelper.DesencriptarFecha(r["FECHA"]) ?? DateTime.MinValue,
            Criticidad = BE.AESHelper.DesencriptarTexto(r["CRITICIDAD"])
        };

        public void Registrar(string usuario, string accion, string criticidad)
        {
            var p = ParametrosAlta(_acceso, usuario, accion, criticidad);
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
                foreach (DataRow r in dt.Rows) lista.Add(Mapear(r));
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
                foreach (DataRow r in dt.Rows) lista.Add(BE.AESHelper.DesencriptarTexto(r["USUARIO"]));
            }
            finally { _acceso.Cerrar(); }
            return lista.Distinct().OrderBy(u => u, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        public static BE.PaginaResultado<BE.BITACORA> Paginar(IEnumerable<BE.BITACORA> filas,
            DateTime? fechaDesde, DateTime? fechaHasta, int pagina, int tamanio)
        {
            if (pagina < 1) pagina = 1;
            if (tamanio < 1) tamanio = 50;
            List<BE.BITACORA> filtradas = filas
                .Where(b => (!fechaDesde.HasValue || b.Fecha >= fechaDesde.Value)
                         && (!fechaHasta.HasValue || b.Fecha < fechaHasta.Value))
                .OrderByDescending(b => b.Id)
                .ToList();
            return new BE.PaginaResultado<BE.BITACORA>
            {
                Pagina = pagina,
                Tamanio = tamanio,
                TotalFilas = filtradas.Count,
                Filas = filtradas.Skip((pagina - 1) * tamanio).Take(tamanio).ToList()
            };
        }

        public BE.PaginaResultado<BE.BITACORA> ListarPaginado(string usuario, string criticidad, DateTime? fechaDesde, DateTime? fechaHasta, int pagina, int tamanio)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@usuario", string.IsNullOrEmpty(usuario) ? null : BE.AESHelper.Encriptar(usuario)),
                _acceso.CrearParametro("@criticidad", string.IsNullOrEmpty(criticidad) ? null : BE.AESHelper.Encriptar(criticidad))
            };
            var filas = new List<BE.BITACORA>();
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("BITACORA_LISTAR_FILTRADO", p);
                foreach (DataRow r in dt.Rows) filas.Add(Mapear(r));
            }
            finally { _acceso.Cerrar(); }
            return Paginar(filas, fechaDesde, fechaHasta, pagina, tamanio);
        }
    }
}
