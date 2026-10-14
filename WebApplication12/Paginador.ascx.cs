using System;

namespace CAPAS_Web
{
    // Paginador reutilizable. La página contenedora consulta con PaginaActual / TamanioPagina,
    // llama a Establecer() con el resultado y vuelve a enlazar su grilla en CambioPagina.
    public partial class Paginador : System.Web.UI.UserControl
    {
        public event EventHandler CambioPagina;

        public int PaginaActual
        {
            get => ViewState["Pagina"] != null ? (int)ViewState["Pagina"] : 1;
            set => ViewState["Pagina"] = Math.Max(1, value);
        }

        public int TotalFilas
        {
            get => ViewState["TotalFilas"] != null ? (int)ViewState["TotalFilas"] : 0;
            set => ViewState["TotalFilas"] = Math.Max(0, value);
        }

        public int TamanioPagina
        {
            get => ViewState["Tamanio"] != null ? (int)ViewState["Tamanio"] : 10;
            set => ViewState["Tamanio"] = Math.Max(1, value);
        }

        public int TotalPaginas => TotalFilas == 0 ? 1 : (TotalFilas + TamanioPagina - 1) / TamanioPagina;

        // Toma la página real devuelta por la consulta (ya ajustada a la última si hacía falta).
        public void Establecer<T>(BE.PaginaResultado<T> resultado)
        {
            TamanioPagina = resultado.Tamanio;
            TotalFilas = resultado.TotalFilas;
            PaginaActual = Math.Min(resultado.Pagina, TotalPaginas);
            Dibujar();
        }

        private void Dibujar()
        {
            bool hayFilas = TotalFilas > 0;
            lblPagina.Text = hayFilas
                ? Textos.TH("Paginador.pagina", "Página {0} de {1} ({2} registros)", PaginaActual, TotalPaginas, TotalFilas)
                : Textos.TH("Paginador.sinRegistros", "Sin registros");
            btnPrimera.Enabled = btnAnterior.Enabled = hayFilas && PaginaActual > 1;
            btnSiguiente.Enabled = btnUltima.Enabled = hayFilas && PaginaActual < TotalPaginas;
        }

        private void IrA(int pagina)
        {
            // Nunca más allá de la última página conocida: evita una grilla vacía.
            PaginaActual = Math.Min(Math.Max(1, pagina), TotalPaginas);
            CambioPagina?.Invoke(this, EventArgs.Empty);
        }

        protected void btnPrimera_Click(object sender, EventArgs e) => IrA(1);
        protected void btnAnterior_Click(object sender, EventArgs e) => IrA(PaginaActual - 1);
        protected void btnSiguiente_Click(object sender, EventArgs e) => IrA(PaginaActual + 1);
        protected void btnUltima_Click(object sender, EventArgs e) => IrA(TotalPaginas);
    }
}
