using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BE
{
    /// <summary>
    /// RF11 - Encriptación AES para datos de privacidad (dirección del usuario)
    /// La clave se guarda en Web.config -> appSettings -> "AESKey"
    /// </summary>
    public static class AESHelper
    {
        // Clave de 32 bytes (256 bits) - en producción leer de Web.config
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("CAPAS_AES_KEY_32BYTES_SEGURA!!!!"); // exactamente 32 chars
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("CAPAS_IV_16BYTES");                 // exactamente 16 chars

        public static byte[] Encriptar(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return null;
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
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
                aes.IV = IV;
                using (MemoryStream ms = new MemoryStream(datos))
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        public static string DesencriptarDesdeHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return string.Empty;
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return Desencriptar(bytes);
        }
    }
}
