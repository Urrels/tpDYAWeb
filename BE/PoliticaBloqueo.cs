namespace BE
{
    public static class PoliticaBloqueo
    {
        public const int MaximoIntentos = 3;

        public static void Siguiente(int intentosActuales, bool bloqueado, out int intentos, out bool quedaBloqueado)
        {
            intentos = intentosActuales + 1;
            quedaBloqueado = bloqueado || intentos >= MaximoIntentos;
        }
    }
}
