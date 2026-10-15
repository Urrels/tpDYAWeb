namespace BE
{
    public static class Permisos
    {
        public const string GestionarMaterias = "Gestionar materias";
        public const string CargarNotas = "Cargar notas";
        public const string VerBitacora = "Ver bitacora";
        public const string DesbloquearUsuarios = "Desbloquear usuarios";
        public const string GestionarPeriodos = "Gestionar periodos e inscripciones";
        public const string VerificarIntegridad = "Verificar integridad";
        public const string IngresarConIntegridadInvalida = "Ingresar con integridad invalida";
        public const string GestionarPerfiles = "Gestionar perfiles";
        public const string GestionarUsuarios = "Gestionar usuarios";
        public const string VerMenuAlumno = "Ver menu de alumno";
        public const string Inscribirse = "Inscribirse a materias";
        public const string VerMisCursadas = "Ver mis cursadas";
        public const string VerDashboard = "Ver dashboard";
        public const string ExportarPdf = "Exportar PDF";
        public const string VerCalendario = "Ver calendario";
        public const string EditarPerfil = "Editar perfil";
        public const string CambiarContrasena = "Cambiar contrasena";
        public const string GestionarIdiomas = "Gestionar idiomas";

        public static readonly string[] Privilegiados =
        {
            GestionarPerfiles, GestionarUsuarios, VerificarIntegridad,
            IngresarConIntegridadInvalida, DesbloquearUsuarios
        };

        public static bool EsPrivilegiado(string permiso) => System.Array.IndexOf(Privilegiados, permiso) >= 0;

        public const string PerfilAdministrador = "Administrador";
        public const string PerfilWebmaster = "Webmaster";

        public const string PerfilAlumno = "Alumno";
    }
}
