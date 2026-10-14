using System;
using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// Hoja del Composite: un permiso puntual, identificado por su nombre.
    /// </summary>
    public class Permiso : NodoPermiso
    {
        public override void Agregar(NodoPermiso nodo)
        {
            throw new InvalidOperationException("Un permiso no puede contener hijos.");
        }

        public override void Quitar(NodoPermiso nodo)
        {
            throw new InvalidOperationException("Un permiso no puede contener hijos.");
        }

        public override IList<NodoPermiso> ObtenerHijos()
        {
            return new List<NodoPermiso>();
        }

        public override bool EsHoja()
        {
            return true;
        }

        public override bool TienePermiso(string nombrePermiso)
        {
            return Nombre == nombrePermiso;
        }

        public override IEnumerable<string> ObtenerPermisos()
        {
            yield return Nombre;
        }
    }
}
