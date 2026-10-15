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
            if (_registrado) IdiomaWeb.Manager.Desregistrar(this);
            base.OnUnload(e);
        }

        protected virtual void ActualizarTextosDinamicos() { }

        public void ActualizarIdioma()
        {
            BE.IdiomaManager idiomas = IdiomaWeb.Manager;
            foreach (Slot s in _slots)
                s.Escribir(idiomas.Traducir(s.Clave) ?? s.Original);
            ActualizarTextosDinamicos();
        }

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

        private static string Clave(Control c) =>
            (c.TemplateControl ?? c.Page).GetType().BaseType.Name + "." + c.ID;

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
