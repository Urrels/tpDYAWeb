using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    internal class Acceso
    {
        private SqlConnection conexion;
        private SqlTransaction transaccion;

        // Cambia el connection string según tu entorno
        private const string CONNECTION_STRING =  "Data Source=(localdb)\\MSSQLLocalDB; Initial Catalog=BDCAPAS; Integrated Security=SSPI";

        public void Abrir()
        {
            conexion = new SqlConnection(CONNECTION_STRING);
            conexion.Open();
        }

        public void Cerrar()
        {
            if (conexion != null && conexion.State == ConnectionState.Open)
                conexion.Close();
            conexion = null;
            GC.Collect();
        }

        public void IniciarTx() { transaccion = conexion.BeginTransaction(); }
        public void ConfirmarTX() { transaccion.Commit(); transaccion = null; }
        public void DeshacerTX() { transaccion.Rollback(); transaccion = null; }

        private SqlCommand CrearComando(string sp, List<SqlParameter> parametros = null)
        {
            SqlCommand cmd = new SqlCommand(sp, conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            if (transaccion != null) cmd.Transaction = transaccion;
            if (parametros != null) cmd.Parameters.AddRange(parametros.ToArray());
            return cmd;
        }

        public int Escribir(string sp, List<SqlParameter> parametros = null)
        {
            SqlCommand cmd = CrearComando(sp, parametros);
            try { return cmd.ExecuteNonQuery(); }
            catch { return -1; }
            finally { cmd.Parameters.Clear(); }
        }

        public object EscribirConRetorno(string sp, List<SqlParameter> parametros = null)
        {
            SqlCommand cmd = CrearComando(sp, parametros);
            try { return cmd.ExecuteScalar(); }
            catch { return null; }
            finally { cmd.Parameters.Clear(); }
        }

        public DataTable Leer(string sp, List<SqlParameter> parametros = null)
        {
            SqlDataAdapter da = new SqlDataAdapter(CrearComando(sp, parametros));
            DataTable dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public SqlParameter CrearParametro(string nombre, string valor)
        {
            return new SqlParameter(nombre, DbType.String) { Value = (object)valor ?? DBNull.Value };
        }
        public SqlParameter CrearParametro(string nombre, int valor)
        {
            return new SqlParameter(nombre, DbType.Int32) { Value = valor };
        }
        public SqlParameter CrearParametro(string nombre, int? valor)
        {
            return new SqlParameter(nombre, DbType.Int32) { Value = (object)valor ?? DBNull.Value };
        }
        public SqlParameter CrearParametro(string nombre, decimal? valor)
        {
            return new SqlParameter(nombre, DbType.Decimal) { Value = (object)valor ?? DBNull.Value };
        }
        public SqlParameter CrearParametro(string nombre, DateTime valor)
        {
            return new SqlParameter(nombre, DbType.DateTime) { Value = valor };
        }
        public SqlParameter CrearParametro(string nombre, byte[] valor)
        {
            return new SqlParameter(nombre, SqlDbType.VarBinary) { Value = (object)valor ?? DBNull.Value };
        }
        public SqlParameter CrearParametro(string nombre, DateTime? valor)
        {
            return new SqlParameter(nombre, SqlDbType.DateTime)
            { Value = (object)valor ?? DBNull.Value };
        }
    }
}
