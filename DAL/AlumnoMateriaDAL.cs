using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class AlumnoMateriaDAL
    {
        private readonly Acceso _acceso = new Acceso();

        public bool Insertar(BE.ALUMNO_MATERIA am)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id_usuario", am.IdUsuario),
                _acceso.CrearParametro("@id_materia", am.IdMateria),
                _acceso.CrearParametro("@estado",     am.Estado)
            };
            try { _acceso.Abrir(); return _acceso.Escribir("ALUMNO_MATERIA_INSERTAR", p) > 0; }
            finally { _acceso.Cerrar(); }
        }
        public bool Actualizar(BE.ALUMNO_MATERIA am)
        {
            var p = new List<SqlParameter>
    {
        _acceso.CrearParametro("@id",                   am.Id),
        _acceso.CrearParametro("@estado",               am.Estado),
        _acceso.CrearParametro("@nota_parcial1",        am.NotaParcial1),
        _acceso.CrearParametro("@nota_parcial2",        am.NotaParcial2),
        _acceso.CrearParametro("@nota_recuperatorio",   am.NotaRecuperatorio),
        _acceso.CrearParametro("@nota_final",           am.NotaFinal),
        _acceso.CrearParametro("@fecha_final",          am.FechaFinal),
        _acceso.CrearParametro("@fecha_recuperatorio",  am.FechaRecuperatorio),
        _acceso.CrearParametro("@dvh",                  am.DVH)
    };
            try { _acceso.Abrir(); return _acceso.Escribir("ALUMNO_MATERIA_ACTUALIZAR", p) > 0; }
            finally { _acceso.Cerrar(); }
        }
        public List<BE.ALUMNO_MATERIA> Listar(int idUsuario)
        {
            var lista = new List<BE.ALUMNO_MATERIA>();
            var p = new List<SqlParameter> { _acceso.CrearParametro("@id_usuario", idUsuario) };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("ALUMNO_MATERIA_LISTAR", p);
                foreach (DataRow r in dt.Rows)
                    lista.Add(new BE.ALUMNO_MATERIA
                    {
                        Id = Convert.ToInt32(r["ID"]),
                        IdMateria = Convert.ToInt32(r["ID_MATERIA"]),
                        NombreMateria = r["NOMBRE"].ToString(),
                        CodigoMateria = r["CODIGO"].ToString(),
                        Modalidad = r["MODALIDAD"].ToString(),
                        PesoMateria = Convert.ToInt32(r["PESO"]),
                        Estado = r["ESTADO"].ToString(),
                        NotaParcial1 = r["NOTA_PARCIAL1"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["NOTA_PARCIAL1"]),
                        NotaParcial2 = r["NOTA_PARCIAL2"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["NOTA_PARCIAL2"]),
                        NotaRecuperatorio = r["NOTA_RECUPERATORIO"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["NOTA_RECUPERATORIO"]),
                        NotaFinal = r["NOTA_FINAL"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["NOTA_FINAL"]),
                        FechaFinal = r["FECHA_FINAL"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["FECHA_FINAL"]),
                        FechaRecuperatorio = r["FECHA_RECUPERATORIO"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["FECHA_RECUPERATORIO"]),
                        DVH = r["DVH"] == DBNull.Value ? 0 : Convert.ToInt32(r["DVH"])
                    });
            }
            finally { _acceso.Cerrar(); }
            return lista;
        }
        public void GuardarDVV(int idUsuario, int dvv)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id_usuario", idUsuario),
                _acceso.CrearParametro("@dvv",        dvv)
            };
            try { _acceso.Abrir(); _acceso.Escribir("DVV_GUARDAR", p); }
            finally { _acceso.Cerrar(); }
        }

        public int ObtenerDVV(int idUsuario)
        {
            var p = new List<SqlParameter> { _acceso.CrearParametro("@id_usuario", idUsuario) };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("DVV_OBTENER", p);
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["DVV"]) : -1;
            }
            finally { _acceso.Cerrar(); }
        }
    }

    public class EventoDAL
    {
        private readonly Acceso _acceso = new Acceso();

        public bool Insertar(BE.EVENTO_ACADEMICO e)
        {
            var p = new List<SqlParameter>
    {
        _acceso.CrearParametro("@id_materia",  e.IdMateria),
        _acceso.CrearParametro("@id_usuario",  e.IdUsuario),
        _acceso.CrearParametro("@tipo",        e.Tipo),
        _acceso.CrearParametro("@descripcion", e.Descripcion),
        _acceso.CrearParametro("@fecha",       e.Fecha),
        _acceso.CrearParametro("@peso",        e.Peso)
    };
            try { _acceso.Abrir(); return _acceso.Escribir("EVENTO_INSERTAR", p) > 0; }
            finally { _acceso.Cerrar(); }
        }

        public bool Actualizar(BE.EVENTO_ACADEMICO e)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id",          e.Id),
                _acceso.CrearParametro("@tipo",        e.Tipo),
                _acceso.CrearParametro("@descripcion", e.Descripcion),
                _acceso.CrearParametro("@fecha",       e.Fecha),
                _acceso.CrearParametro("@peso",        e.Peso)
            };
                    try { _acceso.Abrir(); return _acceso.Escribir("EVENTO_ACTUALIZAR", p) > 0; }
                    finally { _acceso.Cerrar(); }
                }

        public bool Eliminar(int id)
        {
            var p = new List<SqlParameter> { _acceso.CrearParametro("@id", id) };
            try { _acceso.Abrir(); return _acceso.Escribir("EVENTO_ELIMINAR", p) > 0; }
            finally { _acceso.Cerrar(); }
        }

        public List<BE.EVENTO_ACADEMICO> ListarPorUsuario(int idUsuario)
        {
            var p = new List<SqlParameter> { _acceso.CrearParametro("@id_usuario", idUsuario) };
            try
            {
                _acceso.Abrir();
                return Mapear(_acceso.Leer("EVENTO_LISTAR_USUARIO", p));
            }
            finally { _acceso.Cerrar(); }
        }

        public List<BE.EVENTO_ACADEMICO> ListarPorMes(int idUsuario, int anio, int mes)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id_usuario", idUsuario),
                _acceso.CrearParametro("@anio",       anio),
                _acceso.CrearParametro("@mes",        mes)
            };
            try
            {
                _acceso.Abrir();
                return Mapear(_acceso.Leer("EVENTO_LISTAR_MES", p));
            }
            finally { _acceso.Cerrar(); }
        }

        private List<BE.EVENTO_ACADEMICO> Mapear(DataTable dt)
        {
            var lista = new List<BE.EVENTO_ACADEMICO>();
            foreach (DataRow r in dt.Rows)
                lista.Add(new BE.EVENTO_ACADEMICO
                {
                    Id = Convert.ToInt32(r["ID"]),
                    IdMateria = Convert.ToInt32(r["ID_MATERIA"]),
                    NombreMateria = r["MATERIA"].ToString(),
                    Tipo = r["TIPO"].ToString(),
                    Descripcion = r["DESCRIPCION"].ToString(),
                    Fecha = Convert.ToDateTime(r["FECHA"]),
                    Peso = Convert.ToInt32(r["PESO"])
                });
            return lista;
        }
    }
}
