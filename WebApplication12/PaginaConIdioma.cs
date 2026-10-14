using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace CAPAS_Web
{
    /// <summary>
    /// ConcreteObserver del multiidioma (equivale a FormBase en la versión de escritorio).
    /// Al iniciar recorre los controles de la página, su master y sus controles de usuario, guarda
    /// el texto original de cada uno ("slot") y se registra en el IdiomaManager de la sesión.
    /// Cuando cambia el idioma (y en cada pedido, antes de dibujar) vuelve a poner el texto
    /// traducido, o el original si la clave no tiene traducción.
    ///
    /// Qué se traduce (siempre en controles con ID; la clave es "Pagina.IdDelControl"):
    ///  - Literal, Button, LinkButton (se conserva un ícono &lt;i&gt; inicial) y CheckBox/RadioButton: su Text.
    ///  - El mensaje de confirm('...') de OnClientClick.
    ///  - El atributo placeholder de los TextBox.
    ///  - Los ítems fijos de los DropDownList / ListBox / CheckBoxList / RadioButtonList (por su Value).
    ///  - Los encabezados y el EmptyDataText de los GridView.
    ///  - El título de la página.
    /// Los controles dentro de GridView y Repeater se leen cuando se crean cada fila o ítem
    /// (RowCreated / ItemCreated), que es cuando todavía tienen el texto del markup.
    /// Los Label quedan fuera a propósito: guardan valores que arma el código.
    /// </summary>
    public class PaginaConIdioma : Page, BE.IObservadorIdioma
    {
        private sealed class Slot
        {
            public string Clave;
            public string Original;
            public Action<string> Escribir;
        }

        private static readonly Regex Icono = new Regex(@"^(\s*<i\b[^>]*>\s*</i>\s*)(.*)$", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex Confirmacion = new Regex(@"confirm\('((?:[^'\\]|\\.)*)'\)", RegexOptions.Compiled);

        private readonly List<Slot> _slots = new List<Slot>();
        private readonly HashSet<Control> _vistos = new HashSet<Control>();
        private bool _registrado;

        protected override void OnInit(EventArgs e)
        {
            IdiomaWeb.Fachada.AplicarPredeterminadoSiHaceFalta();

            Recolectar(Controls);
            // Page.Title lanza una excepción si el <head> no es runat="server" (como en Login.aspx).
            if (Header != null && !string.IsNullOrWhiteSpace(Title))
                Agregar(GetType().BaseType.Name, Title, t => Title = t);

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

        /// <summary>Hook para textos que no son controles con ID (los arma el código).</summary>
        protected virtual void ActualizarTextosDinamicos() { }

        public void ActualizarIdioma()
        {
            BE.IdiomaManager idiomas = IdiomaWeb.Manager;
            foreach (Slot s in _slots)
                s.Escribir(idiomas.Traducir(s.Clave) ?? s.Original);
            ActualizarTextosDinamicos();
        }

        // ---------- Recolección ----------

        private void Recolectar(ControlCollection controles)
        {
            foreach (Control c in controles)
            {
                Examinar(c);
                if (c.HasControls()) Recolectar(c.Controls);
            }
        }

        private void Examinar(Control c)
        {
            if (!_vistos.Add(c)) return;

            if (c is GridView gv)
            {
                gv.RowCreated += (s, a) => RecolectarPlantilla(a.Row);
                AgregarEncabezados(gv);
                if (!string.IsNullOrWhiteSpace(gv.EmptyDataText) && !string.IsNullOrEmpty(gv.ID))
                    {
                    // El texto de "sin datos" lo lee el GridView al enlazarse (antes del PreRender): se escribe ahora.
                    string claveVacio = Clave(gv) + ".vacio";
                    string originalVacio = gv.EmptyDataText;
                    Textos.Registrar(claveVacio, originalVacio);
                    gv.EmptyDataText = HttpUtility.HtmlEncode(IdiomaWeb.Manager.Traducir(claveVacio) ?? originalVacio);
                }
            }
            else if (c is Repeater rpt)
            {
                rpt.ItemCreated += (s, a) => RecolectarPlantilla(a.Item);
            }
            else if (c is DataList dl)
            {
                dl.ItemCreated += (s, a) => RecolectarPlantilla(a.Item);
            }

            if (string.IsNullOrEmpty(c.ID)) return;
            string clave = Clave(c);

            if (c is Literal lit && !string.IsNullOrWhiteSpace(lit.Text))
                Agregar(clave, lit.Text, t => { lit.Mode = LiteralMode.Encode; lit.Text = t; });
            else if (c is Button btn)
            {
                // data-i18n="manual": el código le pone el texto (no se pisa con el del markup).
                if (!string.IsNullOrWhiteSpace(btn.Text) && btn.Attributes["data-i18n"] != "manual") Agregar(clave, btn.Text, t => btn.Text = t);
                AgregarConfirmacion(clave, btn);
            }
            else if (c is LinkButton lnk)
            {
                string texto = lnk.Text ?? "";
                Match m = Icono.Match(texto);
                string icono = m.Success ? m.Groups[1].Value : "";
                string resto = (m.Success ? m.Groups[2].Value : texto).Trim();
                if (resto.Length > 0 && Regex.IsMatch(resto, @"\p{L}") && !resto.Contains("<"))
                    Agregar(clave, resto, t => lnk.Text = icono + HttpUtility.HtmlEncode(t));
                AgregarConfirmacion(clave, lnk);
            }
            else if (c is CheckBox chk && !string.IsNullOrWhiteSpace(chk.Text))
                Agregar(clave, chk.Text, t => chk.Text = HttpUtility.HtmlEncode(t));
            else if (c is ListControl lista)
                AgregarItems(clave, lista);

            if (c is TextBox txt && !string.IsNullOrWhiteSpace(txt.Attributes["placeholder"]))
                Agregar(clave + ".placeholder", txt.Attributes["placeholder"], t => txt.Attributes["placeholder"] = t);
        }

        // Controles creados por una fila o un ítem de plantilla: acá todavía tienen el texto del markup.
        private void RecolectarPlantilla(Control raiz)
        {
            Examinar(raiz);
            if (raiz.HasControls()) Recolectar(raiz.Controls);
        }

        private void AgregarConfirmacion(string clave, WebControl control)
        {
            string js = control is Button b ? b.OnClientClick : ((LinkButton)control).OnClientClick;
            Match m = Confirmacion.Match(js ?? "");
            if (!m.Success) return;
            string original = Regex.Unescape(m.Groups[1].Value);
            Agregar(clave + ".confirmar", original, t =>
            {
                string nuevo = Confirmacion.Replace(js, "confirm('" + HttpUtility.JavaScriptStringEncode(t) + "')", 1);
                if (control is Button bb) bb.OnClientClick = nuevo; else ((LinkButton)control).OnClientClick = nuevo;
            });
        }

        private void AgregarItems(string clave, ListControl lista)
        {
            // Solo los ítems fijos del markup (los que cargó el código tienen datos, no texto de interfaz).
            foreach (ListItem item in lista.Items.Cast<ListItem>().ToList())
            {
                if (string.IsNullOrWhiteSpace(item.Text) || !Regex.IsMatch(item.Text, @"\p{L}")) continue;
                string valor = item.Value;
                Agregar(clave + ".item." + (valor.Length > 0 ? valor : "vacio"), item.Text, t =>
                {
                    ListItem it = lista.Items.FindByValue(valor);
                    if (it != null) it.Text = t;
                });
            }
        }

        private void AgregarEncabezados(GridView gv)
        {
            if (string.IsNullOrEmpty(gv.ID)) return;
            for (int i = 0; i < gv.Columns.Count; i++)
            {
                string original = gv.Columns[i].HeaderText;
                if (string.IsNullOrWhiteSpace(original) || !Regex.IsMatch(original, @"\p{L}")) continue;
                int indice = i;
                string clave = Clave(gv) + ".enc." + Compacto(original);
                Agregar(clave, original, t =>
                {
                    if (gv.HeaderRow != null && indice < gv.HeaderRow.Cells.Count)
                        gv.HeaderRow.Cells[indice].Text = HttpUtility.HtmlEncode(t);
                });
            }
        }

        private void Agregar(string clave, string original, Action<string> escribir)
        {
            Textos.Registrar(clave, original);
            _slots.Add(new Slot { Clave = clave, Original = original, Escribir = escribir });
        }

        // Página, master o control de usuario donde se declaró el control + su ID.
        private static string Clave(Control c) =>
            (c.TemplateControl ?? c.Page).GetType().BaseType.Name + "." + c.ID;

        // Parte del texto de un encabezado que se usa dentro de la clave.
        private static string Compacto(string texto)
        {
            string sin = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char ch in sin)
                if (char.IsLetterOrDigit(ch) && ch < 128) sb.Append(ch);
            string s = sb.ToString();
            return s.Length == 0 ? "col" : s.Substring(0, Math.Min(s.Length, 24));
        }
    }
}
