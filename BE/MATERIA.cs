using System.Collections.Generic;

namespace BE
{
    public class MATERIA
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Codigo { get; set; }
        public string Modalidad { get; set; } // Presencial / Virtual
        public int Peso { get; set; }         // 1-5, para mapa de calor RF07
        public bool Activa { get; set; }
        public List<MATERIA> Correlativas { get; set; } = new List<MATERIA>();
    }
}
