using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BE
{
    [TestClass]
    public class PoliticaBloqueoTests
    {
        [TestMethod]
        public void PrimerIntentoFallido_SumaUnoYNoBloquea()
        {
            global::BE.PoliticaBloqueo.Siguiente(0, false, out int intentos, out bool bloqueado);
            Assert.AreEqual(1, intentos);
            Assert.IsFalse(bloqueado);
        }

        [TestMethod]
        public void ElTercerIntentoFallido_Bloquea()
        {
            global::BE.PoliticaBloqueo.Siguiente(2, false, out int intentos, out bool bloqueado);
            Assert.AreEqual(3, intentos);
            Assert.IsTrue(bloqueado);
            Assert.AreEqual(3, global::BE.PoliticaBloqueo.MaximoIntentos);
        }

        [TestMethod]
        public void UnaCuentaYaBloqueada_SigueBloqueada()
        {
            global::BE.PoliticaBloqueo.Siguiente(0, true, out int intentos, out bool bloqueado);
            Assert.AreEqual(1, intentos);
            Assert.IsTrue(bloqueado);
        }
    }
}
