using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BLL
{
    /// <summary>
    /// Validación de la importación XML de materias. Todos estos casos se rechazan ANTES de consultar la
    /// base, así que no necesitan conexión. Lo que sí escribe (altas, cambios, correlativas) se prueba a
    /// mano o en las pruebas de integración.
    /// </summary>
    [TestClass]
    public class MateriaXmlBLLTests
    {
        private readonly global::BLL.MateriaXmlBLL _bll = new global::BLL.MateriaXmlBLL();

        private global::BLL.ResultadoImportacionXml Importar(string xml) =>
            _bll.Importar(Encoding.UTF8.GetBytes(xml), "pruebas");

        private static string Materia(string codigo, string nombre = "Algoritmos", string modalidad = "Presencial",
                                      string peso = "3", string correlativas = "") =>
            $"<Materia codigo=\"{codigo}\"><Nombre>{nombre}</Nombre><Modalidad>{modalidad}</Modalidad><Peso>{peso}</Peso>" +
            $"<Correlativas>{correlativas}</Correlativas></Materia>";

        private static string Archivo(params string[] materias) =>
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><Materias>" + string.Concat(materias) + "</Materias>";

        private static void AssertRechaza(global::BLL.ResultadoImportacionXml r, string fragmento)
        {
            Assert.IsFalse(r.Ok, "debería rechazar el archivo");
            Assert.IsTrue(r.Errores.Any(e => e.IndexOf(fragmento, System.StringComparison.OrdinalIgnoreCase) >= 0),
                $"se esperaba un error que contuviera '{fragmento}' pero fueron: {string.Join(" | ", r.Errores)}");
            Assert.AreEqual(0, r.Creadas + r.Actualizadas + r.CorrelativasAgregadas, "no debe informar cambios");
        }

        // ---------- el archivo en sí ----------

        [TestMethod]
        public void ArchivoVacioONulo_SeRechaza()
        {
            AssertRechaza(_bll.Importar(null, "pruebas"), "vacío");
            AssertRechaza(_bll.Importar(new byte[0], "pruebas"), "vacío");
        }

        [TestMethod]
        public void ArchivoMayorA1MB_SeRechaza()
        {
            var grande = new byte[global::BLL.MateriaXmlBLL.TamanioMaximoBytes + 1];
            AssertRechaza(_bll.Importar(grande, "pruebas"), "1 MB");
        }

        [TestMethod]
        public void XmlMalFormado_SeRechaza()
        {
            AssertRechaza(Importar("<Materias><Materia>"), "XML válido");
            AssertRechaza(Importar("esto no es xml"), "XML válido");
        }

        [TestMethod]
        public void RaizIncorrecta_SeRechaza()
        {
            AssertRechaza(Importar("<Otra/>"), "<Materias>");
        }

        [TestMethod]
        public void XmlConDtd_SeRechaza_ProtegeContraXXE()
        {
            string xxe = "<?xml version=\"1.0\"?><!DOCTYPE Materias [<!ENTITY x SYSTEM \"file:///C:/Windows/win.ini\">]><Materias>&x;</Materias>";
            AssertRechaza(Importar(xxe), "XML válido");
        }

        [TestMethod]
        public void ArchivoSinMaterias_SeRechaza()
        {
            AssertRechaza(Importar("<Materias/>"), "no contiene materias");
        }

        // ---------- los campos de cada materia ----------

        [TestMethod]
        public void ModalidadInvalida_SeRechaza()
        {
            AssertRechaza(Importar(Archivo(Materia("X1", modalidad: "Online"))), "modalidad");
        }

        [DataTestMethod]
        [DataRow("0")]
        [DataRow("6")]
        [DataRow("9")]
        [DataRow("-1")]
        [DataRow("abc")]
        [DataRow("")]
        public void PesoFueraDeRango_SeRechaza(string peso)
        {
            AssertRechaza(Importar(Archivo(Materia("X1", peso: peso))), "peso");
        }

        [TestMethod]
        public void NombreVacioOMuyLargo_SeRechaza()
        {
            AssertRechaza(Importar(Archivo(Materia("X1", nombre: ""))), "nombre");
            AssertRechaza(Importar(Archivo(Materia("X1", nombre: new string('n', 101)))), "nombre");
        }

        [TestMethod]
        public void CodigoVacioOMuyLargo_SeRechaza()
        {
            AssertRechaza(Importar(Archivo(Materia(""))), "código");
            AssertRechaza(Importar(Archivo(Materia(new string('C', 21)))), "código");
        }

        [TestMethod]
        public void CodigoRepetido_SeRechaza_IgnorandoMayusculas()
        {
            AssertRechaza(Importar(Archivo(Materia("ABC"), Materia("abc"))), "repetido");
        }

        [TestMethod]
        public void CorrelativaSinCodigo_SeRechaza()
        {
            AssertRechaza(Importar(Archivo(Materia("X1", correlativas: "<Correlativa codigo=\"\"/>"))), "sin código");
        }

        [TestMethod]
        public void VariosErrores_SeInformanTodosJuntos()
        {
            var r = Importar(Archivo(Materia("A", modalidad: "Online"), Materia("B", peso: "9"), Materia("C", nombre: "")));
            Assert.IsFalse(r.Ok);
            Assert.IsTrue(r.Errores.Count >= 3, "debe listar un error por cada materia mala");
        }

        // ---------- ciclos de correlativas (método privado, se prueba por reflexión) ----------

        private static string BuscarCiclo(IEnumerable<string> actuales, params string[] nuevos)
        {
            MethodInfo m = typeof(global::BLL.MateriaXmlBLL).GetMethod("BuscarCiclo", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "no se encontró BuscarCiclo (¿cambió el nombre?)");
            var pares = nuevos.Select(n => { var p = n.Split('>'); return new KeyValuePair<string, string>(p[0], p[1]); }).ToList();
            return (string)m.Invoke(null, new object[] { new HashSet<string>(actuales, System.StringComparer.OrdinalIgnoreCase), pares });
        }

        [TestMethod]
        public void Ciclos_UnaCadenaNoEsCiclo()
        {
            Assert.IsNull(BuscarCiclo(new string[0], "A>B", "B>C", "C>D"));
        }

        [TestMethod]
        public void Ciclos_UnRombo_NoEsCiclo()
        {
            Assert.IsNull(BuscarCiclo(new string[0], "A>B", "A>C", "B>D", "C>D"));
        }

        [TestMethod]
        public void Ciclos_DosMateriasQueSeRequierenMutuamente()
        {
            string ciclo = BuscarCiclo(new string[0], "A>B", "B>A");
            Assert.IsNotNull(ciclo);
            StringAssert.Contains(ciclo, "A");
            StringAssert.Contains(ciclo, "B");
        }

        [TestMethod]
        public void Ciclos_UnCicloLargo()
        {
            Assert.IsNotNull(BuscarCiclo(new string[0], "A>B", "B>C", "C>D", "D>A"));
        }

        [TestMethod]
        public void Ciclos_UnaMateriaCorrelativaDeSiMisma()
        {
            Assert.IsNotNull(BuscarCiclo(new string[0], "A>A"));
        }

        [TestMethod]
        public void Ciclos_ConsideraLasCorrelativasQueYaEstanEnLaBase()
        {
            // En la base ya existe B requiere A; el archivo agrega A requiere B.
            Assert.IsNotNull(BuscarCiclo(new[] { "B|A" }, "A>B"));
            Assert.IsNull(BuscarCiclo(new[] { "B|A" }, "C>B"));
        }

        [TestMethod]
        public void Ciclos_IgnoraMayusculas()
        {
            Assert.IsNotNull(BuscarCiclo(new[] { "a|b" }, "B>A"));
        }
    }
}
