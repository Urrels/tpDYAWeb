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

        public bool Activo { get; set; } = true;
        public bool Bloqueado { get; set; }

        public string PerfilesTexto { get; set; }

        public List<string> Permisos { get; set; } = new List<string>();

        public int VersionPermisos { get; set; }

        public long EpocaSesion { get; set; }

        public bool TienePermiso(string nombrePermiso)
        {
            return Permisos != null && Permisos.Contains(nombrePermiso);
        }
    }
}
