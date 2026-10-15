using System;

namespace BLL
{
    public static class Paginacion
    {
        public static BE.PaginaResultado<T> ConsultarAjustando<T>(int pagina, Func<int, BE.PaginaResultado<T>> consulta)
        {
            if (pagina < 1) pagina = 1;
            BE.PaginaResultado<T> r = consulta(pagina);
            if (r.Filas.Count > 0 || pagina == 1) return r;

            BE.PaginaResultado<T> primera = consulta(1);
            if (primera.TotalPaginas == 1) return primera;
            return consulta(primera.TotalPaginas);
        }
    }
}
