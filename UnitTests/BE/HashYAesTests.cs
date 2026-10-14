using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BE
{
    [TestClass]
    public class HashYAesTests
    {
        [TestMethod]
        public void Sha256_VectorConocido()
        {
            Assert.AreEqual("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
                global::BE.HashHelper.HashSHA256("abc"));
            Assert.AreEqual("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                global::BE.HashHelper.HashSHA256(""));
        }

        [TestMethod]
        public void Sha256_EsHexadecimalEnMinusculasDe64Caracteres()
        {
            string hash = global::BE.HashHelper.HashSHA256("Admin123");
            Assert.AreEqual(64, hash.Length);
            Assert.IsTrue(hash.All(c => "0123456789abcdef".IndexOf(c) >= 0));
        }

        [TestMethod]
        public void Sha256_ContrasenasDistintasDanHashDistinto()
        {
            Assert.AreNotEqual(global::BE.HashHelper.HashSHA256("Admin123"), global::BE.HashHelper.HashSHA256("admin123"));
        }

        [TestMethod]
        public void Aes_EncriptarYDesencriptar_DevuelveElOriginal()
        {
            foreach (string texto in new[] { "Av. Corrientes 1234, CABA", "Dirección con ñ y acentos: áéíóú", "x" })
            {
                byte[] cifrado = global::BE.AESHelper.Encriptar(texto);
                Assert.AreEqual(texto, global::BE.AESHelper.Desencriptar(cifrado));
            }
        }

        [TestMethod]
        public void Aes_ElTextoCifradoNoContieneElOriginal()
        {
            string texto = "Calle Falsa 123";
            byte[] cifrado = global::BE.AESHelper.Encriptar(texto);
            Assert.IsNotNull(cifrado);
            Assert.IsFalse(Encoding.UTF8.GetString(cifrado).Contains(texto));
            Assert.AreNotEqual(Encoding.UTF8.GetByteCount(texto), 0);
        }

        [TestMethod]
        public void Aes_VacioONulo_NoCifraNiRompe()
        {
            Assert.IsNull(global::BE.AESHelper.Encriptar(null));
            Assert.IsNull(global::BE.AESHelper.Encriptar(""));
            Assert.AreEqual(string.Empty, global::BE.AESHelper.Desencriptar(null));
            Assert.AreEqual(string.Empty, global::BE.AESHelper.Desencriptar(new byte[0]));
        }
    }
}
