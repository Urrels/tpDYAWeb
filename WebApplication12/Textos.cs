using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace CAPAS_Web
{
    /// <summary>
    /// Traducción de textos armados desde el código (mensajes, encabezados de grillas, etc.).
    /// La primera vez que se usa una clave se registra en CONTROL_IDIOMA con su texto por defecto,
    /// así aparece sola en Idiomas.aspx para traducirla.
    /// </summary>
    internal static class Textos
    {
        private static readonly object _cerrojo = new object();
        private static readonly HashSet<string> _registradas = new HashSet<string>();

        internal static string T(string clave, string textoDefault, params object[] args)
        {
            Registrar(clave, textoDefault);
            string texto = IdiomaWeb.Fachada.Traducir(clave) ?? textoDefault;
            if (args.Length == 0) return texto;
            try
            {
                return string.Format(texto, args);
            }
            catch (FormatException)
            {
                return string.Format(textoDefault, args);
            }
        }

        /// <summary>Igual que T pero codificado para HTML: es lo que hay que usar al asignar a un Label.Text, que no codifica.</summary>
        internal static string TH(string clave, string textoDefault, params object[] args) =>
            System.Web.HttpUtility.HtmlEncode(T(clave, textoDefault, args));

        internal static void Registrar(string clave, string textoDefault)
        {
            lock (_cerrojo)
                if (!_registradas.Add(clave)) return;
            try
            {
                IdiomaWeb.Fachada.RegistrarClave(clave, textoDefault);
            }
            catch (SqlException)
            {
                // Si la base no responde la clave se vuelve a intentar en el próximo uso.
                lock (_cerrojo) _registradas.Remove(clave);
            }
        }
    }
}
