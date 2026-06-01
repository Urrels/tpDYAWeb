using System;

namespace BE
{
    public class EVENTO_ACADEMICO
    {
        public int Id { get; set; }
        public int IdMateria { get; set; }
        public string NombreMateria { get; set; }
        public int IdUsuario { get; set; }
        public string Tipo { get; set; }
        public string Descripcion { get; set; }
        public DateTime Fecha { get; set; }
        public int Peso { get; set; }
        public string ColorCalor { get; set; }
    }
}
