using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// Singleton thread-safe (double-checked locking) que registra qué
    /// usuarios tienen una sesión web activa en este momento, para
    /// garantizar sesión única por usuario. No reemplaza a Session["Usuario"]
    /// de ASP.NET (que sigue siendo el estado real por usuario/request) — es
    /// solo un registro compartido entre todos los usuarios concurrentes de
    /// la app para poder rechazar un segundo login del mismo usuario
    /// mientras el primero sigue activo.
    /// </summary>
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

        public void CerrarSesion(string usuario)
        {
            lock (_lock) { _sesionesActivas.Remove(usuario); }
        }
    }
}
