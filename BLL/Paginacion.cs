using System;

namespace BLL
{
    public static class Paginacion
    {
        // Ejecuta la consulta paginada; si la página pedida quedó más allá de la última
        // (se borraron filas, o se cambió el filtro), vuelve a consultar la última página real.
        public static BE.PaginaResultado<T> ConsultarAjustando<T>(int pagina, Func<int, BE.PaginaResultado<T>> consulta)
        {
            if (pagina < 1) pagina = 1;
            BE.PaginaResultado<T> r = consulta(pagina);
            if (r.Filas.Count > 0 || pagina == 1) return r;

            // Página vacía: la primera trae el total y permite calcular la última.
            BE.PaginaResultado<T> primera = consulta(1);
            if (primera.TotalPaginas == 1) return primera;
            return consulta(primera.TotalPaginas);
        }
    }
}
