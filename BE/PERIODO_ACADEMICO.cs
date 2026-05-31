using System;

namespace BE
{
    public class PERIODO_ACADEMICO
    {
        public int Id { get; set; }
        public int Anio { get; set; }
        public int Cuatrimestre { get; set; }
        public string Descripcion { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Etiqueta => $"{Anio} — {Cuatrimestre}° Cuatrimestre";
    }
}
