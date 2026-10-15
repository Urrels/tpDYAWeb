using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace BLL
{
    public class ResultadoImportacionXml
    {
        public int Creadas { get; set; }
        public int Actualizadas { get; set; }
        public int CorrelativasAgregadas { get; set; }
        public List<string> Errores { get; } = new List<string>();
        public bool Ok => Errores.Count == 0;
    }

    public class MateriaXmlBLL
    {
        public const int TamanioMaximoBytes = 1024 * 1024;

        private static readonly string[] Modalidades = { "Presencial", "Virtual" };

        private readonly DAL.MateriaDAL _dal = new DAL.MateriaDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();
        private readonly IntegridadBLL _integridad = new IntegridadBLL();

        public byte[] Exportar(string usuarioAccion)
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlElement raiz = doc.CreateElement("Materias");
            raiz.SetAttribute("exportado", DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss"));
            doc.AppendChild(raiz);

            foreach (BE.MATERIA m in _dal.Listar().OrderBy(x => x.Id))
            {
                XmlElement el = doc.CreateElement("Materia");
                el.SetAttribute("codigo", m.Codigo);
                el.AppendChild(CrearTexto(doc, "Nombre", m.Nombre));
                el.AppendChild(CrearTexto(doc, "Modalidad", m.Modalidad));
                el.AppendChild(CrearTexto(doc, "Peso", m.Peso.ToString()));

                XmlElement correlativas = doc.CreateElement("Correlativas");
                foreach (BE.MATERIA c in _dal.ListarCorrelativas(m.Id).OrderBy(x => x.Id))
                {
                    XmlElement ce = doc.CreateElement("Correlativa");
                    ce.SetAttribute("codigo", c.Codigo);
                    correlativas.AppendChild(ce);
                }
                el.AppendChild(correlativas);
                raiz.AppendChild(el);
            }

            _bitacora.RegistrarAccion(usuarioAccion, "EXPORTACION_XML_MATERIAS");

            using (var ms = new MemoryStream())
            {
                var config = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
                using (XmlWriter w = XmlWriter.Create(ms, config)) doc.Save(w);
                return ms.ToArray();
            }
        }

        public ResultadoImportacionXml Importar(byte[] contenido, string usuarioAccion)
        {
            var resultado = new ResultadoImportacionXml();
            if (contenido == null || contenido.Length == 0)
            {
                resultado.Errores.Add("El archivo está vacío.");
                return resultado;
            }
            if (contenido.Length > TamanioMaximoBytes)
            {
                resultado.Errores.Add("El archivo supera el máximo de 1 MB.");
                return resultado;
            }

            XmlDocument doc = Cargar(contenido, resultado);
            if (doc == null) return resultado;

            var entrantes = LeerMaterias(doc, resultado);
            if (!resultado.Ok) return resultado;

            var existentes = _dal.Listar().ToDictionary(m => m.Codigo, StringComparer.OrdinalIgnoreCase);
            var correlativasActuales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (BE.MATERIA m in existentes.Values)
                foreach (BE.MATERIA c in _dal.ListarCorrelativas(m.Id))
                    correlativasActuales.Add(Par(m.Codigo, c.Codigo));

            var codigosConocidos = new HashSet<string>(existentes.Keys, StringComparer.OrdinalIgnoreCase);
            foreach (var e in entrantes) codigosConocidos.Add(e.Materia.Codigo);

            var aplicar = new List<BE.MATERIA>();
            var pares = new List<KeyValuePair<string, string>>();
            foreach (var e in entrantes)
            {
                BE.MATERIA m = e.Materia;
                if (existentes.TryGetValue(m.Codigo, out BE.MATERIA actual))
                {
                    m.Id = actual.Id;
                    if (actual.Nombre != m.Nombre || actual.Modalidad != m.Modalidad || actual.Peso != m.Peso)
                    {
                        aplicar.Add(m);
                        resultado.Actualizadas++;
                    }
                }
                else
                {
                    aplicar.Add(m);
                    resultado.Creadas++;
                }

                foreach (string cod in e.Correlativas)
                {
                    if (!codigosConocidos.Contains(cod))
                        resultado.Errores.Add($"Materia {m.Codigo}: la correlativa {cod} no existe ni en el archivo ni en el sistema.");
                    else if (string.Equals(cod, m.Codigo, StringComparison.OrdinalIgnoreCase))
                        resultado.Errores.Add($"Materia {m.Codigo}: no puede ser correlativa de sí misma.");
                    else if (!correlativasActuales.Contains(Par(m.Codigo, cod)))
                        pares.Add(new KeyValuePair<string, string>(m.Codigo, cod));
                }
            }
            if (!resultado.Ok) return resultado;

            string ciclo = BuscarCiclo(correlativasActuales, pares);
            if (ciclo != null)
            {
                resultado.Errores.Add("Las correlativas forman un ciclo: " + ciclo + ".");
                return resultado;
            }

            resultado.CorrelativasAgregadas = pares.Count;
            if (aplicar.Count == 0 && pares.Count == 0) return resultado;

            var idPorCodigo = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (BE.MATERIA m in existentes.Values) idPorCodigo[m.Codigo] = m.Id;

            try
            {
                _dal.Importar(aplicar, idPorCodigo, pares);
            }
            catch (Exception)
            {
                resultado.Errores.Add("No se pudo importar y no se guardó ningún cambio. Revisá que ningún código coincida con una materia dada de baja.");
                resultado.Creadas = resultado.Actualizadas = resultado.CorrelativasAgregadas = 0;
                return resultado;
            }

            _integridad.RecalcularMateria();
            _integridad.RecalcularCorrelativa();
            _bitacora.RegistrarAccion(usuarioAccion,
                $"IMPORTACION_XML_MATERIAS:{resultado.Creadas} altas, {resultado.Actualizadas} cambios, {resultado.CorrelativasAgregadas} correlativas");
            return resultado;
        }

        private static XmlElement CrearTexto(XmlDocument doc, string nombre, string texto)
        {
            XmlElement el = doc.CreateElement(nombre);
            el.InnerText = texto ?? "";
            return el;
        }

        private static string Par(string materia, string correlativa) => materia + "|" + correlativa;

        private static XmlDocument Cargar(byte[] contenido, ResultadoImportacionXml resultado)
        {
            var config = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true
            };
            try
            {
                var doc = new XmlDocument { XmlResolver = null };
                using (var ms = new MemoryStream(contenido))
                using (XmlReader r = XmlReader.Create(ms, config))
                    doc.Load(r);
                if (doc.DocumentElement == null || doc.DocumentElement.Name != "Materias")
                {
                    resultado.Errores.Add("El elemento raíz debe ser <Materias>.");
                    return null;
                }
                return doc;
            }
            catch (XmlException ex)
            {
                resultado.Errores.Add($"El archivo no es un XML válido (línea {ex.LineNumber}).");
                return null;
            }
        }

        private class Entrante
        {
            public BE.MATERIA Materia;
            public List<string> Correlativas = new List<string>();
        }

        private static List<Entrante> LeerMaterias(XmlDocument doc, ResultadoImportacionXml resultado)
        {
            var lista = new List<Entrante>();
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (XmlElement el in doc.DocumentElement.SelectNodes("Materia"))
            {
                string codigo = (el.GetAttribute("codigo") ?? "").Trim();
                string nombre = (el.SelectSingleNode("Nombre")?.InnerText ?? "").Trim();
                string modalidad = (el.SelectSingleNode("Modalidad")?.InnerText ?? "").Trim();
                string pesoTexto = (el.SelectSingleNode("Peso")?.InnerText ?? "").Trim();
                string etiqueta = codigo.Length > 0 ? codigo : "(sin código)";

                if (codigo.Length == 0 || codigo.Length > 20)
                    resultado.Errores.Add($"Materia {etiqueta}: el código es obligatorio y admite hasta 20 caracteres.");
                else if (!vistos.Add(codigo))
                    resultado.Errores.Add($"Materia {codigo}: el código está repetido en el archivo.");
                if (nombre.Length == 0 || nombre.Length > 100)
                    resultado.Errores.Add($"Materia {etiqueta}: el nombre es obligatorio y admite hasta 100 caracteres.");
                if (!Modalidades.Contains(modalidad))
                    resultado.Errores.Add($"Materia {etiqueta}: la modalidad debe ser Presencial o Virtual.");
                if (!int.TryParse(pesoTexto, out int peso) || peso < 1 || peso > 5)
                    resultado.Errores.Add($"Materia {etiqueta}: el peso debe ser un número entre 1 y 5.");

                var entrante = new Entrante
                {
                    Materia = new BE.MATERIA { Codigo = codigo, Nombre = nombre, Modalidad = modalidad, Peso = peso }
                };
                foreach (XmlElement c in el.SelectNodes("Correlativas/Correlativa"))
                {
                    string cod = (c.GetAttribute("codigo") ?? "").Trim();
                    if (cod.Length == 0)
                        resultado.Errores.Add($"Materia {etiqueta}: hay una correlativa sin código.");
                    else if (!entrante.Correlativas.Contains(cod, StringComparer.OrdinalIgnoreCase))
                        entrante.Correlativas.Add(cod);
                }
                lista.Add(entrante);
            }

            if (lista.Count == 0 && resultado.Ok)
                resultado.Errores.Add("El archivo no contiene materias.");
            return lista;
        }

        private static string BuscarCiclo(HashSet<string> actuales, List<KeyValuePair<string, string>> nuevos)
        {
            var aristas = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            void Agregar(string de, string a)
            {
                if (!aristas.TryGetValue(de, out List<string> destinos)) aristas[de] = destinos = new List<string>();
                destinos.Add(a);
            }
            foreach (string p in actuales)
            {
                string[] partes = p.Split('|');
                Agregar(partes[0], partes[1]);
            }
            foreach (var n in nuevos) Agregar(n.Key, n.Value);

            var estado = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var camino = new List<string>();

            string Visitar(string nodo)
            {
                estado[nodo] = 1;
                camino.Add(nodo);
                if (aristas.TryGetValue(nodo, out List<string> destinos))
                    foreach (string d in destinos)
                    {
                        estado.TryGetValue(d, out int e);
                        if (e == 1)
                        {
                            int desde = camino.FindIndex(x => string.Equals(x, d, StringComparison.OrdinalIgnoreCase));
                            return string.Join(" → ", camino.Skip(desde).Concat(new[] { d }));
                        }
                        if (e == 0)
                        {
                            string c = Visitar(d);
                            if (c != null) return c;
                        }
                    }
                camino.RemoveAt(camino.Count - 1);
                estado[nodo] = 2;
                return null;
            }

            foreach (string nodo in aristas.Keys.ToList())
            {
                estado.TryGetValue(nodo, out int e);
                if (e != 0) continue;
                string ciclo = Visitar(nodo);
                if (ciclo != null) return ciclo;
            }
            return null;
        }
    }
}
