using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BE
{
    public static class AESHelper
    {
        // Clave e IV viven en Web.config (appSettings), no en el código fuente.
        private static readonly byte[] Key = Convert.FromBase64String(ConfigurationManager.AppSettings["AesKey"]);
        private static readonly byte[] IV  = Convert.FromBase64String(ConfigurationManager.AppSettings["AesIV"]);

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
    }
}
