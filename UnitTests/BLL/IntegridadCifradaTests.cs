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

        private static DataRow FilaUsuario()
        {
            var dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            foreach (string c in new[] { "USUARIO", "PASS", "DIRECCION", "INTENTOS_FALLIDOS", "BLOQUEADO", "ACTIVO" })
                dt.Columns.Add(c, typeof(byte[]));
            DataRow r = dt.NewRow();
            r["ID"] = 3;
            r["USUARIO"] = global::BE.AESHelper.Encriptar("admin");
            r["PASS"] = global::BE.AESHelper.Encriptar(global::BE.HashHelper.HashSHA256("Admin123"));
            r["DIRECCION"] = DBNull.Value;
            r["INTENTOS_FALLIDOS"] = global::BE.AESHelper.EncriptarEntero(0);
            r["BLOQUEADO"] = global::BE.AESHelper.EncriptarBool(false);
            r["ACTIVO"] = global::BE.AESHelper.EncriptarBool(true);
            dt.Rows.Add(r);
            return r;
        }

        [TestMethod]
        public void Usuario_TodosLosAtributosDeNegocioSalenCifrados()
        {
            string[] a = Invocar<string[]>("ExtraerUsuario", FilaUsuario());
            Assert.AreEqual("3", a[0]);
            Assert.AreEqual("", a[3]);
            foreach (int i in new[] { 1, 2, 4, 5, 6 })
                Assert.AreNotEqual(0, Convert.FromBase64String(a[i]).Length, "columna " + i);
            Assert.IsFalse(a.Any(x => x.Contains("admin")));
        }

        [TestMethod]
        public void Usuario_LaRehidratacionDevuelveLosMismosBytesCifrados()
        {
            DataRow fila = FilaUsuario();
            object u = Invocar<object>("RehidratarUsuario", new object[] { Invocar<string[]>("ExtraerUsuario", fila) });
            Type t = u.GetType();
            CollectionAssert.AreEqual((byte[])fila["USUARIO"], (byte[])t.GetField("Usuario").GetValue(u));
            CollectionAssert.AreEqual((byte[])fila["PASS"], (byte[])t.GetField("Pass").GetValue(u));
            Assert.IsNull(t.GetField("Direccion").GetValue(u));
            CollectionAssert.AreEqual((byte[])fila["INTENTOS_FALLIDOS"], (byte[])t.GetField("Intentos").GetValue(u));
            CollectionAssert.AreEqual((byte[])fila["BLOQUEADO"], (byte[])t.GetField("Bloqueado").GetValue(u));
            CollectionAssert.AreEqual((byte[])fila["ACTIVO"], (byte[])t.GetField("Activo").GetValue(u));
        }

        [TestMethod]
        public void Rol_ElNombreYElFlagVanCifrados_YElPadreEnClaro()
        {
            var dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("NOMBRE", typeof(byte[]));
            dt.Columns.Add("PADRE_ID", typeof(int));
            dt.Columns.Add("PROTEGIDO", typeof(byte[]));
            DataRow r = dt.NewRow();
            r["ID"] = 5;
            r["NOMBRE"] = global::BE.AESHelper.Encriptar("Administrador");
            r["PADRE_ID"] = DBNull.Value;
            r["PROTEGIDO"] = global::BE.AESHelper.EncriptarBool(true);
            dt.Rows.Add(r);

            string[] a = Invocar<string[]>("ExtraerRol", r);
            Assert.AreEqual("5", a[0]);
            Assert.AreEqual(Convert.ToBase64String((byte[])r["NOMBRE"]), a[1]);
            Assert.AreEqual("", a[2]);
            Assert.IsFalse(a[1].Contains("Administrador"));

            r["PADRE_ID"] = 2;
            Assert.AreEqual("2", Invocar<string[]>("ExtraerRol", r)[2]);

            object rehidratado = Invocar<object>("RehidratarRol", new object[] { a });
            Type t = rehidratado.GetType();
            CollectionAssert.AreEqual((byte[])r["NOMBRE"], (byte[])t.GetField("Nombre").GetValue(rehidratado));
            CollectionAssert.AreEqual((byte[])r["PROTEGIDO"], (byte[])t.GetField("Protegido").GetValue(rehidratado));
            Assert.IsNull(t.GetField("PadreId").GetValue(rehidratado));
        }

        [TestMethod]
        public void Permiso_ElNombreVaCifrado_YSeRehidrataConLosMismosBytes()
        {
            var dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("NOMBRE", typeof(byte[]));
            DataRow r = dt.NewRow();
            r["ID"] = 9;
            r["NOMBRE"] = global::BE.AESHelper.Encriptar("Gestionar perfiles");
            dt.Rows.Add(r);

            string[] a = Invocar<string[]>("ExtraerPermiso", r);
            Assert.IsFalse(a[1].Contains("Gestionar"));
            object p = Invocar<object>("RehidratarPermiso", new object[] { a });
            CollectionAssert.AreEqual((byte[])r["NOMBRE"], (byte[])p.GetType().GetField("Nombre").GetValue(p));
        }
    }
}
