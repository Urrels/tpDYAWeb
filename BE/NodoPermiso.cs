using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// Componente del patrón Composite de perfiles y permisos. Tanto el
    /// <see cref="Rol"/> (compuesto) como el <see cref="Permiso"/> (hoja) se
    /// consultan de la misma forma, sin que quien llama distinga el tipo.
    /// </summary>
    public abstract class NodoPermiso
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int? PadreId { get; set; }

        public abstract void Agregar(NodoPermiso nodo);
        public abstract void Quitar(NodoPermiso nodo);
        public abstract IList<NodoPermiso> ObtenerHijos();
        public abstract bool EsHoja();

        /// <summary>
        /// Indica si el nodo concede el permiso, ya sea propio o heredado de
        /// algún descendiente.
        /// </summary>
        public abstract bool TienePermiso(string nombrePermiso);

        /// <summary>
        /// Nombres de todos los permisos efectivos del nodo (propios más los
        /// de todos sus descendientes), sin repetidos.
        /// </summary>
        public abstract IEnumerable<string> ObtenerPermisos();

        public override string ToString() => Nombre;
    }
}
