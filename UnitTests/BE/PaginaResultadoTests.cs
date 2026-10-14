using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BE
{
    [TestClass]
    public class PaginaResultadoTests
    {
        [DataTestMethod]
        [DataRow(0, 10, 1)]      // sin filas hay una página (vacía)
        [DataRow(1, 10, 1)]
        [DataRow(10, 10, 1)]
        [DataRow(11, 10, 2)]
        [DataRow(100, 10, 10)]
        [DataRow(101, 10, 11)]
        [DataRow(5, 0, 1)]       // tamaño inválido → no divide por cero
        [DataRow(-3, 10, 1)]
        public void TotalPaginas(int total, int tamanio, int esperado)
        {
            var r = new global::BE.PaginaResultado<string> { TotalFilas = total, Tamanio = tamanio };
            Assert.AreEqual(esperado, r.TotalPaginas);
        }

        [TestMethod]
        public void ValoresPorDefecto()
        {
            var r = new global::BE.PaginaResultado<int>();
            Assert.AreEqual(0, r.Filas.Count);
            Assert.AreEqual(1, r.Pagina);
            Assert.AreEqual(1, r.TotalPaginas);
        }
    }
}
