using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BE
{
    [TestClass]
    public class UsuarioPermisosSesionTests
    {
        // ---------- USUARIO ----------

        [TestMethod]
        public void Usuario_TienePermiso_SegunSuListaEfectiva()
        {
            var u = new global::BE.USUARIO { Permisos = new List<string> { "Ver bitacora", "Gestionar materias" } };
            Assert.IsTrue(u.TienePermiso("Ver bitacora"));
            Assert.IsFalse(u.TienePermiso("Gestionar usuarios"));
        }

        [TestMethod]
        public void Usuario_SinPermisos_NoTieneNinguno()
        {
            Assert.IsFalse(new global::BE.USUARIO().TienePermiso("X"));
            Assert.IsFalse(new global::BE.USUARIO { Permisos = null }.TienePermiso("X"));
        }

        [TestMethod]
        public void Usuario_NuevoEstaActivoYNoBloqueado()
        {
            var u = new global::BE.USUARIO();
            Assert.IsTrue(u.Activo);
            Assert.IsFalse(u.Bloqueado);
        }

        // ---------- Permisos ----------

        [TestMethod]
        public void Permisos_Privilegiados_SonLosCincoDeSeguridad()
        {
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "Gestionar perfiles", "Gestionar usuarios", "Verificar integridad",
                    "Ingresar con integridad invalida", "Desbloquear usuarios"
                },
                global::BE.Permisos.Privilegiados);
        }

        [TestMethod]
        public void Permisos_EsPrivilegiado()
        {
            Assert.IsTrue(global::BE.Permisos.EsPrivilegiado(global::BE.Permisos.GestionarPerfiles));
            Assert.IsTrue(global::BE.Permisos.EsPrivilegiado(global::BE.Permisos.DesbloquearUsuarios));
            Assert.IsFalse(global::BE.Permisos.EsPrivilegiado(global::BE.Permisos.VerBitacora));
            Assert.IsFalse(global::BE.Permisos.EsPrivilegiado(global::BE.Permisos.GestionarIdiomas));
            Assert.IsFalse(global::BE.Permisos.EsPrivilegiado("no existe"));
        }

        // ---------- SessionRegistry (sesión única por usuario) ----------

        private static string UsuarioUnico() => "ut_" + Guid.NewGuid().ToString("N");

        [TestMethod]
        public void SessionRegistry_EsUnSingleton()
        {
            Assert.AreSame(global::BE.SessionRegistry.Instancia, global::BE.SessionRegistry.Instancia);
        }

        [TestMethod]
        public void SessionRegistry_RegistrarYCerrar()
        {
            var reg = global::BE.SessionRegistry.Instancia;
            string u = UsuarioUnico();
            Assert.IsFalse(reg.TieneSesionActiva(u));

            reg.RegistrarSesion(u, "S1");
            Assert.IsTrue(reg.TieneSesionActiva(u));

            reg.CerrarSesion(u, "S1");
            Assert.IsFalse(reg.TieneSesionActiva(u));
        }

        [TestMethod]
        public void SessionRegistry_CerrarConOtroId_NoBorraLaSesionVigente()
        {
            var reg = global::BE.SessionRegistry.Instancia;
            string u = UsuarioUnico();
            reg.RegistrarSesion(u, "NUEVA");

            reg.CerrarSesion(u, "VIEJA");   // el cierre tardío de una sesión vieja
            Assert.IsTrue(reg.TieneSesionActiva(u));

            reg.CerrarSesion(u, "NUEVA");
            Assert.IsFalse(reg.TieneSesionActiva(u));
        }

        [TestMethod]
        public void SessionRegistry_ForzarCierre_LiberaSeaCualSeaLaSesion()
        {
            var reg = global::BE.SessionRegistry.Instancia;
            string u = UsuarioUnico();
            reg.RegistrarSesion(u, "S1");
            reg.ForzarCierre(u);
            Assert.IsFalse(reg.TieneSesionActiva(u));
        }

        [TestMethod]
        public void SessionRegistry_UsuariosIndependientes()
        {
            var reg = global::BE.SessionRegistry.Instancia;
            string a = UsuarioUnico(), b = UsuarioUnico();
            reg.RegistrarSesion(a, "SA");
            Assert.IsTrue(reg.TieneSesionActiva(a));
            Assert.IsFalse(reg.TieneSesionActiva(b));
            reg.ForzarCierre(a);
        }

        [TestMethod]
        public void SessionRegistry_AguantaAccesoConcurrente()
        {
            var reg = global::BE.SessionRegistry.Instancia;
            var usuarios = Enumerable.Range(0, 200).Select(i => UsuarioUnico()).ToList();
            System.Threading.Tasks.Parallel.ForEach(usuarios, u =>
            {
                reg.RegistrarSesion(u, "S");
                Assert.IsTrue(reg.TieneSesionActiva(u));
                reg.CerrarSesion(u, "S");
            });
            Assert.IsTrue(usuarios.All(u => !reg.TieneSesionActiva(u)));
        }
    }
}
