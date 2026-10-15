using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BLL
{
    [TestClass]
    public class NotasValidacionTests
    {
        private System.Globalization.CultureInfo _cultura;

        // Cultura fija para que cada test pruebe solo lo que dice su nombre (punto decimal, sin miles).
        [TestInitialize]
        public void FijarCultura()
        {
            _cultura = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
        }

        [TestCleanup]
        public void RestaurarCultura() => System.Threading.Thread.CurrentThread.CurrentCulture = _cultura;

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
        [DataRow("10.01")]
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

        [TestMethod]
        public void NotaConDecimales_SeAceptaConElSeparadorDeLaCultura()
        {
            Assert.IsTrue(global::BLL.AlumnoMateriaBLL.TryParsearNota("7.5", out decimal? nota));
            Assert.AreEqual(7.5m, nota);
        }

        [TestMethod]
        public void SeparadorDeMiles_SeRechaza()
        {
            Assert.IsFalse(global::BLL.AlumnoMateriaBLL.TryParsearNota("1,000", out decimal? nota));
            Assert.IsNull(nota);
        }
    }
}
