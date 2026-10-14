using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    /// <summary>
    /// ConcreteObserver del multiidioma (equivalente a FormBase en la versión de escritorio).
    /// Al iniciar recorre los controles de la página, su master y sus controles de usuario, guarda
    /// el texto original de cada uno y se registra en el IdiomaManager de la sesión. Cuando cambia
    /// el idioma (o en cada pedido, antes de dibujar) vuelve a poner el texto traducido, o el
    /// original si la clave no tiene traducción.
    ///
    /// Se traducen los controles con ID de tipo Literal y Button. Para un texto fijo del markup
    /// se usa &lt;asp:Literal ID="..." runat="server" Text="..."/&gt;; los Label quedan fuera
    /// porque guardan valores que arma el código. La clave es "Pagina.IdDelControl".
    /// </summary>
    public class PaginaConIdioma : Page, BE.IObservadorIdioma
    {
        private readonly Dictionary<string, Control> _controles = new Dictionary<string, Control>();
        private readonly Dictionary<string, string> _defaults = new Dictionary<string, string>();
        private string _claveTitulo;
        private string _tituloDefault;
        private bool _registrado;

        protected override void OnInit(EventArgs e)
        {
            IdiomaWeb.Fachada.AplicarPredeterminadoSiHaceFalta();

            GuardarDefaults(Controls);
            if (!string.IsNullOrWhiteSpace(Title))
            {
                _claveTitulo = GetType().BaseType.Name;
                _tituloDefault = Title;
                Textos.Registrar(_claveTitulo, _tituloDefault);
            }

            IdiomaWeb.Manager.Registrar(this);
            _registrado = true;
            base.OnInit(e);
        }

        protected override void OnPreRender(EventArgs e)
        {
            IdiomaWeb.Fachada.RefrescarSiHaceFalta();
            ActualizarIdioma();
            base.OnPreRender(e);
        }

        protected override void OnUnload(EventArgs e)
        {
            // El manager vive toda la sesión: la página debe salir de su lista al terminar el pedido.
            if (_registrado) IdiomaWeb.Manager.Desregistrar(this);
            base.OnUnload(e);
        }

        /// <summary>Hook para textos que no son controles con ID (encabezados de grillas, etc.).</summary>
        protected virtual void ActualizarTextosDinamicos() { }

        public void ActualizarIdioma()
        {
            BE.IdiomaManager idiomas = IdiomaWeb.Manager;
            foreach (var kvp in _controles)
                Escribir(kvp.Value, idiomas.Traducir(kvp.Key) ?? _defaults[kvp.Key]);
            if (_claveTitulo != null)
                Title = idiomas.Traducir(_claveTitulo) ?? _tituloDefault;
            ActualizarTextosDinamicos();
        }

        private void GuardarDefaults(ControlCollection controles)
        {
            foreach (Control c in controles)
            {
                string texto = Leer(c);
                if (!string.IsNullOrEmpty(c.ID) && !string.IsNullOrWhiteSpace(texto))
                {
                    string clave = Prefijo(c) + "." + c.ID;
                    _controles[clave] = c;
                    _defaults[clave] = texto;
                    Textos.Registrar(clave, texto);
                }
                if (c.HasControls()) GuardarDefaults(c.Controls);
            }
        }

        // Página, master o control de usuario donde se declaró el control.
        private static string Prefijo(Control c) => (c.TemplateControl ?? c.Page).GetType().BaseType.Name;

        private static string Leer(Control c)
        {
            if (c is Literal lit) return lit.Text;
            if (c is Button btn) return btn.Text;
            return null;
        }

        private static void Escribir(Control c, string texto)
        {
            if (c is Literal lit)
            {
                // El texto viene de la base de datos: se codifica para que no pueda inyectar HTML.
                lit.Mode = LiteralMode.Encode;
                lit.Text = texto;
            }
            else if (c is Button btn) btn.Text = texto;
        }
    }
}
