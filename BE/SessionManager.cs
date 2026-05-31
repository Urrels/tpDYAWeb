namespace BE
{
    public class SessionManager
    {
        private static SessionManager _instance = null;
        private USUARIO _usuario;

        private SessionManager() { }

        public static SessionManager getInstane()
        {
            if (_instance == null)
                _instance = new SessionManager();
            return _instance;
        }

        public USUARIO getUsuario() { return _usuario; }
        public int setUsuario(USUARIO u) { _usuario = u; return 1; }
        public void cerrarSesion() { _usuario = null; _instance = null; }
    }
}
