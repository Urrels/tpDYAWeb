using System.Collections.Generic;

namespace BE
{
    public class USUARIO
    {
        public int Id { get; set; }
        public string Usuario { get; set; }
        public string Contrasena { get; set; }
        public string DireccionEncriptada { get; set; }
        public string DireccionVisible { get; set; }
        public int IntentosFallidos { get; set; }

        // Nombres de los permisos efectivos, calculados una vez al iniciar
        // sesión a partir de los perfiles asignados (ver BLL.PerfilBLL).
        public List<string> Permisos { get; set; } = new List<string>();

        public bool TienePermiso(string nombrePermiso)
        {
            return Permisos != null && Permisos.Contains(nombrePermiso);
        }
    }
}
