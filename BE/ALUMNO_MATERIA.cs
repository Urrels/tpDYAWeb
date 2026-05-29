using System;

namespace BE
{
    public class ALUMNO_MATERIA
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public int IdMateria { get; set; }
        public string NombreMateria { get; set; }
        public string CodigoMateria { get; set; }
        public string Modalidad { get; set; }
        public int PesoMateria { get; set; }
        public string Estado { get; set; }

        // Notas (0 a 10)
        public decimal? NotaParcial1 { get; set; }
        public decimal? NotaParcial2 { get; set; }
        public decimal? NotaRecuperatorio { get; set; }
        public decimal? NotaFinal { get; set; }

        // Fechas de mesa
        public DateTime? FechaFinal { get; set; }
        public DateTime? FechaRecuperatorio { get; set; }

        // RF10
        public int DVH { get; set; }

        // Calculado - no persistido
        public bool AproboAmbosPariales =>
            (NotaParcial1 ?? 0) >= 4 && (NotaParcial2 ?? 0) >= 4;

        public bool NecesitaRecuperatorio =>
            (NotaParcial1 ?? 0) < 4 || (NotaParcial2 ?? 0) < 4;

        public bool TieneFinal =>
            AproboAmbosPariales && FechaFinal.HasValue;

        public bool TieneRecuperatorio =>
            NecesitaRecuperatorio && FechaRecuperatorio.HasValue;
        // Predictor de riesgo - calculado, no persistido
        public string NivelRiesgo { get; set; }     // "Alto", "Medio", "Bajo"
        public string MensajeRiesgo { get; set; }   // explicación para el alumno
    }
}