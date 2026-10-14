using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// Compuesto del Composite: un perfil (familia de permisos) que contiene
    /// permisos propios y otros perfiles hijos. Los permisos efectivos de un
    /// rol son los suyos más los de todos sus descendientes, resueltos de
    /// forma recursiva a través de <see cref="ObtenerHijos"/>.
    /// </summary>
    public class Rol : NodoPermiso
    {
        public bool Protegido { get; set; }

        private readonly List<NodoPermiso> _hijos = new List<NodoPermiso>();

        // Corta la recursión si los datos tuvieran una referencia circular.
        // El árbol se arma en memoria por pedido, no se comparte entre hilos.
        private bool _visitando;

        public override void Agregar(NodoPermiso nodo)
        {
            _hijos.Add(nodo);
        }

        public override void Quitar(NodoPermiso nodo)
        {
            _hijos.Remove(nodo);
        }

        public override IList<NodoPermiso> ObtenerHijos()
        {
            return _hijos;
        }

        public override bool EsHoja()
        {
            return false;
        }

        public override bool TienePermiso(string nombrePermiso)
        {
            if (_visitando) return false;
            _visitando = true;
            try
            {
                foreach (NodoPermiso hijo in ObtenerHijos())
                    if (hijo.TienePermiso(nombrePermiso)) return true;
                return false;
            }
            finally { _visitando = false; }
        }

        public override IEnumerable<string> ObtenerPermisos()
        {
            var resultado = new HashSet<string>();
            if (_visitando) return resultado;
            _visitando = true;
            try
            {
                foreach (NodoPermiso hijo in ObtenerHijos())
                    foreach (string nombre in hijo.ObtenerPermisos())
                        resultado.Add(nombre);
            }
            finally { _visitando = false; }
            return resultado;
        }
    }
}
