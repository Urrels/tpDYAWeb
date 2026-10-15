using System.Collections.Generic;

namespace BE
{
    public class PaginaResultado<T>
    {
        public List<T> Filas { get; set; } = new List<T>();
        public int TotalFilas { get; set; }
        public int Pagina { get; set; } = 1;
        public int Tamanio { get; set; } = 1;

        public int TotalPaginas =>
            TotalFilas <= 0 || Tamanio <= 0 ? 1 : (TotalFilas + Tamanio - 1) / Tamanio;
    }
}
