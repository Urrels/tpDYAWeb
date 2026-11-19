using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

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
                _acceso.CrearParametro("@estado",     BE.AESHelper.Encriptar(am.Estado))
            };
            try
            {
                _acceso.Abrir();
                object resultado = _acceso.EscribirConRetorno("ALUMNO_MATERIA_INSERTAR", p);
                return resultado != null && resultado != DBNull.Value;
            }
            finally { _acceso.Cerrar(); }
        }
        public bool Actualizar(BE.ALUMNO_MATERIA am)
        {
            var p = new List<SqlParameter>
    {
        _acceso.CrearParametro("@id",                   am.Id),
        _acceso.CrearParametro("@estado",               BE.AESHelper.Encriptar(am.Estado)),
        _acceso.CrearParametro("@nota_parcial1",        BE.AESHelper.EncriptarDecimal(am.NotaParcial1)),
        _acceso.CrearParametro("@nota_parcial2",        BE.AESHelper.EncriptarDecimal(am.NotaParcial2)),
        _acceso.CrearParametro("@nota_recuperatorio",   BE.AESHelper.EncriptarDecimal(am.NotaRecuperatorio)),
        _acceso.CrearParametro("@nota_final",           BE.AESHelper.EncriptarDecimal(am.NotaFinal)),
        _acceso.CrearParametro("@fecha_final",          BE.AESHelper.EncriptarFecha(am.FechaFinal)),
        _acceso.CrearParametro("@fecha_recuperatorio",  BE.AESHelper.EncriptarFecha(am.FechaRecuperatorio))
    };
            try
            {
                _acceso.Abrir();
                object resultado = _acceso.EscribirConRetorno("ALUMNO_MATERIA_ACTUALIZAR", p);
                return Convert.ToInt32(resultado) > 0;
            }
            finally { _acceso.Cerrar(); }
        }
        public List<BE.ALUMNO_MATERIA> Listar(int idUsuario)
        {
            var lista = new List<BE.ALUMNO_MATERIA>();
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id_usuario", idUsuario),
                _acceso.CrearParametro("@activa", BE.AESHelper.EncriptarBool(true))
            };
            try
            {
                _acceso.Abrir();
                DataTable dt = _acceso.Leer("ALUMNO_MATERIA_LISTAR", p);
                foreach (DataRow r in dt.Rows)
                    lista.Add(new BE.ALUMNO_MATERIA
                    {
                        Id = Convert.ToInt32(r["ID"]),
                        IdMateria = Convert.ToInt32(r["ID_MATERIA"]),
                        NombreMateria = BE.AESHelper.DesencriptarTexto(r["NOMBRE"]),
                        CodigoMateria = BE.AESHelper.DesencriptarTexto(r["CODIGO"]),
                        Modalidad = BE.AESHelper.DesencriptarTexto(r["MODALIDAD"]),
                        PesoMateria = BE.AESHelper.DesencriptarEntero(r["PESO"]),
                        Estado = BE.AESHelper.DesencriptarTexto(r["ESTADO"]),
                        NotaParcial1 = BE.AESHelper.DesencriptarDecimal(r["NOTA_PARCIAL1"]),
                        NotaParcial2 = BE.AESHelper.DesencriptarDecimal(r["NOTA_PARCIAL2"]),
                        NotaRecuperatorio = BE.AESHelper.DesencriptarDecimal(r["NOTA_RECUPERATORIO"]),
                        NotaFinal = BE.AESHelper.DesencriptarDecimal(r["NOTA_FINAL"]),
                        FechaFinal = BE.AESHelper.DesencriptarFecha(r["FECHA_FINAL"]),
                        FechaRecuperatorio = BE.AESHelper.DesencriptarFecha(r["FECHA_RECUPERATORIO"]),
                        DVH = r["DVH"] == DBNull.Value ? 0 : Convert.ToInt32(r["DVH"])
                    });
            }
            finally { _acceso.Cerrar(); }
            return lista;
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
        _acceso.CrearParametro("@tipo",        BE.AESHelper.Encriptar(e.Tipo)),
        _acceso.CrearParametro("@descripcion", BE.AESHelper.Encriptar(e.Descripcion)),
        _acceso.CrearParametro("@fecha",       BE.AESHelper.EncriptarFecha(e.Fecha)),
        _acceso.CrearParametro("@peso",        BE.AESHelper.EncriptarEntero(e.Peso))
    };
            try
            {
                _acceso.Abrir();
                object resultado = _acceso.EscribirConRetorno("EVENTO_INSERTAR", p);
                return resultado != null && resultado != DBNull.Value;
            }
            finally { _acceso.Cerrar(); }
        }

        public bool Actualizar(BE.EVENTO_ACADEMICO e)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id",          e.Id),
                _acceso.CrearParametro("@id_usuario",  e.IdUsuario),
                _acceso.CrearParametro("@tipo",        BE.AESHelper.Encriptar(e.Tipo)),
                _acceso.CrearParametro("@descripcion", BE.AESHelper.Encriptar(e.Descripcion)),
                _acceso.CrearParametro("@fecha",       BE.AESHelper.EncriptarFecha(e.Fecha)),
                _acceso.CrearParametro("@peso",        BE.AESHelper.EncriptarEntero(e.Peso))
            };
            try
            {
                _acceso.Abrir();
                object resultado = _acceso.EscribirConRetorno("EVENTO_ACTUALIZAR", p);
                return Convert.ToInt32(resultado) > 0;
            }
            finally { _acceso.Cerrar(); }
        }

        public bool Eliminar(int id, int idUsuario)
        {
            var p = new List<SqlParameter>
            {
                _acceso.CrearParametro("@id", id),
                _acceso.CrearParametro("@id_usuario", idUsuario)
            };
            try
            {
                _acceso.Abrir();
                object resultado = _acceso.EscribirConRetorno("EVENTO_ELIMINAR", p);
                return Convert.ToInt32(resultado) > 0;
            }
            finally { _acceso.Cerrar(); }
        }

        public List<BE.EVENTO_ACADEMICO> ListarPorUsuario(int idUsuario)
        {
            var p = new List<SqlParameter> { _acceso.CrearParametro("@id_usuario", idUsuario) };
            try
            {
                _acceso.Abrir();
                return Mapear(_acceso.Leer("EVENTO_LISTAR_USUARIO", p)).OrderBy(e => e.Fecha).ThenBy(e => e.Id).ToList();
            }
            finally { _acceso.Cerrar(); }
        }

        public List<BE.EVENTO_ACADEMICO> ListarPorMes(int idUsuario, int anio, int mes)
        {
            return ListarPorUsuario(idUsuario)
                .Where(e => e.Fecha.Year == anio && e.Fecha.Month == mes)
                .ToList();
        }

        private List<BE.EVENTO_ACADEMICO> Mapear(DataTable dt)
        {
            var lista = new List<BE.EVENTO_ACADEMICO>();
            foreach (DataRow r in dt.Rows)
                lista.Add(new BE.EVENTO_ACADEMICO
                {
                    Id = Convert.ToInt32(r["ID"]),
                    IdMateria = Convert.ToInt32(r["ID_MATERIA"]),
                    NombreMateria = BE.AESHelper.DesencriptarTexto(r["MATERIA"]),
                    Tipo = BE.AESHelper.DesencriptarTexto(r["TIPO"]),
                    Descripcion = BE.AESHelper.DesencriptarTexto(r["DESCRIPCION"]) ?? "",
                    Fecha = BE.AESHelper.DesencriptarFecha(r["FECHA"]) ?? DateTime.MinValue,
                    Peso = BE.AESHelper.DesencriptarEntero(r["PESO"])
                });
            return lista;
        }
    }
}
