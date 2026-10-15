using System.Collections.Generic;

namespace BE
{
    public sealed class IdiomaManager
    {
        private readonly List<IObservadorIdioma> _observers = new List<IObservadorIdioma>();
        private Dictionary<string, string> _traducciones = new Dictionary<string, string>();
        private IDIOMA _idiomaActivo;

        public IDIOMA IdiomaActivo => _idiomaActivo;

        public int VersionAplicada { get; set; }

        public void Registrar(IObservadorIdioma obs)
        {
            if (!_observers.Contains(obs))
                _observers.Add(obs);
        }

        public void Desregistrar(IObservadorIdioma obs)
        {
            _observers.Remove(obs);
        }

        public void CambiarIdioma(IDIOMA idioma, Dictionary<string, string> traducciones)
        {
            _idiomaActivo = idioma;
            _traducciones = traducciones ?? new Dictionary<string, string>();
            Notificar();
        }

        private void Notificar()
        {
            foreach (var obs in new List<IObservadorIdioma>(_observers))
                obs.ActualizarIdioma();
        }

        public string Traducir(string clave)
        {
            return _traducciones.TryGetValue(clave, out string texto) ? texto : null;
        }
    }
}
