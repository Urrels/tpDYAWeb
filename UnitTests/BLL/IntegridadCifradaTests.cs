using System;
using System.Data;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BLL
{
    [TestClass]
    public class IntegridadCifradaTests
    {
        private static T Invocar<T>(string nombre, params object[] args)
        {
            MethodInfo m = typeof(global::BLL.IntegridadBLL).GetMethod(nombre, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "no existe " + nombre);
            return (T)m.Invoke(null, args);
        }

        private static DataRow FilaMateria()
        {
            var dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            foreach (string c in new[] { "NOMBRE", "CODIGO", "MODALIDAD", "PESO", "ACTIVA" })
                dt.Columns.Add(c, typeof(byte[]));
            DataRow r = dt.NewRow();
            r["ID"] = 7;
            r["NOMBRE"] = global::BE.AESHelper.Encriptar("Algebra y Geometria");
            r["CODIGO"] = global::BE.AESHelper.Encriptar("MAT01");
            r["MODALIDAD"] = global::BE.AESHelper.Encriptar("Presencial");
            r["PESO"] = global::BE.AESHelper.EncriptarEntero(4);
            r["ACTIVA"] = global::BE.AESHelper.EncriptarBool(true);
            dt.Rows.Add(r);
            return r;
        }

        [TestMethod]
        public void Materia_LosAtributosDeIntegridadSonElTextoCifradoEnBase64()
        {
            string[] atributos = Invocar<string[]>("ExtraerMateria", FilaMateria());
            Assert.AreEqual("7", atributos[0]);
            Assert.AreEqual(Convert.ToBase64String(global::BE.AESHelper.Encriptar("MAT01")), atributos[2]);
            Assert.IsTrue(atributos.Skip(1).All(a => a.Length > 0 && !a.Contains("Algebra") && !a.Contains("Presencial")));
        }

        [TestMethod]
        public void Materia_ElDvhNoNecesitaLaClave_YCambiaSiSeAlteraElCifrado()
        {
            DataRow fila = FilaMateria();
            int original = global::BE.CalculadorDVH.Calcular(Invocar<string[]>("ExtraerMateria", fila));
            byte[] codigo = (byte[])fila["CODIGO"];
            codigo[0] ^= 0xFF;
            fila["CODIGO"] = codigo;
            Assert.AreNotEqual(original, global::BE.CalculadorDVH.Calcular(Invocar<string[]>("ExtraerMateria", fila)));
        }

        [TestMethod]
        public void Materia_LaRehidratacionDevuelveLosMismosBytesCifrados()
        {
            DataRow fila = FilaMateria();
            string[] atributos = Invocar<string[]>("ExtraerMateria", fila);
            object restaurada = Invocar<object>("RehidratarMateria", new object[] { atributos });
            Type t = restaurada.GetType();
            CollectionAssert.AreEqual((byte[])fila["NOMBRE"], (byte[])t.GetField("Nombre").GetValue(restaurada));
            CollectionAssert.AreEqual((byte[])fila["CODIGO"], (byte[])t.GetField("Codigo").GetValue(restaurada));
            CollectionAssert.AreEqual((byte[])fila["ACTIVA"], (byte[])t.GetField("Activa").GetValue(restaurada));
            Assert.AreEqual(7, t.GetField("Id").GetValue(restaurada));
        }
    }
}
