using BE;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BLL
{
    public class IdiomaBLL
    {
        private readonly DAL.IdiomaDAL _dal = new DAL.IdiomaDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();

        private static int _versionTraducciones;

        /// <summary>
        /// Se incrementa cada vez que cambia alguna traducción o idioma. Las sesiones comparan su
        /// IdiomaManager.VersionAplicada con este valor y recargan el diccionario si quedó viejo
        /// (mismo mecanismo que PerfilBLL.VersionPermisos). Solo vale con sesiones InProc.
        /// </summary>
        public static int VersionTraducciones => Volatile.Read(ref _versionTraducciones);

        private static void Invalidar() => Interlocked.Increment(ref _versionTraducciones);

        public List<IDIOMA> ListarTodos() => _dal.ListarTodos();

        public List<IDIOMA> ListarHabilitados() => _dal.ListarHabilitados();

        public IDIOMA ObtenerPorId(int id) => _dal.ObtenerPorId(id);

        public IDIOMA ObtenerPredeterminado() => _dal.ObtenerPredeterminado();

        public bool EstaEnUso(int id) => _dal.EstaEnUso(id);

        public int? ObtenerIdiomaDeUsuario(int idUsuario) => _dal.ObtenerIdiomaDeUsuario(idUsuario);

        public void ActualizarIdiomaDeUsuario(int idUsuario, int idiomaId) =>
            _dal.ActualizarIdiomaDeUsuario(idUsuario, idiomaId);

        public IDIOMA Crear(string nombre, bool habilitado, string usuarioAccion)
        {
            nombre = (nombre ?? "").Trim();
            if (nombre.Length == 0 || nombre.Length > 50)
                throw new InvalidOperationException("El nombre del idioma es obligatorio y admite hasta 50 caracteres.");
            int id = _dal.Insertar(nombre, habilitado);
            _bitacora.RegistrarAccion(usuarioAccion, $"ALTA_IDIOMA:{nombre}");
            Invalidar();
            return new IDIOMA { Id = id, Nombre = nombre, Habilitado = habilitado };
        }

        public void Renombrar(int id, string nombre, string usuarioAccion)
        {
            nombre = (nombre ?? "").Trim();
            if (nombre.Length == 0 || nombre.Length > 50)
                throw new InvalidOperationException("El nombre del idioma es obligatorio y admite hasta 50 caracteres.");
            _dal.Renombrar(id, nombre);
            _bitacora.RegistrarAccion(usuarioAccion, $"RENOMBRAR_IDIOMA:{nombre}");
            Invalidar();
        }

        public void ActualizarEstado(int id, bool habilitado, string usuarioAccion)
        {
            IDIOMA idioma = _dal.ObtenerPorId(id);
            if (idioma == null) throw new InvalidOperationException("El idioma no existe.");
            if (!habilitado)
            {
                if (idioma.Predeterminado)
                    throw new InvalidOperationException(
                        "No se puede deshabilitar el idioma predeterminado del sistema.");
                if (_dal.EstaEnUso(id))
                    throw new InvalidOperationException(
                        "No se puede deshabilitar un idioma que está en uso por algún usuario.");
            }
            _dal.ActualizarEstado(id, habilitado);
            _bitacora.RegistrarAccion(usuarioAccion,
                $"{(habilitado ? "HABILITAR" : "DESHABILITAR")}_IDIOMA:{idioma.Nombre}");
            Invalidar();
        }

        public void Eliminar(int id, string usuarioAccion)
        {
            IDIOMA idioma = _dal.ObtenerPorId(id);
            if (idioma == null) throw new InvalidOperationException("El idioma no existe.");
            if (idioma.Predeterminado)
                throw new InvalidOperationException(
                    "No se puede eliminar el idioma predeterminado del sistema.");
            if (_dal.EstaEnUso(id))
                throw new InvalidOperationException(
                    "No se puede eliminar un idioma que está en uso por algún usuario.");
            _dal.Eliminar(id);
            _bitacora.RegistrarAccion(usuarioAccion, $"BAJA_IDIOMA:{idioma.Nombre}");
            Invalidar();
        }

        public void RegistrarControl(string clave, string textoDefault) =>
            _dal.RegistrarControl(clave, textoDefault);

        public Dictionary<string, string> CargarTraducciones(int idiomaId) =>
            _dal.CargarTraducciones(idiomaId);

        public List<CONTROL_IDIOMA> ListarControlesConTraduccion(int idiomaId) =>
            _dal.ListarControlesConTraduccion(idiomaId);

        /// <summary>Guarda varias traducciones de un idioma (id de control → texto) y deja una sola entrada en la bitácora.</summary>
        public int GuardarTraducciones(int idiomaId, Dictionary<int, string> textos, string usuarioAccion)
        {
            IDIOMA idioma = _dal.ObtenerPorId(idiomaId);
            if (idioma == null) throw new InvalidOperationException("El idioma no existe.");
            int guardadas = 0;
            foreach (var par in textos)
            {
                string texto = (par.Value ?? "").Trim();
                if (texto.Length == 0) continue;
                if (texto.Length > 200)
                    throw new InvalidOperationException("Una traducción supera los 200 caracteres.");
                _dal.GuardarTraduccion(idiomaId, par.Key, texto);
                guardadas++;
            }
            if (guardadas > 0)
            {
                _bitacora.RegistrarAccion(usuarioAccion, $"EDICION_TRADUCCIONES:{idioma.Nombre} ({guardadas})");
                Invalidar();
            }
            return guardadas;
        }
    }
}
