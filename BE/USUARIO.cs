namespace BE
{
    public class USUARIO
    {
        public int Id { get; set; }
        public string Usuario { get; set; }
        public string Contrasena { get; set; }
        public string DireccionEncriptada { get; set; } // AES - RF11
        public string DireccionVisible { get; set; }    // desencriptada en memoria
    }
}