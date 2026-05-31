namespace BE
{
    public class USUARIO
    {
        public int Id { get; set; }
        public string Usuario { get; set; }
        public string Contrasena { get; set; }
        public string DireccionEncriptada { get; set; }
        public string DireccionVisible { get; set; }
        public string Rol { get; set; }

        public bool EsAdmin => Rol == "Admin";
    }
}
