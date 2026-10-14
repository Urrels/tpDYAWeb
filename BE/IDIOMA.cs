namespace BE
{
    public class IDIOMA
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public bool Habilitado { get; set; }
        public bool Predeterminado { get; set; }

        public override string ToString() => Nombre;
    }
}
