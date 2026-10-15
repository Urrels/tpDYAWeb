using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BLL
{
    [TestClass]
    public class NotasValidacionTests
    {
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void NotaVacia_EsValidaYQuedaSinCargar(string texto)
        {
            Assert.IsTrue(global::BLL.AlumnoMateriaBLL.TryParsearNota(texto, out decimal? nota));
            Assert.IsNull(nota);
        }

        [TestMethod]
        [DataRow("0", 0)]
        [DataRow("10", 10)]
        [DataRow("7", 7)]
        [DataRow(" 4 ", 4)]
        public void NotaEnRango_SeAcepta(string texto, int esperado)
        {
            Assert.IsTrue(global::BLL.AlumnoMateriaBLL.TryParsearNota(texto, out decimal? nota));
            Assert.AreEqual((decimal)esperado, nota);
        }

        [TestMethod]
        [DataRow("-1")]
        [DataRow("10.5")]
        [DataRow("11")]
        [DataRow("100")]
        public void NotaFueraDeRango_SeRechazaSinRecortar(string texto)
        {
            Assert.IsFalse(global::BLL.AlumnoMateriaBLL.TryParsearNota(texto, out decimal? nota));
            Assert.IsNull(nota);
        }

        [TestMethod]
        [DataRow("abc")]
        [DataRow("7x")]
        [DataRow("1e1")]
        public void NotaNoNumerica_SeRechazaEnLugarDeLanzarExcepcion(string texto)
        {
            Assert.IsFalse(global::BLL.AlumnoMateriaBLL.TryParsearNota(texto, out decimal? nota));
            Assert.IsNull(nota);
        }
    }
}
