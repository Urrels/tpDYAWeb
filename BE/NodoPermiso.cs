using System.Collections.Generic;

namespace BE
{
    public abstract class NodoPermiso
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int? PadreId { get; set; }

        public abstract void Agregar(NodoPermiso nodo);
        public abstract void Quitar(NodoPermiso nodo);
        public abstract IList<NodoPermiso> ObtenerHijos();
        public abstract bool EsHoja();

        public abstract bool TienePermiso(string nombrePermiso);

        public abstract IEnumerable<string> ObtenerPermisos();

        public override string ToString() => Nombre;
    }
}
