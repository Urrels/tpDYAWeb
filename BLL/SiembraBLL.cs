using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BE;

namespace BLL
{
    public class SiembraBLL
    {
        private const string UsuarioAdmin = "admin";
        private const string ContrasenaAdmin = "Admin123";
        private const string UsuarioWebmaster = "webmaster";
        private const string ContrasenaWebmaster = "Webmaster123";

        private static readonly KeyValuePair<string, bool>[] PerfilesBase =
        {
            new KeyValuePair<string, bool>(Permisos.PerfilAdministrador, true),
            new KeyValuePair<string, bool>(Permisos.PerfilWebmaster, true),
            new KeyValuePair<string, bool>(Permisos.PerfilAlumno, false)
        };

        private static readonly Dictionary<string, string[]> MatrizBase = new Dictionary<string, string[]>
        {
            { Permisos.PerfilAdministrador, new[]
                {
                    Permisos.GestionarMaterias, Permisos.CargarNotas, Permisos.VerBitacora,
                    Permisos.DesbloquearUsuarios, Permisos.GestionarPeriodos, Permisos.GestionarPerfiles,
                    Permisos.GestionarUsuarios, Permisos.GestionarIdiomas, Permisos.EditarPerfil, Permisos.CambiarContrasena
                } },
            { Permisos.PerfilWebmaster, new[]
                {
                    Permisos.VerificarIntegridad, Permisos.IngresarConIntegridadInvalida, Permisos.GestionarUsuarios,
                    Permisos.DesbloquearUsuarios, Permisos.GestionarIdiomas
                } },
            { Permisos.PerfilAlumno, new[]
                {
                    Permisos.VerMenuAlumno, Permisos.Inscribirse, Permisos.VerMisCursadas, Permisos.VerDashboard,
                    Permisos.ExportarPdf, Permisos.VerCalendario, Permisos.EditarPerfil, Permisos.CambiarContrasena
                } }
        };

        private readonly DAL.PerfilDAL _perfilDal = new DAL.PerfilDAL();
        private readonly DAL.UsuarioDAL _usuarioDal = new DAL.UsuarioDAL();
        private readonly DAL.SiembraDAL _siembraDal = new DAL.SiembraDAL();
        private readonly IntegridadBLL _integridad = new IntegridadBLL();

