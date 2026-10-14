namespace BE
{
    /// <summary>
    /// Nombres de los permisos del sistema. Son la clave que une el código
    /// con la tabla PERMISO, por eso se centralizan acá para evitar errores
    /// de tipeo. Los nombres no llevan tildes a propósito: se comparan como
    /// texto y el script SQL puede abrirse con otra codificación.
    /// </summary>
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

        /// <summary>
        /// Permisos de seguridad y administración. Quien tiene "Gestionar perfiles" puede
        /// manejar libremente los demás permisos, pero estos solo puede otorgarlos, quitarlos
        /// o tocar a quienes los tienen si los posee él mismo (ver PerfilBLL). Sin este límite
        /// un administrador podría escalar hasta los permisos exclusivos del Webmaster.
        /// </summary>
        public static readonly string[] Privilegiados =
        {
            GestionarPerfiles, GestionarUsuarios, VerificarIntegridad,
            IngresarConIntegridadInvalida, DesbloquearUsuarios
        };

        public static bool EsPrivilegiado(string permiso) => System.Array.IndexOf(Privilegiados, permiso) >= 0;

        // Perfiles base (no son permisos: el catálogo de permisos excluye los que empiezan con "Perfil").
        public const string PerfilAdministrador = "Administrador";
        public const string PerfilWebmaster = "Webmaster";

        /// <summary>Nombre del perfil de alumno (identidad de dominio, no permiso).</summary>
        public const string PerfilAlumno = "Alumno";
    }
}
