using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BE
{
    [TestClass]
    public class CalculadorDVHTests
    {
        [TestMethod]
        public void Calcular_UnAtributo_SumaCaracterPorPosicion()
        {
            // 'A' = 65 en la posición 1, 'B' = 66 en la posición 2 → 65*1*1 + 66*1*2
            Assert.AreEqual(197, global::BE.CalculadorDVH.Calcular(new[] { "AB" }));
        }

        [TestMethod]
        public void Calcular_VariosAtributos_PesaPorColumna()
        {
            // 'A' en la columna 1 y 'B' en la columna 2 → 65*1*1 + 66*2*1
            Assert.AreEqual(197, global::BE.CalculadorDVH.Calcular(new[] { "A", "B" }));
            Assert.AreEqual(398, global::BE.CalculadorDVH.Calcular(new[] { "A", "B", "C" }));
        }

        [TestMethod]
        public void Calcular_NuloOVacio_ValeCero()
        {
            Assert.AreEqual(0, global::BE.CalculadorDVH.Calcular(new string[0]));
            Assert.AreEqual(0, global::BE.CalculadorDVH.Calcular(new string[] { null }));
            Assert.AreEqual(0, global::BE.CalculadorDVH.Calcular(new[] { "", "" }));
        }

        [TestMethod]
        public void Calcular_EsSensibleAlOrden()
        {
            Assert.AreNotEqual(
                global::BE.CalculadorDVH.Calcular(new[] { "ab" }),
                global::BE.CalculadorDVH.Calcular(new[] { "ba" }));
        }

        [TestMethod]
        public void Calcular_DetectaCualquierCambioDeUnCampo()
        {
            int original = global::BE.CalculadorDVH.Calcular(new[] { "1", "Cursando", "7.5" });
            int alterado = global::BE.CalculadorDVH.Calcular(new[] { "1", "Cursando", "7.6" });
            Assert.AreNotEqual(original, alterado);
        }

        [TestMethod]
        public void CalcularVertical_SumaLaColumnaPorFila()
        {
            var filas = new List<string[]> { new[] { "A", "X" }, new[] { "B", "Y" } };
            Assert.AreEqual(65 * 1 * 1 + 66 * 2 * 1, global::BE.CalculadorDVH.CalcularVertical(filas, 0));
            Assert.AreEqual(88 * 1 * 1 + 89 * 2 * 1, global::BE.CalculadorDVH.CalcularVertical(filas, 1));
        }

        [TestMethod]
        public void CalcularVertical_ColumnaInexistenteOFilaNula_NoSuma()
        {
            var filas = new List<string[]> { new[] { "A" }, null, new[] { "C", "Z" } };
            // La columna 1 solo existe en la fila 3: 'Z' = 90 → 90*3*1
            Assert.AreEqual(90 * 3 * 1, global::BE.CalculadorDVH.CalcularVertical(filas, 1));
            Assert.AreEqual(0, global::BE.CalculadorDVH.CalcularVertical(new List<string[]>(), 0));
        }

        [TestMethod]
        public void CalcularVertical_CambiarUnaFilaCambiaElDigito()
        {
            var a = new List<string[]> { new[] { "1" }, new[] { "2" } };
            var b = new List<string[]> { new[] { "1" }, new[] { "3" } };
            Assert.AreNotEqual(
                global::BE.CalculadorDVH.CalcularVertical(a, 0),
                global::BE.CalculadorDVH.CalcularVertical(b, 0));
        }
    }
}
