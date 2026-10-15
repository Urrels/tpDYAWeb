using System.Collections.Generic;

namespace BE
{
    public class Rol : NodoPermiso
    {
        public bool Protegido { get; set; }

        private readonly List<NodoPermiso> _hijos = new List<NodoPermiso>();

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
