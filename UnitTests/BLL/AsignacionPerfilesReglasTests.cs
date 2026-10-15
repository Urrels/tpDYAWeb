using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BLL
{
    [TestClass]
    public class AsignacionPerfilesReglasTests
    {
        private static readonly global::BE.USUARIO Actor = new global::BE.USUARIO { Id = 5, Usuario = "admin" };

        [TestMethod]
        public void SinPerfiles_SeRechaza()
        {
            var ex = Assert.ThrowsException<InvalidOperationException>(() =>
                global::BLL.PerfilBLL.ValidarAsignacionDePerfiles(7, Actor, new HashSet<int> { 1 }, new HashSet<int>()));
            Assert.AreEqual("El usuario debe tener al menos un perfil.", ex.Message);
        }

        [TestMethod]
        public void CambiarLosPropiosPerfiles_SeRechaza()
        {
            var ex = Assert.ThrowsException<InvalidOperationException>(() =>
                global::BLL.PerfilBLL.ValidarAsignacionDePerfiles(Actor.Id, Actor, new HashSet<int> { 1 }, new HashSet<int> { 1, 2 }));
            Assert.AreEqual("No podés cambiar tus propios perfiles.", ex.Message);
        }

        [TestMethod]
        public void GuardarLosPropiosPerfilesSinCambios_EsValido()
        {
            global::BLL.PerfilBLL.ValidarAsignacionDePerfiles(Actor.Id, Actor, new HashSet<int> { 1, 2 }, new HashSet<int> { 2, 1 });
        }

        [TestMethod]
        public void CambiarLosPerfilesDeOtroUsuario_EsValido()
        {
            global::BLL.PerfilBLL.ValidarAsignacionDePerfiles(7, Actor, new HashSet<int> { 1 }, new HashSet<int> { 2, 3 });
        }
    }
}
