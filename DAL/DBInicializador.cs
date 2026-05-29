using System;
using System.Data.SqlClient;
using System.IO;

namespace DAL
{
    public class DBInicializador
    {
        private const string ConexionMaster =
            "initial catalog=master; Data Source=.; Integrated Security=SSPI";

        public static void InicializarSiNoExiste()
        {
            try
            {
                string rutaScript = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Script_BD.sql");

                if (!File.Exists(rutaScript))
                    throw new FileNotFoundException("No se encontró Script_BD.sql en: " + rutaScript);

                string script = File.ReadAllText(rutaScript);
                string[] bloques = script.Split(
                    new[] { "\r\nGO", "\nGO", "\r\ngo", "\ngo" },
                    StringSplitOptions.RemoveEmptyEntries);

                using (SqlConnection conn = new SqlConnection(ConexionMaster))
                {
                    conn.Open();
                    foreach (string bloque in bloques)
                    {
                        string sql = bloque.Trim();
                        if (string.IsNullOrWhiteSpace(sql)) continue;
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                            cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al inicializar la base de datos: " + ex.Message, ex);
            }
        }
    }
}
