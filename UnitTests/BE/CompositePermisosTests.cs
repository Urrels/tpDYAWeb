using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using global::BE;

namespace UnitTests.BE
{
    [TestClass]
    public class CompositePermisosTests
    {
        private static Permiso P(string nombre) => new Permiso { Nombre = nombre };

        private static Rol R(string nombre, params NodoPermiso[] hijos)
        {
            var rol = new Rol { Nombre = nombre };
            foreach (var h in hijos) rol.Agregar(h);
            return rol;
        }

        [TestMethod]
        public void Permiso_EsHoja_ConcedeSoloSuNombre()
        {
            var p = P("Ver bitacora");
            Assert.IsTrue(p.EsHoja());
            Assert.IsTrue(p.TienePermiso("Ver bitacora"));
            Assert.IsFalse(p.TienePermiso("Otro"));
            CollectionAssert.AreEqual(new[] { "Ver bitacora" }, p.ObtenerPermisos().ToArray());
            Assert.AreEqual(0, p.ObtenerHijos().Count);
        }

        [TestMethod]
        public void Permiso_NoAdmiteHijos()
        {
            var p = P("A");
            Assert.ThrowsException<InvalidOperationException>(() => p.Agregar(P("B")));
            Assert.ThrowsException<InvalidOperationException>(() => p.Quitar(P("B")));
        }

        [TestMethod]
        public void Rol_NoEsHoja_ConcedeLosPermisosPropios()
        {
            var rol = R("Alumno", P("Inscribirse"), P("Ver calendario"));
            Assert.IsFalse(rol.EsHoja());
            Assert.IsTrue(rol.TienePermiso("Inscribirse"));
            Assert.IsFalse(rol.TienePermiso("Gestionar usuarios"));
        }

        [TestMethod]
        public void Rol_HeredaLosPermisosDeSusHijos_EnVariosNiveles()
        {
            var nieto = R("Nieto", P("C"));
            var hijo = R("Hijo", P("B"), nieto);
            var padre = R("Padre", P("A"), hijo);

            Assert.IsTrue(padre.TienePermiso("A"));
            Assert.IsTrue(padre.TienePermiso("B"));
            Assert.IsTrue(padre.TienePermiso("C"));
            Assert.IsFalse(hijo.TienePermiso("A"), "un hijo no hereda del padre, es el padre quien hereda");
            CollectionAssert.AreEquivalent(new[] { "A", "B", "C" }, padre.ObtenerPermisos().ToArray());
        }

        [TestMethod]
        public void Rol_ObtenerPermisos_NoRepiteLosDuplicados()
        {
            var padre = R("Padre", P("A"), R("Hijo", P("A"), P("B")));
            var permisos = padre.ObtenerPermisos().ToList();
            Assert.AreEqual(2, permisos.Count);
            CollectionAssert.AreEquivalent(new[] { "A", "B" }, permisos);
        }

        [TestMethod]
        public void Rol_Quitar_DejaDeConcederElPermiso()
        {
            var a = P("A");
            var rol = R("Rol", a, P("B"));
            rol.Quitar(a);
            Assert.IsFalse(rol.TienePermiso("A"));
            Assert.IsTrue(rol.TienePermiso("B"));
        }

        [TestMethod]
        public void Rol_Vacio_NoConcedeNada()
        {
            var rol = R("Vacio");
            Assert.IsFalse(rol.TienePermiso("A"));
            Assert.AreEqual(0, rol.ObtenerPermisos().Count());
        }

        [TestMethod]
        public void Rol_ConReferenciaCircular_NoSeCuelga()
        {
            var a = R("A", P("PA"));
            var b = R("B", P("PB"));
            a.Agregar(b);
            b.Agregar(a);

            Assert.IsTrue(a.TienePermiso("PB"));
            Assert.IsFalse(a.TienePermiso("inexistente"));
            CollectionAssert.AreEquivalent(new[] { "PA", "PB" }, a.ObtenerPermisos().ToArray());
            // Una segunda consulta da lo mismo: la marca de "visitando" se limpia.
            CollectionAssert.AreEquivalent(new[] { "PA", "PB" }, a.ObtenerPermisos().ToArray());
        }

        [TestMethod]
        public void NodoPermiso_ToString_EsElNombre()
        {
            Assert.AreEqual("Administrador", R("Administrador").ToString());
        }
    }
}
