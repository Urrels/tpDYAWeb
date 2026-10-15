using System.Collections.Generic;

namespace BE
{
    public sealed class SessionRegistry
    {
        private static volatile SessionRegistry _instancia;
        private static readonly object _lock = new object();

        private readonly Dictionary<string, string> _sesionesActivas = new Dictionary<string, string>();

        private SessionRegistry() { }

        public static SessionRegistry Instancia
        {
            get
            {
                if (_instancia == null)
                {
                    lock (_lock)
                    {
                        if (_instancia == null)
                            _instancia = new SessionRegistry();
                    }
                }
                return _instancia;
            }
        }

        public bool TieneSesionActiva(string usuario)
        {
            lock (_lock) { return _sesionesActivas.ContainsKey(usuario); }
        }

        public void RegistrarSesion(string usuario, string sessionId)
        {
            lock (_lock) { _sesionesActivas[usuario] = sessionId; }
        }

        public void CerrarSesion(string usuario, string sessionId)
        {
            lock (_lock)
            {
                if (_sesionesActivas.TryGetValue(usuario, out string registrada) && registrada == sessionId)
                    _sesionesActivas.Remove(usuario);
            }
        }

        public void ForzarCierre(string usuario)
        {
            lock (_lock) { _sesionesActivas.Remove(usuario); }
        }
    }
}
