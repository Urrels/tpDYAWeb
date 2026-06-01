using System;
using System.Collections.Generic;

namespace BE
{
    public class INSCRIPCION
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public int IdPeriodo { get; set; }
        public DateTime FechaInscripcion { get; set; }
        public string EtiquetaPeriodo { get; set; }
        public List<MATERIA> Materias { get; set; } = new List<MATERIA>();
    }
}
