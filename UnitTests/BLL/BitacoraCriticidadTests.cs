using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BLL
{
    [TestClass]
    public class BitacoraCriticidadTests
    {
        // ClasificarCriticidad es privado: se invoca por reflexión para no tocar código de producción.
        private static string Clasificar(string accion)
        {
            MethodInfo m = typeof(global::BLL.BitacoraBLL).GetMethod("ClasificarCriticidad", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "BitacoraBLL.ClasificarCriticidad no existe: si lo renombraste, actualizá este test.");
            return (string)m.Invoke(null, new object[] { accion });
        }

        [TestMethod]
        [DataRow("RESTAURACION_DATOS")]
        [DataRow("RESTAURACION_BACKUP")]
        [DataRow("RECALCULO_INTEGRIDAD")]
        public void AccionesDeIntegridad_SonDeCriticidadAlta(string accion)
        {
            Assert.AreEqual("Alta", Clasificar(accion));
        }

        [TestMethod]
        public void LoginYLogout_SiguenSiendoBaja()
        {
            Assert.AreEqual("Baja", Clasificar("LOGIN"));
            Assert.AreEqual("Baja", Clasificar("LOGOUT"));
        }
    }
}
