using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BE
{
    public static class AESHelper
    {
        private static readonly byte[] Key = Convert.FromBase64String(ConfigurationManager.AppSettings["AesKey"]);
        private static readonly byte[] IV  = Convert.FromBase64String(ConfigurationManager.AppSettings["AesIV"]);

        public const string TextoIlegible = "[dato ilegible]";

        private const string FormatoFecha = "yyyy-MM-ddTHH:mm:ss.fff";
        private const string FormatoDecimal = "F2";

        public static byte[] Encriptar(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return null;
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV  = IV;
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    byte[] datos = Encoding.UTF8.GetBytes(texto);
                    cs.Write(datos, 0, datos.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        public static string Desencriptar(byte[] datos)
        {
            if (datos == null || datos.Length == 0) return string.Empty;
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV  = IV;
                using (MemoryStream ms = new MemoryStream(datos))
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        public static byte[] EncriptarEntero(int valor) =>
            Encriptar(valor.ToString(CultureInfo.InvariantCulture));

        public static byte[] EncriptarBool(bool valor) =>
            Encriptar(valor ? "1" : "0");

        public static byte[] EncriptarDecimal(decimal? valor) =>
            valor.HasValue ? Encriptar(valor.Value.ToString(FormatoDecimal, CultureInfo.InvariantCulture)) : null;

        public static byte[] EncriptarFecha(DateTime? valor) =>
            valor.HasValue ? Encriptar(valor.Value.ToString(FormatoFecha, CultureInfo.InvariantCulture)) : null;

        private static byte[] ComoBytes(object valor)
        {
            if (valor == null || valor == DBNull.Value) return null;
            return (byte[])valor;
        }

        private static string DesencriptarSeguro(object valor, out bool ilegible)
        {
            ilegible = false;
            byte[] datos = ComoBytes(valor);
            if (datos == null || datos.Length == 0) return null;
            try { return Desencriptar(datos); }
            catch (CryptographicException) { ilegible = true; return null; }
        }

        public static string DesencriptarTexto(object valor)
        {
            string texto = DesencriptarSeguro(valor, out bool ilegible);
            return ilegible ? TextoIlegible : texto;
        }

        public static int DesencriptarEntero(object valor)
        {
            string texto = DesencriptarSeguro(valor, out _);
            return int.TryParse(texto, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int n) ? n : 0;
        }

        public static bool DesencriptarBool(object valor) =>
            DesencriptarSeguro(valor, out _) == "1";

        public static decimal? DesencriptarDecimal(object valor)
        {
            string texto = DesencriptarSeguro(valor, out _);
            return decimal.TryParse(texto, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out decimal d) ? d : (decimal?)null;
        }

        public static DateTime? DesencriptarFecha(object valor)
        {
            string texto = DesencriptarSeguro(valor, out _);
            return DateTime.TryParseExact(texto, FormatoFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime f)
                ? f : (DateTime?)null;
        }
    }
}
