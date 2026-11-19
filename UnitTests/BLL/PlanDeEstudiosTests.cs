using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BLL
{
    [TestClass]
    public class PlanDeEstudiosTests
    {
        [TestMethod]
        public void ElPlan_TieneLas51MateriasYLas43Correlativas()
        {
            Assert.AreEqual(51, global::BLL.SiembraBLL.MateriasDelPlan().Count);
            Assert.AreEqual(43, global::BLL.SiembraBLL.CorrelativasDelPlan().Count);
        }

        [TestMethod]
        public void ElPlan_ConservaLosDatosOriginales()
        {
            var porCodigo = global::BLL.SiembraBLL.MateriasDelPlan().ToDictionary(m => m.Codigo);
            Assert.AreEqual("Introducción a los Algoritmos y la Programación", porCodigo["01"].Nombre);
            Assert.AreEqual(4, porCodigo["01"].Peso);
            Assert.AreEqual("Inglés I", porCodigo["05"].Nombre);
            Assert.AreEqual(1, porCodigo["05"].Peso);
            Assert.AreEqual("Práctica Profesional Supervisada", porCodigo["46"].Nombre);
            Assert.AreEqual(5, porCodigo["46"].Peso);
            Assert.AreEqual("Trabajo Final de Ingeniería", porCodigo["51"].Nombre);
            Assert.IsTrue(porCodigo.Values.All(m => m.Modalidad == "Presencial"));
        }

        [TestMethod]
        public void ElPlan_CodigosUnicosNormalizadosYPesosValidos()
        {
            var materias = global::BLL.SiembraBLL.MateriasDelPlan();
            Assert.AreEqual(materias.Count, materias.Select(m => m.Codigo).Distinct().Count());
            Assert.IsTrue(materias.All(m => m.Codigo == global::BE.Normalizador.CodigoMateria(m.Codigo)));
            Assert.IsTrue(materias.All(m => m.Peso >= 1 && m.Peso <= 5));
            Assert.IsTrue(materias.All(m => !string.IsNullOrWhiteSpace(m.Nombre) && m.Nombre.Length <= 100));
        }

        [TestMethod]
        public void ElPlan_LasCorrelativasExistenYNoFormanCiclos()
        {
            var codigos = new HashSet<string>(global::BLL.SiembraBLL.MateriasDelPlan().Select(m => m.Codigo));
            var correlativas = global::BLL.SiembraBLL.CorrelativasDelPlan();
            Assert.IsTrue(correlativas.All(c => codigos.Contains(c.Key) && codigos.Contains(c.Value) && c.Key != c.Value));
            Assert.AreEqual(correlativas.Count, correlativas.Distinct().Count());

            MethodInfo buscar = typeof(global::BLL.MateriaXmlBLL).GetMethod("BuscarCiclo", BindingFlags.NonPublic | BindingFlags.Static);
            var aristas = new HashSet<string>(correlativas.Select(c => c.Key + "|" + c.Value), StringComparer.OrdinalIgnoreCase);
            Assert.IsNull(buscar.Invoke(null, new object[] { aristas, new List<KeyValuePair<string, string>>() }));
        }

        [TestMethod]
        public void ImportacionXml_NormalizaLosCodigosAMayusculasYSinEspacios()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<Materias><Materia codigo=\" mat-1a \"><Nombre>Algoritmos</Nombre><Modalidad>Presencial</Modalidad>" +
                        "<Peso>3</Peso><Correlativas><Correlativa codigo=\" fis-2b\"/></Correlativas></Materia></Materias>");
            var resultado = new global::BLL.ResultadoImportacionXml();
            MethodInfo leer = typeof(global::BLL.MateriaXmlBLL).GetMethod("LeerMaterias", BindingFlags.NonPublic | BindingFlags.Static);
            var lista = (System.Collections.IEnumerable)leer.Invoke(null, new object[] { doc, resultado });
            object entrante = lista.Cast<object>().Single();
            var materia = (global::BE.MATERIA)entrante.GetType().GetField("Materia").GetValue(entrante);
            var correlativas = (List<string>)entrante.GetType().GetField("Correlativas").GetValue(entrante);
            Assert.AreEqual("MAT-1A", materia.Codigo);
            CollectionAssert.AreEqual(new[] { "FIS-2B" }, correlativas);
        }
    }
}
