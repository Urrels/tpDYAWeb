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

        private static BE.MATERIA Materia(string codigo, string nombre, int peso) =>
            new BE.MATERIA { Codigo = codigo, Nombre = nombre, Modalidad = "Presencial", Peso = peso };

        private static readonly BE.MATERIA[] PlanMaterias =
        {
            Materia("01", "Introducción a los Algoritmos y la Programación", 4),
            Materia("02", "Arquitectura de Computadoras I", 4),
            Materia("03", "Álgebra y Geometría Analítica", 4),
            Materia("04", "Problemática del Mundo Actual", 3),
            Materia("05", "Inglés I", 1),
            Materia("06", "Arquitectura de Computadoras II", 4),
            Materia("07", "Cálculo Infinitesimal I", 4),
            Materia("08", "Historia de la Ciencia y de la Técnica", 3),
            Materia("09", "Inglés II", 1),
            Materia("10", "Programación y Estructuras de Datos", 4),
            Materia("11", "Programación Orientada a Objetos", 4),
            Materia("12", "Análisis y Diseño de Sistemas I", 4),
            Materia("13", "Cálculo Infinitesimal II", 4),
            Materia("14", "Inglés III", 1),
            Materia("15", "Sistemas Operativos", 4),
            Materia("16", "Análisis de Procesos Administrativos", 3),
            Materia("17", "Análisis y Diseño de Sistemas II", 4),
            Materia("18", "Física I", 4),
            Materia("19", "Probabilidad y Estadística", 4),
            Materia("20", "Inglés IV", 1),
            Materia("21", "Ingeniería de Requerimientos", 2),
            Materia("22", "Desarrollo y Arquitectura de Software", 4),
            Materia("23", "Análisis y Diseño de Sistemas III", 2),
            Materia("24", "Base de Datos", 4),
            Materia("25", "Tecnología de las Comunicaciones I", 4),
            Materia("26", "Física II", 3),
            Materia("27", "Ingeniería de Software", 4),
            Materia("28", "Experiencias Formativas Obligatorias (3er Año)", 4),
            Materia("29", "Tecnología de las Comunicaciones II", 4),
            Materia("30", "Matemática Discreta y Autómatas", 3),
            Materia("31", "Bases de Datos Aplicada", 4),
            Materia("32", "Trabajo de Diploma", 4),
            Materia("33", "Metodologías Ágiles", 4),
            Materia("34", "Organización y Gestión Empresaria", 3),
            Materia("35", "Bases de Datos Avanzada", 4),
            Materia("36", "Desarrollo y Arquitecturas WEB", 4),
            Materia("37", "Experiencias Formativas Obligatorias (4to Año)", 2),
            Materia("38", "Administración de Proyectos", 3),
            Materia("39", "Planificación Estratégica", 4),
            Materia("40", "Inteligencia Artificial", 3),
            Materia("41", "Metodologías y Desarrollos WEB", 4),
            Materia("42", "Electromagnetismo I", 3),
            Materia("43", "Sistema de Hardware", 4),
            Materia("44", "Modelización Numérica", 4),
            Materia("45", "Electromagnetismo II", 3),
            Materia("46", "Práctica Profesional Supervisada", 5),
            Materia("47", "Seminario de Trabajo Final de Ingeniería", 4),
            Materia("48", "Auditoría de Sistemas", 3),
            Materia("49", "Redes y Teleprocesamiento", 4),
            Materia("50", "Seguridad Informática", 3),
            Materia("51", "Trabajo Final de Ingeniería", 4),
        };

        private static readonly KeyValuePair<string, string>[] PlanCorrelativas =
        {
            new KeyValuePair<string, string>("06", "02"),
            new KeyValuePair<string, string>("07", "03"),
            new KeyValuePair<string, string>("09", "05"),
            new KeyValuePair<string, string>("10", "01"),
            new KeyValuePair<string, string>("11", "10"),
            new KeyValuePair<string, string>("13", "07"),
            new KeyValuePair<string, string>("14", "09"),
            new KeyValuePair<string, string>("15", "06"),
            new KeyValuePair<string, string>("17", "12"),
            new KeyValuePair<string, string>("18", "07"),
            new KeyValuePair<string, string>("20", "14"),
            new KeyValuePair<string, string>("21", "12"),
            new KeyValuePair<string, string>("22", "11"),
            new KeyValuePair<string, string>("23", "17"),
            new KeyValuePair<string, string>("24", "12"),
            new KeyValuePair<string, string>("25", "16"),
            new KeyValuePair<string, string>("26", "18"),
            new KeyValuePair<string, string>("27", "17"),
            new KeyValuePair<string, string>("27", "21"),
            new KeyValuePair<string, string>("27", "22"),
            new KeyValuePair<string, string>("29", "25"),
            new KeyValuePair<string, string>("31", "24"),
            new KeyValuePair<string, string>("32", "23"),
            new KeyValuePair<string, string>("32", "24"),
            new KeyValuePair<string, string>("32", "27"),
            new KeyValuePair<string, string>("33", "17"),
            new KeyValuePair<string, string>("35", "24"),
            new KeyValuePair<string, string>("36", "22"),
            new KeyValuePair<string, string>("39", "34"),
            new KeyValuePair<string, string>("40", "22"),
            new KeyValuePair<string, string>("41", "36"),
            new KeyValuePair<string, string>("42", "26"),
            new KeyValuePair<string, string>("43", "18"),
            new KeyValuePair<string, string>("44", "19"),
            new KeyValuePair<string, string>("45", "42"),
            new KeyValuePair<string, string>("46", "32"),
            new KeyValuePair<string, string>("47", "38"),
            new KeyValuePair<string, string>("47", "39"),
            new KeyValuePair<string, string>("47", "41"),
            new KeyValuePair<string, string>("48", "16"),
            new KeyValuePair<string, string>("49", "29"),
            new KeyValuePair<string, string>("50", "43"),
            new KeyValuePair<string, string>("51", "47"),
        };

        public static List<BE.MATERIA> MateriasDelPlan() => PlanMaterias.Select(m => Materia(m.Codigo, m.Nombre, m.Peso)).ToList();

        public static List<KeyValuePair<string, string>> CorrelativasDelPlan() => PlanCorrelativas.ToList();

        private readonly DAL.PerfilDAL _perfilDal = new DAL.PerfilDAL();
        private readonly DAL.MateriaDAL _materiaDal = new DAL.MateriaDAL();
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
            bool materiaOk = _integridad.VerificarMateria().EsValido;
            bool correlativaOk = _integridad.VerificarCorrelativa().EsValido;

            bool marcador = _siembraDal.ExisteMarcadorSiembra();
            bool crearPerfiles = !marcador && rolesExistentes.Count == 0;
            bool crearUsuarios = crearPerfiles && usuariosExistentes.Count == 0;
            bool crearMaterias = !marcador && _materiaDal.ListarTodasConInactivas().Count == 0;

            var plan = new DAL.PlanSiembra { IncluirMigracionDePerfiles = crearPerfiles };
            if (bitacoraOk) plan.TablasIntegras.Add("BITACORA");
            if (materiaOk) plan.TablasIntegras.Add("MATERIA");
            if (correlativaOk) plan.TablasIntegras.Add("CORRELATIVA");
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

            if (crearMaterias)
            {
                plan.Materias.AddRange(MateriasDelPlan());
                plan.Correlativas.AddRange(CorrelativasDelPlan());
            }

            plan.EscribirMarcador = !marcador
                || plan.Permisos.Count + plan.Roles.Count + plan.RolPermisos.Count + plan.Usuarios.Count + plan.Materias.Count > 0;

            DAL.ResultadoSiembra r = _siembraDal.Aplicar(plan);

            lock (_lockPendientes)
            {
                if (r.Permisos > 0 && permisoOk) _tablasPendientes.Add("PERMISO");
                if (r.Roles > 0 && rolOk) _tablasPendientes.Add("ROL");
                if (r.RolPermisos > 0 && rolPermisoOk) _tablasPendientes.Add("ROL_PERMISO");
                if (r.Usuarios > 0 && usuarioOk) _tablasPendientes.Add("USUARIO");
                if (r.UsuarioPerfiles > 0 && usuarioPerfilOk) _tablasPendientes.Add("USUARIO_PERFIL");
                if (r.Marcador && bitacoraOk) _tablasPendientes.Add("BITACORA");
                if (r.Materias > 0 && materiaOk) _tablasPendientes.Add("MATERIA");
                if (r.Correlativas > 0 && correlativaOk) _tablasPendientes.Add("CORRELATIVA");
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
                    case "MATERIA": _integridad.RecalcularMateria(); break;
                    case "CORRELATIVA": _integridad.RecalcularCorrelativa(); break;
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
