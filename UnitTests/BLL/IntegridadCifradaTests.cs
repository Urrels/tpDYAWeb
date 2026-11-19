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

        private static DataRow FilaCon(params object[] paresNombreValor)
        {
            var dt = new DataTable();
            for (int i = 0; i < paresNombreValor.Length; i += 2)
            {
                object valor = paresNombreValor[i + 1];
                dt.Columns.Add((string)paresNombreValor[i], valor is byte[] ? typeof(byte[]) : typeof(int));
            }
            DataRow r = dt.NewRow();
            for (int i = 0; i < paresNombreValor.Length; i += 2)
                r[(string)paresNombreValor[i]] = paresNombreValor[i + 1];
            dt.Rows.Add(r);
            return r;
        }

        private static void AssertCifradoEnBase64(string[] atributos, int indice, byte[] esperado)
        {
            Assert.AreEqual(Convert.ToBase64String(esperado), atributos[indice], "atributo " + indice);
        }

        [TestMethod]
        public void AlumnoMateria_NotasFechasYEstadoSalenCifrados_YLosNulosQuedanVacios()
        {
            byte[] estado = global::BE.AESHelper.Encriptar("Cursando");
            byte[] nota = global::BE.AESHelper.EncriptarDecimal(7.5m);
            byte[] fecha = global::BE.AESHelper.EncriptarFecha(new DateTime(2026, 3, 4));
            DataRow r = FilaCon("ID", 1, "ID_USUARIO", 2, "ID_MATERIA", 3, "ESTADO", estado,
                "NOTA_PARCIAL1", nota, "NOTA_PARCIAL2", DBNull.Value, "NOTA_RECUPERATORIO", DBNull.Value,
                "NOTA_FINAL", DBNull.Value, "FECHA_FINAL", fecha, "FECHA_RECUPERATORIO", DBNull.Value);
            string[] a = Invocar<string[]>("ExtraerAlumnoMateria", r);

            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, a.Take(3).ToArray());
            AssertCifradoEnBase64(a, 3, estado);
            AssertCifradoEnBase64(a, 4, nota);
            Assert.AreEqual("", a[5]);
            AssertCifradoEnBase64(a, 8, fecha);
            Assert.AreEqual("", a[9]);
            Assert.IsFalse(a.Any(x => x.Contains("7.5") || x.Contains("Cursando")));

            object h = Invocar<object>("RehidratarAlumnoMateria", new object[] { a });
            Type t = h.GetType();
            CollectionAssert.AreEqual(estado, (byte[])t.GetField("Estado").GetValue(h));
            CollectionAssert.AreEqual(nota, (byte[])t.GetField("NotaParcial1").GetValue(h));
            Assert.IsNull(t.GetField("NotaParcial2").GetValue(h));
            CollectionAssert.AreEqual(fecha, (byte[])t.GetField("FechaFinal").GetValue(h));
        }

        [TestMethod]
        public void EventoAcademico_TipoDescripcionFechaYPesoSalenCifrados()
        {
            byte[] tipo = global::BE.AESHelper.Encriptar("Parcial");
            byte[] desc = global::BE.AESHelper.Encriptar("Primer parcial de Base de Datos");
            byte[] fecha = global::BE.AESHelper.EncriptarFecha(new DateTime(2026, 5, 20));
            byte[] peso = global::BE.AESHelper.EncriptarEntero(3);
            DataRow r = FilaCon("ID", 4, "ID_MATERIA", 5, "ID_USUARIO", 6, "TIPO", tipo, "DESCRIPCION", desc, "FECHA", fecha, "PESO", peso);
            string[] a = Invocar<string[]>("ExtraerEventoAcademico", r);

            CollectionAssert.AreEqual(new[] { "4", "5", "6" }, a.Take(3).ToArray());
            AssertCifradoEnBase64(a, 3, tipo);
            AssertCifradoEnBase64(a, 4, desc);
            AssertCifradoEnBase64(a, 5, fecha);
            AssertCifradoEnBase64(a, 6, peso);

            object h = Invocar<object>("RehidratarEventoAcademico", new object[] { a });
            Type t = h.GetType();
            CollectionAssert.AreEqual(desc, (byte[])t.GetField("Descripcion").GetValue(h));
            CollectionAssert.AreEqual(fecha, (byte[])t.GetField("Fecha").GetValue(h));
        }

        [TestMethod]
        public void PeriodoAcademico_TodoSalvoElIdSaleCifrado()
        {
            byte[] anio = global::BE.AESHelper.EncriptarEntero(2026);
            byte[] cuatri = global::BE.AESHelper.EncriptarEntero(2);
            byte[] fin = global::BE.AESHelper.EncriptarFecha(new DateTime(2026, 12, 1));
            DataRow r = FilaCon("ID", 8, "ANIO", anio, "CUATRIMESTRE", cuatri, "DESCRIPCION", DBNull.Value,
                "FECHA_INICIO", DBNull.Value, "FECHA_FIN", fin);
            string[] a = Invocar<string[]>("ExtraerPeriodoAcademico", r);

            Assert.AreEqual("8", a[0]);
            AssertCifradoEnBase64(a, 1, anio);
            AssertCifradoEnBase64(a, 2, cuatri);
            Assert.AreEqual("", a[3]);
            AssertCifradoEnBase64(a, 5, fin);

            object h = Invocar<object>("RehidratarPeriodoAcademico", new object[] { a });
            Type t = h.GetType();
            CollectionAssert.AreEqual(anio, (byte[])t.GetField("Anio").GetValue(h));
            Assert.IsNull(t.GetField("FechaInicio").GetValue(h));
        }

        [TestMethod]
        public void Inscripcion_LaFechaSaleCifrada_YLosIdsEnClaro()
        {
            byte[] fecha = global::BE.AESHelper.EncriptarFecha(new DateTime(2026, 8, 1, 9, 30, 0));
            DataRow r = FilaCon("ID", 11, "ID_USUARIO", 2, "ID_PERIODO", 8, "FECHA_INSCRIPCION", fecha);
            string[] a = Invocar<string[]>("ExtraerInscripcion", r);
            CollectionAssert.AreEqual(new[] { "11", "2", "8" }, a.Take(3).ToArray());
            AssertCifradoEnBase64(a, 3, fecha);

            object h = Invocar<object>("RehidratarInscripcion", new object[] { a });
            CollectionAssert.AreEqual(fecha, (byte[])h.GetType().GetField("FechaInscripcion").GetValue(h));
        }

        [TestMethod]
        public void AlumnoMateria_ElDvhSeCalculaSobreElCifrado_YCambiaSiCambiaUnaNota()
        {
            DataRow Cursada(decimal nota) => FilaCon("ID", 1, "ID_USUARIO", 2, "ID_MATERIA", 3,
                "ESTADO", global::BE.AESHelper.Encriptar("Cursando"), "NOTA_PARCIAL1", global::BE.AESHelper.EncriptarDecimal(nota),
                "NOTA_PARCIAL2", DBNull.Value, "NOTA_RECUPERATORIO", DBNull.Value, "NOTA_FINAL", DBNull.Value,
                "FECHA_FINAL", DBNull.Value, "FECHA_RECUPERATORIO", DBNull.Value);

            int a = global::BE.CalculadorDVH.Calcular(Invocar<string[]>("ExtraerAlumnoMateria", Cursada(7)));
            int b = global::BE.CalculadorDVH.Calcular(Invocar<string[]>("ExtraerAlumnoMateria", Cursada(7)));
            int c = global::BE.CalculadorDVH.Calcular(Invocar<string[]>("ExtraerAlumnoMateria", Cursada(8)));
            Assert.AreEqual(a, b);
            Assert.AreNotEqual(a, c);
        }
    }
}
