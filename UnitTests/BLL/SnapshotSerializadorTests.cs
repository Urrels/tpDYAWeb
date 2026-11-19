using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BLL
{
    [TestClass]
    public class SnapshotSerializadorTests
    {
        private class Fila
        {
            public int Id { get; set; }
            public string[] Atributos { get; set; }
        }

        private static JavaScriptSerializer Crear()
        {
            MethodInfo m = typeof(global::BLL.IntegridadBLL).GetMethod("CrearSerializador", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "falta IntegridadBLL.CrearSerializador");
            return (JavaScriptSerializer)m.Invoke(null, null);
        }

        private static List<Fila> SnapshotGrande()
        {
            string celda = Convert.ToBase64String(global::BE.AESHelper.Encriptar(new string('x', 200)));
            return Enumerable.Range(1, 12000)
                .Select(i => new Fila { Id = i, Atributos = new[] { i.ToString(), celda, celda, celda, celda } })
                .ToList();
        }

        [TestMethod]
        public void ElSerializadorPorDefecto_NoAlcanzaParaUnSnapshotGrande_YElDeLaBllSi()
        {
            var filas = SnapshotGrande();
            Assert.ThrowsException<InvalidOperationException>(() => new JavaScriptSerializer().Serialize(filas));

            var serializador = Crear();
            Assert.AreEqual(int.MaxValue, serializador.MaxJsonLength);
            string json = serializador.Serialize(filas);
            Assert.IsTrue(json.Length > 2 * 1024 * 1024, "el snapshot de prueba debe superar los 2 MB");

            List<Fila> vuelta = serializador.Deserialize<List<Fila>>(json);
            Assert.AreEqual(filas.Count, vuelta.Count);
            Assert.AreEqual(filas[11999].Atributos[1], vuelta[11999].Atributos[1]);
        }
    }
}
