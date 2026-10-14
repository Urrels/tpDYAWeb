namespace BE
{
    /// <summary>Observer: lo implementa todo lo que muestra textos y debe refrescarse al cambiar de idioma.</summary>
    public interface IObservadorIdioma
    {
        void ActualizarIdioma();
    }
}
