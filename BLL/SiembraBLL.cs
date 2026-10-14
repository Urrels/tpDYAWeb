using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BE;

namespace BLL
{
    /// <summary>
    /// Siembra de datos base de seguridad al iniciar la aplicación: catálogo de
    /// permisos, perfiles base, su matriz de permisos y los usuarios iniciales.
    /// Cada fila nace con su DVH/DVV calculado por el motor de integridad de C#.
    ///
    /// La entrada SIEMBRA_DATOS_BASE de la bitácora es el marcador de "siembra ya
    /// realizada"; se escribe en la misma transacción que la siembra. Su ausencia no
    /// alcanza para sembrar: el marcador se puede perder (por ejemplo al restaurar un
    /// backup JSON de la bitácora) y una base migrada nunca lo tuvo. Por eso:
    ///  - Los usuarios iniciales (admin, webmaster) se crean solo si no hay marcador y
    ///    además USUARIO y ROL están vacías (instalación nueva).
    ///  - Los perfiles base y su matriz se crean solo si no hay marcador y ROL está
    ///    vacía (instalación nueva, o base migrada cuyos usuarios esperan su perfil);
    ///    en ese caso también se asignan los perfiles pendientes de MIGRACION_USUARIO_ROL.
    ///  - En cualquier otro caso (marcador presente, o datos existentes sin marcador)
    ///    no se crean perfiles ni usuarios: si un administrador los borró o renombró,
    ///    no se recrean (admin nacería con una contraseña conocida). Solo se agregan
    ///    los permisos del catálogo que falten y, para esos permisos recién creados,
    ///    se aplica la matriz por defecto a los perfiles base que existan por nombre
    ///    (Administrador y Webmaster solo si son el perfil protegido del sistema).
    ///    Nunca se vuelve a otorgar un permiso que un administrador quitó.
    ///  - Si no hay marcador, se escribe igual aunque no se haya insertado nada.
    /// </summary>
    public class SiembraBLL
    {
        private const string UsuarioAdmin = "admin";
        private const string ContrasenaAdmin = "Admin123";
        private const string UsuarioWebmaster = "webmaster";
        private const string ContrasenaWebmaster = "Webmaster123";

        // Perfiles base. Administrador y Webmaster son del sistema (protegidos).
        private static readonly KeyValuePair<string, bool>[] PerfilesBase =
        {
            new KeyValuePair<string, bool>(Permisos.PerfilAdministrador, true),
            new KeyValuePair<string, bool>(Permisos.PerfilWebmaster, true),
            new KeyValuePair<string, bool>(Permisos.PerfilAlumno, false)
        };

        // Matriz de permisos por defecto de cada perfil base.
        private static readonly Dictionary<string, string[]> MatrizBase = new Dictionary<string, string[]>
        {
            { Permisos.PerfilAdministrador, new[]
                {
                    Permisos.GestionarMaterias, Permisos.CargarNotas, Permisos.VerBitacora,
                    Permisos.DesbloquearUsuarios, Permisos.GestionarPeriodos, Permisos.GestionarPerfiles,
                    Permisos.GestionarUsuarios, Permisos.EditarPerfil, Permisos.CambiarContrasena
                } },
            // Solo el Webmaster puede ingresar con la integridad inválida. Gestiona y desbloquea
            // las cuentas webmaster (el Administrador no las cubre y no puede hacerlo).
            { Permisos.PerfilWebmaster, new[]
                {
                    Permisos.VerificarIntegridad, Permisos.IngresarConIntegridadInvalida, Permisos.GestionarUsuarios,
                    Permisos.DesbloquearUsuarios
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

        // Tablas que la siembra misma dejó sin DVV/snapshot (los descartó dentro de su
        // transacción) y cuyo recálculo todavía no se completó. Vive en memoria a propósito:
        // solo estas tablas se recalculan solas en un reintento. Una tabla a la que alguien
        // le quitó el DVV desde afuera no figura acá y sigue figurando como inválida.
        private static readonly object _lockPendientes = new object();
        private static readonly HashSet<string> _tablasPendientes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Todos los permisos del sistema: las constantes de <see cref="Permisos"/>.</summary>
        public static List<string> CatalogoDePermisos()
        {
            return typeof(Permisos).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string) && !f.Name.StartsWith("Perfil"))
                .Select(f => (string)f.GetRawConstantValue())
                .ToList();
        }

        /// <returns>true si insertó alguna fila de datos base.</returns>
        public bool SembrarDatosBase()
        {
            // Reintento: completa primero lo que una corrida anterior dejó a medias.
            RecalcularPendientes();

            var permisosExistentes = new HashSet<string>(
                _perfilDal.ListarPermisosDisponibles().Select(p => p.Nombre), StringComparer.OrdinalIgnoreCase);
            // nombre -> protegido
            var rolesExistentes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var rol in _perfilDal.ListarRoles())
                rolesExistentes[rol.Nombre] = rol.Protegido;
            var usuariosExistentes = new HashSet<string>(
                _usuarioDal.ListarTodos().Select(u => u.Usuario), StringComparer.OrdinalIgnoreCase);

            // Integridad de cada tabla ANTES de tocarla: solo si estaba íntegra se la
            // recalcula después de sembrar (así no se "limpia" una alteración previa).
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

            // El marcador se escribe si falta, o si esta corrida agregó algo (no en cada arranque).
            plan.EscribirMarcador = !marcador
                || plan.Permisos.Count + plan.Roles.Count + plan.RolPermisos.Count + plan.Usuarios.Count > 0;

            DAL.ResultadoSiembra r = _siembraDal.Aplicar(plan);

            // Las tablas tocadas que estaban íntegras quedaron sin DVV/snapshot dentro de la
            // transacción; se registran como pendientes ANTES de recalcular, así un fallo
            // de recálculo se reintenta. Las que no estaban íntegras no se tocan (no se
            // "limpia" una alteración previa).
            lock (_lockPendientes)
            {
                if (r.Permisos > 0 && permisoOk) _tablasPendientes.Add("PERMISO");
                if (r.Roles > 0 && rolOk) _tablasPendientes.Add("ROL");
                if (r.RolPermisos > 0 && rolPermisoOk) _tablasPendientes.Add("ROL_PERMISO");
                if (r.Usuarios > 0 && usuarioOk) _tablasPendientes.Add("USUARIO");
                if (r.UsuarioPerfiles > 0 && usuarioPerfilOk) _tablasPendientes.Add("USUARIO_PERFIL");
                // El marcador ya está en la base (misma transacción); falta su DVH.
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

        // Perfil base existente al que se le puede aplicar la matriz por defecto.
        // Administrador y Webmaster solo si es el perfil protegido del sistema: un
        // perfil común que un administrador creó con ese nombre no debe heredarla.
        private static bool PerfilBaseVigente(string nombre, Dictionary<string, bool> rolesExistentes)
        {
            if (!rolesExistentes.TryGetValue(nombre, out bool protegido)) return false;
            bool delSistema = PerfilesBase.Any(p => p.Value && string.Equals(p.Key, nombre, StringComparison.OrdinalIgnoreCase));
            return !delSistema || protegido;
        }
    }
}