        private static readonly object _lockPendientes = new object();
        private static readonly HashSet<string> _tablasPendientes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static List<string> CatalogoDePermisos()
        {
            return typeof(Permisos).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string) && !f.Name.StartsWith("Perfil"))
                .Select(f => (string)f.GetRawConstantValue())
                .ToList();
        }

        public bool SembrarDatosBase()
        {
            RecalcularPendientes();

            var permisosExistentes = new HashSet<string>(
                _perfilDal.ListarPermisosDisponibles().Select(p => p.Nombre), StringComparer.OrdinalIgnoreCase);
            var rolesExistentes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var rol in _perfilDal.ListarRoles())
                rolesExistentes[rol.Nombre] = rol.Protegido;
            var usuariosExistentes = new HashSet<string>(
                _usuarioDal.ListarTodos().Select(u => u.Usuario), StringComparer.OrdinalIgnoreCase);

            bool permisoOk = _integridad.VerificarPermiso().EsValido;
            bool rolOk = _integridad.VerificarRol().EsValido;
            bool rolPermisoOk = _integridad.VerificarRolPermiso().EsValido;
            bool usuarioOk = _integridad.VerificarUsuario().EsValido;
            bool usuarioPerfilOk = _integridad.VerificarUsuarioPerfil().EsValido;
            bool bitacoraOk = _integridad.VerificarBitacora().EsValido;

            bool marcador = _siembraDal.ExisteMarcadorSiembra();
            bool crearPerfiles = !marcador && rolesExistentes.Count == 0;
            bool crearUsuarios = crearPerfiles && usuariosExistentes.Count == 0;

            var plan = new DAL.PlanSiembra { IncluirMigracionDePerfiles = crearPerfiles };
            if (bitacoraOk) plan.TablasIntegras.Add("BITACORA");
            if (permisoOk) plan.TablasIntegras.Add("PERMISO");
            if (rolOk) plan.TablasIntegras.Add("ROL");
            if (rolPermisoOk) plan.TablasIntegras.Add("ROL_PERMISO");
            if (usuarioOk) plan.TablasIntegras.Add("USUARIO");
            if (usuarioPerfilOk) plan.TablasIntegras.Add("USUARIO_PERFIL");

            var permisosNuevos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string nombre in CatalogoDePermisos())
                if (!permisosExistentes.Contains(nombre)) { plan.Permisos.Add(nombre); permisosNuevos.Add(nombre); }

            var rolesNuevos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var perfil in PerfilesBase)
                if (crearPerfiles && !rolesExistentes.ContainsKey(perfil.Key)) { plan.Roles.Add(perfil); rolesNuevos.Add(perfil.Key); }

            foreach (var perfil in MatrizBase)
                foreach (string permiso in perfil.Value)
                    if (rolesNuevos.Contains(perfil.Key) || (permisosNuevos.Contains(permiso) && PerfilBaseVigente(perfil.Key, rolesExistentes)))
                        plan.RolPermisos.Add(new KeyValuePair<string, string>(perfil.Key, permiso));

            if (crearUsuarios)
                plan.Usuarios.Add(new DAL.UsuarioSembrado
                    { Usuario = UsuarioAdmin, PassHash = HashHelper.HashSHA256(ContrasenaAdmin), Rol = Permisos.PerfilAdministrador });
            if (crearUsuarios)
                plan.Usuarios.Add(new DAL.UsuarioSembrado
                    { Usuario = UsuarioWebmaster, PassHash = HashHelper.HashSHA256(ContrasenaWebmaster), Rol = Permisos.PerfilWebmaster });

            plan.EscribirMarcador = !marcador
                || plan.Permisos.Count + plan.Roles.Count + plan.RolPermisos.Count + plan.Usuarios.Count > 0;

            DAL.ResultadoSiembra r = _siembraDal.Aplicar(plan);

            lock (_lockPendientes)
            {
                if (r.Permisos > 0 && permisoOk) _tablasPendientes.Add("PERMISO");
                if (r.Roles > 0 && rolOk) _tablasPendientes.Add("ROL");
                if (r.RolPermisos > 0 && rolPermisoOk) _tablasPendientes.Add("ROL_PERMISO");
                if (r.Usuarios > 0 && usuarioOk) _tablasPendientes.Add("USUARIO");
                if (r.UsuarioPerfiles > 0 && usuarioPerfilOk) _tablasPendientes.Add("USUARIO_PERFIL");
                if (r.Marcador && bitacoraOk) _tablasPendientes.Add("BITACORA");
            }
            RecalcularPendientes();
            return r.Algo;
        }

        private void RecalcularPendientes()
        {
            string[] tablas;
            lock (_lockPendientes) tablas = _tablasPendientes.ToArray();
            foreach (string tabla in tablas)
            {
                switch (tabla)
                {
                    case "PERMISO": _integridad.RecalcularPermiso(); break;
                    case "ROL": _integridad.RecalcularRol(); break;
                    case "ROL_PERMISO": _integridad.RecalcularRolPermiso(); break;
                    case "USUARIO": _integridad.RecalcularUsuario(); break;
                    case "USUARIO_PERFIL": _integridad.RecalcularUsuarioPerfil(); break;
                    case "BITACORA": _integridad.RecalcularBitacora(); break;
                }
                lock (_lockPendientes) _tablasPendientes.Remove(tabla);
            }
        }

        private static bool PerfilBaseVigente(string nombre, Dictionary<string, bool> rolesExistentes)
        {
            if (!rolesExistentes.TryGetValue(nombre, out bool protegido)) return false;
            bool delSistema = PerfilesBase.Any(p => p.Value && string.Equals(p.Key, nombre, StringComparison.OrdinalIgnoreCase));
            return !delSistema || protegido;
        }
    }
}
