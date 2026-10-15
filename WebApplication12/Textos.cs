using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace CAPAS_Web
{
    public static class Textos
    {
        private static readonly object _cerrojo = new object();
        private static readonly HashSet<string> _registradas = new HashSet<string>();

        private static readonly KeyValuePair<Regex, string>[] Plantillas =
        {
            Plantilla(@"^El perfil (.+) es del sistema: no se puede (.+)\.$", "El perfil {0} es del sistema: no se puede {1}."),
            Plantilla(@"^La dirección es demasiado larga \(máximo (\d+) caracteres; los acentos y símbolos ocupan más\)\.$",
                      "La dirección es demasiado larga (máximo {0} caracteres; los acentos y símbolos ocupan más)."),
            Plantilla(@"^El cambio se guardó, pero falló un paso posterior \(integridad o bitácora\): (.+)$",
                      "El cambio se guardó, pero falló un paso posterior (integridad o bitácora): {0}"),
            Plantilla(@"^No podés inscribirte a '(.+)': tenés correlativas pendientes\.$",
                      "No podés inscribirte a '{0}': tenés correlativas pendientes."),
            Plantilla(@"^🔴 Parcial 1 desaprobado \((.*)\)\. Necesitás ir al recuperatorio\.$", "🔴 Parcial 1 desaprobado ({0}). Necesitás ir al recuperatorio."),
            Plantilla(@"^🔴 Parcial 2 desaprobado \((.*)\)\. Necesitás ir al recuperatorio\.$", "🔴 Parcial 2 desaprobado ({0}). Necesitás ir al recuperatorio."),
            Plantilla(@"^🟡 Parcial 1 con margen justo \((.*)\)\. Necesitás al menos 6 en el Parcial 2\.$", "🟡 Parcial 1 con margen justo ({0}). Necesitás al menos 6 en el Parcial 2."),
            Plantilla(@"^✔ Inscripción completada: (\d+) materia\(s\) registrada\(s\)\.$", "✔ Inscripción completada: {0} materia(s) registrada(s)."),
            Plantilla(@"^(.+): dígitos verificadores verticales ausentes\.$", "{0}: dígitos verificadores verticales ausentes."),
            Plantilla(@"^(.+): dígito verificador vertical ausente para la columna (.+)\.$", "{0}: dígito verificador vertical ausente para la columna {1}."),
            Plantilla(@"^(.+): DVV inválido en la columna (.+) \(esperado (.+), guardado (.+)\)\.$", "{0}: DVV inválido en la columna {1} (esperado {2}, guardado {3})."),
            Plantilla(@"^(.+): hay al menos un registro con DVH inválido, pero no se pudo identificar el campo alterado \(dígitos verificadores ausentes o no inicializados para la tabla, o el propio DVH fue modificado directamente\)\. Recalculá la integridad de la tabla para refinar el diagnóstico\.$",
                      "{0}: hay al menos un registro con DVH inválido, pero no se pudo identificar el campo alterado (dígitos verificadores ausentes o no inicializados para la tabla, o el propio DVH fue modificado directamente). Recalculá la integridad de la tabla para refinar el diagnóstico."),
            Plantilla(@"^(.+): se agregó el registro (.+) \(no estaba en el último estado válido conocido\)\.$", "{0}: se agregó el registro {1} (no estaba en el último estado válido conocido)."),
            Plantilla(@"^(.+): se eliminó el registro (.+)\.$", "{0}: se eliminó el registro {1}."),
            Plantilla(@"^(.+): se modificó el campo (.+) del registro (.+) \(antes: '(.*)', ahora: '(.*)'\)\.$", "{0}: se modificó el campo {1} del registro {2} (antes: '{3}', ahora: '{4}')."),
            Plantilla(@"^(.+): el snapshot está vacío, no se borraron las (\d+) fila\(s\) actuales\.$", "{0}: el snapshot está vacío, no se borraron las {1} fila(s) actuales."),
            Plantilla(@"^No se encontró el perfil (.+) para el usuario (.+)\.$", "No se encontró el perfil {0} para el usuario {1}."),
            Plantilla(@"^No se pudieron sembrar los datos base o calcular su integridad inicial: (.+)$", "No se pudieron sembrar los datos base o calcular su integridad inicial: {0}"),
            Plantilla(@"^Materia (.+): la correlativa (.+) no existe ni en el archivo ni en el sistema\.$", "Materia {0}: la correlativa {1} no existe ni en el archivo ni en el sistema."),
            Plantilla(@"^Materia (.+): no puede ser correlativa de sí misma\.$", "Materia {0}: no puede ser correlativa de sí misma."),
            Plantilla(@"^Materia (.+): el código es obligatorio y admite hasta 20 caracteres\.$", "Materia {0}: el código es obligatorio y admite hasta 20 caracteres."),
            Plantilla(@"^Materia (.+): el código está repetido en el archivo\.$", "Materia {0}: el código está repetido en el archivo."),
            Plantilla(@"^Materia (.+): el nombre es obligatorio y admite hasta 100 caracteres\.$", "Materia {0}: el nombre es obligatorio y admite hasta 100 caracteres."),
            Plantilla(@"^Materia (.+): la modalidad debe ser Presencial o Virtual\.$", "Materia {0}: la modalidad debe ser Presencial o Virtual."),
            Plantilla(@"^Materia (.+): el peso debe ser un número entre 1 y 5\.$", "Materia {0}: el peso debe ser un número entre 1 y 5."),
            Plantilla(@"^Materia (.+): hay una correlativa sin código\.$", "Materia {0}: hay una correlativa sin código."),
            Plantilla(@"^Las correlativas forman un ciclo: (.+)\.$", "Las correlativas forman un ciclo: {0}."),
            Plantilla(@"^El archivo no es un XML válido \(línea (\d+)\)\.$", "El archivo no es un XML válido (línea {0})."),
            Plantilla(@"^Los perfiles se guardaron, pero falló la actualización de la dirección \((.*)\)\.$", "Los perfiles se guardaron, pero falló la actualización de la dirección ({0})."),
            Plantilla(@"^No se guardó: falló la actualización de la dirección \((.*)\)\.$", "No se guardó: falló la actualización de la dirección ({0})."),
            Plantilla(@"^El backup no contiene datos para la tabla (.+)\.$", "El backup no contiene datos para la tabla {0}."),
        };

        private static KeyValuePair<Regex, string> Plantilla(string patron, string plantilla) =>
            new KeyValuePair<Regex, string>(new Regex(patron, RegexOptions.Compiled | RegexOptions.Singleline), plantilla);

        public static string T(string clave, string textoDefault, params object[] args)
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

        public static string TH(string clave, string textoDefault, params object[] args) =>
            HttpUtility.HtmlEncode(T(clave, textoDefault, args));

        public static string THF(string clave, string textoDefault, params string[] argsHtml) =>
            string.Format(HttpUtility.HtmlEncode(T(clave, textoDefault)), argsHtml);

        public static string Negrita(string texto) => "<strong>" + HttpUtility.HtmlEncode(texto) + "</strong>";

        public static string Mensaje(string espanol)
        {
            if (string.IsNullOrWhiteSpace(espanol)) return espanol;
            foreach (var p in Plantillas)
            {
                Match m = p.Key.Match(espanol);
                if (!m.Success) continue;
                var args = new object[m.Groups.Count - 1];
                for (int i = 1; i < m.Groups.Count; i++)
                {
                    string valor = m.Groups[i].Value;
                    args[i - 1] = (valor == "renombrar" || valor == "mover" || valor == "eliminar" || valor == "modificar sus permisos") ? Valor(valor) : valor;
                }
                return T(ClaveMensaje(p.Value), p.Value, args);
            }
            return T(ClaveMensaje(espanol), espanol);
        }

        public static string MH(string espanol) => HttpUtility.HtmlEncode(Mensaje(espanol));

        public static string Valor(string espanol)
        {
            if (string.IsNullOrWhiteSpace(espanol)) return espanol;
            return T("Valor." + ClaveBase(espanol, 80), espanol);
        }

        public static string ValorH(object valor) => HttpUtility.HtmlEncode(Valor(valor as string ?? valor?.ToString()));

        public static string Js(string clave, string textoDefault) =>
            HttpUtility.JavaScriptStringEncode(T(clave, textoDefault));

        public static string ClaveMensaje(string espanol) => "Msg." + ClaveBase(espanol, 90);

        private static string ClaveBase(string texto, int max)
        {
            var sb = new StringBuilder();
            foreach (char ch in texto.Normalize(NormalizationForm.FormD))
            {
                if (sb.Length >= max) break;
                if (ch < 128 && char.IsLetterOrDigit(ch)) sb.Append(ch);
            }
            return sb.Length == 0 ? "vacio" : sb.ToString();
        }

        public static void Registrar(string clave, string textoDefault)
        {
            lock (_cerrojo)
                if (!_registradas.Add(clave)) return;
            try
            {
                IdiomaWeb.Fachada.RegistrarClave(clave, textoDefault.Length > 500 ? textoDefault.Substring(0, 500) : textoDefault);
            }
            catch (SqlException)
            {
                lock (_cerrojo) _registradas.Remove(clave);
            }
        }
    }
}
