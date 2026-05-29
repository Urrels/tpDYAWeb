using System;

namespace BE
{
    public class EVENTO_ACADEMICO
    {
        public int Id { get; set; }
        public int IdMateria { get; set; }
        public string NombreMateria { get; set; }
        public int IdUsuario { get; set; }
        public string Tipo { get; set; }        // Parcial / Final / TP
        public string Descripcion { get; set; }
        public DateTime Fecha { get; set; }
        public int Peso { get; set; }           // 1-5, para mapa de calor RF07

        public string ColorCalor { get; set; }  // Verde / Amarillo / Rojo
    }
}
