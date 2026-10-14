using BE;
using System.Collections.Generic;

namespace BLL
{
    /// <summary>
    /// Facade del multiidioma: la interfaz web solo habla con esta clase. Trabaja sobre el
    /// IdiomaManager de la sesión (en la versión de escritorio era un Singleton).
    /// </summary>
    public class FachadaIdioma
    {
        private readonly IdiomaBLL _idiomas = new IdiomaBLL();
        private readonly IdiomaManager _manager;

        public FachadaIdioma(IdiomaManager manager)
        {
            _manager = manager;
        }

        public IDIOMA IdiomaActivo => _manager.IdiomaActivo;

        public List<IDIOMA> ListarDisponibles() => _idiomas.ListarHabilitados();

        /// <summary>Cambia el idioma de la sesión y, si hay un usuario con sesión iniciada, guarda su preferencia.</summary>
        public void Cambiar(IDIOMA idioma, int? idUsuario)
        {
            Aplicar(idioma);
            if (idUsuario.HasValue)
                _idiomas.ActualizarIdiomaDeUsuario(idUsuario.Value, idioma.Id);
        }

        /// <summary>Al iniciar sesión: aplica el idioma guardado del usuario si todavía está habilitado.</summary>
        public void AplicarPreferencia(int idUsuario)
        {
            int? idiomaId = _idiomas.ObtenerIdiomaDeUsuario(idUsuario);
            if (!idiomaId.HasValue) return;
            IDIOMA idioma = _idiomas.ObtenerPorId(idiomaId.Value);
            if (idioma != null && idioma.Habilitado)
                Aplicar(idioma);
        }

        /// <summary>Si la sesión todavía no tiene idioma (primer pedido), usa el predeterminado del sistema.</summary>
        public void AplicarPredeterminadoSiHaceFalta()
        {
            if (_manager.IdiomaActivo != null) return;
            IDIOMA idioma = _idiomas.ObtenerPredeterminado();
            if (idioma != null) Aplicar(idioma);
        }

        /// <summary>
        /// Recarga el diccionario si alguien editó traducciones o idiomas desde que se cargó.
        /// Si el idioma activo se deshabilitó o se borró, vuelve al predeterminado.
        /// </summary>
        public void RefrescarSiHaceFalta()
        {
            if (_manager.IdiomaActivo == null || _manager.VersionAplicada == IdiomaBLL.VersionTraducciones) return;
            IDIOMA vigente = _idiomas.ObtenerPorId(_manager.IdiomaActivo.Id);
            if (vigente != null && vigente.Habilitado) Aplicar(vigente);
            else
            {
                IDIOMA predeterminado = _idiomas.ObtenerPredeterminado();
                if (predeterminado != null) Aplicar(predeterminado);
            }
        }

        public void RegistrarClave(string clave, string textoDefault) =>
            _idiomas.RegistrarControl(clave, textoDefault);

        public string Traducir(string clave) => _manager.Traducir(clave);

        private void Aplicar(IDIOMA idioma)
        {
            int version = IdiomaBLL.VersionTraducciones;
            Dictionary<string, string> traducciones = _idiomas.CargarTraducciones(idioma.Id);
            _manager.VersionAplicada = version;
            _manager.CambiarIdioma(idioma, traducciones);
        }
    }
}
