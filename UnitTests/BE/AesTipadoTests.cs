using System;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BE
{
    [TestClass]
    public class AesTipadoTests
    {
        [TestMethod]
        public void Entero_IdaYVuelta_YEsDeterministico()
        {
            foreach (int n in new[] { 0, 1, -7, 2026, int.MaxValue })
            {
                byte[] a = global::BE.AESHelper.EncriptarEntero(n);
                byte[] b = global::BE.AESHelper.EncriptarEntero(n);
                CollectionAssert.AreEqual(a, b);
                Assert.AreEqual(n, global::BE.AESHelper.DesencriptarEntero(a));
            }
            CollectionAssert.AreNotEqual(global::BE.AESHelper.EncriptarEntero(1), global::BE.AESHelper.EncriptarEntero(2));
        }

        [TestMethod]
        public void Bool_IdaYVuelta_YDistingueVerdaderoDeFalso()
        {
            byte[] v = global::BE.AESHelper.EncriptarBool(true);
            byte[] f = global::BE.AESHelper.EncriptarBool(false);
            CollectionAssert.AreNotEqual(v, f);
            Assert.IsTrue(global::BE.AESHelper.DesencriptarBool(v));
            Assert.IsFalse(global::BE.AESHelper.DesencriptarBool(f));
            CollectionAssert.AreEqual(v, global::BE.AESHelper.EncriptarBool(true));
        }

        [TestMethod]
        public void Decimal_UsaFormatoCanonicoDeDosDecimales()
        {
            CollectionAssert.AreEqual(global::BE.AESHelper.EncriptarDecimal(7.5m), global::BE.AESHelper.EncriptarDecimal(7.50m));
            Assert.AreEqual(7.5m, global::BE.AESHelper.DesencriptarDecimal(global::BE.AESHelper.EncriptarDecimal(7.5m)));
            Assert.AreEqual(10m, global::BE.AESHelper.DesencriptarDecimal(global::BE.AESHelper.EncriptarDecimal(10m)));
            Assert.AreEqual(0m, global::BE.AESHelper.DesencriptarDecimal(global::BE.AESHelper.EncriptarDecimal(0m)));
            Assert.IsNull(global::BE.AESHelper.EncriptarDecimal(null));
            Assert.IsNull(global::BE.AESHelper.DesencriptarDecimal(null));
            Assert.IsNull(global::BE.AESHelper.DesencriptarDecimal(DBNull.Value));
        }

        [TestMethod]
        public void Fecha_IdaYVuelta_ConMilisegundos_YNuloEsNulo()
        {
            var fecha = new DateTime(2026, 3, 4, 10, 30, 15, 123);
            byte[] cifrado = global::BE.AESHelper.EncriptarFecha(fecha);
            Assert.AreEqual(fecha, global::BE.AESHelper.DesencriptarFecha(cifrado));
            CollectionAssert.AreEqual(cifrado, global::BE.AESHelper.EncriptarFecha(fecha));
            Assert.IsNull(global::BE.AESHelper.EncriptarFecha(null));
            Assert.IsNull(global::BE.AESHelper.DesencriptarFecha(null));
        }

        [TestMethod]
        public void Texto_AceptaDBNullYNulo()
        {
            Assert.IsNull(global::BE.AESHelper.DesencriptarTexto(DBNull.Value));
            Assert.IsNull(global::BE.AESHelper.DesencriptarTexto(null));
            Assert.AreEqual("hola", global::BE.AESHelper.DesencriptarTexto(global::BE.AESHelper.Encriptar("hola")));
        }

        [TestMethod]
        public void TiposNoDependenDeLaCulturaDelEquipo()
        {
            var original = Thread.CurrentThread.CurrentCulture;
            try
            {
                var fecha = new DateTime(2026, 3, 4, 10, 30, 0);
                Thread.CurrentThread.CurrentCulture = new CultureInfo("es-AR");
                byte[] d1 = global::BE.AESHelper.EncriptarDecimal(7.5m);
                byte[] f1 = global::BE.AESHelper.EncriptarFecha(fecha);
                byte[] n1 = global::BE.AESHelper.EncriptarEntero(-5);
                Thread.CurrentThread.CurrentCulture = new CultureInfo("ar-SA");
                CollectionAssert.AreEqual(d1, global::BE.AESHelper.EncriptarDecimal(7.5m));
                CollectionAssert.AreEqual(f1, global::BE.AESHelper.EncriptarFecha(fecha));
                CollectionAssert.AreEqual(n1, global::BE.AESHelper.EncriptarEntero(-5));
                Assert.AreEqual(7.5m, global::BE.AESHelper.DesencriptarDecimal(d1));
                Assert.AreEqual(fecha, global::BE.AESHelper.DesencriptarFecha(f1));
            }
            finally { Thread.CurrentThread.CurrentCulture = original; }
        }

        [TestMethod]
        public void DatoAlterado_NoRompeLaLectura()
        {
            byte[] basura = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            Assert.AreEqual(global::BE.AESHelper.TextoIlegible, global::BE.AESHelper.DesencriptarTexto(basura));
            Assert.AreEqual(0, global::BE.AESHelper.DesencriptarEntero(basura));
            Assert.IsFalse(global::BE.AESHelper.DesencriptarBool(basura));
            Assert.IsNull(global::BE.AESHelper.DesencriptarDecimal(basura));
            Assert.IsNull(global::BE.AESHelper.DesencriptarFecha(basura));
        }

        [TestMethod]
        public void NormalizadorUsuario_RecortaYPasaAMinusculas()
        {
            Assert.AreEqual("admin", global::BE.Normalizador.Usuario("  Admin "));
            Assert.AreEqual("", global::BE.Normalizador.Usuario(null));
            Assert.AreEqual("i", global::BE.Normalizador.Usuario("I"));
        }

        [TestMethod]
        public void NormalizadorCodigoMateria_RecortaYPasaAMayusculas()
        {
            Assert.AreEqual("MAT-01A", global::BE.Normalizador.CodigoMateria(" mat-01a "));
            Assert.AreEqual("01", global::BE.Normalizador.CodigoMateria("01"));
            Assert.AreEqual("", global::BE.Normalizador.CodigoMateria(null));
        }

        [TestMethod]
        public void UsuarioNormalizado_CifraIgualSinImportarMayusculas()
        {
            CollectionAssert.AreEqual(
                global::BE.AESHelper.Encriptar(global::BE.Normalizador.Usuario("Admin")),
                global::BE.AESHelper.Encriptar(global::BE.Normalizador.Usuario(" ADMIN ")));
        }
    }
}
