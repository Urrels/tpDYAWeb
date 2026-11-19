namespace BE
{
    public static class Normalizador
    {
        public static string Usuario(string usuario) =>
            (usuario ?? string.Empty).Trim().ToLowerInvariant();

        public static string CodigoMateria(string codigo) =>
            (codigo ?? string.Empty).Trim().ToUpperInvariant();
    }
}
