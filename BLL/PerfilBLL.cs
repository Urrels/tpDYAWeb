using System;
using System.Collections.Generic;
using System.Linq;
using BE;

namespace BLL
{
    /// <summary>
    /// Perfiles y permisos. Arma el árbol Composite en memoria (Rol contiene
    /// Roles hijos y Permisos hoja) y resuelve los permisos efectivos de un
    /// usuario consultando ese árbol, sin una consulta SQL plana.
    /// </summary>
    public class PerfilBLL
    {
        private readonly DAL.PerfilDAL _dal = new DAL.PerfilDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();
        private readonly IntegridadBLL _integridad = new IntegridadBLL();

        /// <summary>
        /// Carga todos los roles y permisos una sola vez y devuelve los roles
        /// raíz con sus hijos y permisos ya enganchados.
        /// </summary>
        public List<Rol> ObtenerArbol()
        {
            return ConstruirArbol(out _);
        }

        private List<Rol> ConstruirArbol(out Dictionary<int, Rol> mapa)
        {
            List<Rol> roles = _dal.ListarRoles();
            mapa = roles.ToDictionary(r => r.Id);

            var raices = new List<Rol>();
            foreach (Rol r in roles)
            {
                if (r.PadreId == null)
                    raices.Add(r);
                else if (mapa.TryGetValue(r.PadreId.Value, out Rol padre))
                    padre.Agregar(r);
            }

            foreach (Permiso p in _dal.ListarPermisosPorRol())
            {
                if (p.PadreId.HasValue && mapa.TryGetValue(p.PadreId.Value, out Rol dueno))
                    dueno.Agregar(p);
            }

            return raices;
        }

        /// <summary>
        /// Nombres de los permisos efectivos del usuario: unión de los que
        /// concede cada uno de sus perfiles asignados, incluyendo los de los
        /// perfiles que cada uno contiene.
        /// </summary>
        public List<string> ObtenerPermisosEfectivos(int usuarioId)
        {
            ConstruirArbol(out Dictionary<int, Rol> mapa);

            var permisos = new HashSet<string>();
            foreach (int rolId in _dal.ListarRolesDeUsuario(usuarioId))
            {
                if (mapa.TryGetValue(rolId, out Rol rol))
                    foreach (string nombre in rol.ObtenerPermisos())
                        permisos.Add(nombre);
            }
            return permisos.ToList();
        }

        public void AsignarPerfil(int usuarioId, int rolId, string actor)
        {
            _dal.AsignarPerfil(usuarioId, rolId);
            _bitacora.RegistrarAccion(actor, "ASIGNACION_PERFIL:" + usuarioId + "/" + rolId);
            _integridad.RecalcularUsuarioPerfil();
        }

        public void CambiarPadre(int rolId, int? nuevoPadreId, string actor)
        {
            if (nuevoPadreId.HasValue)
            {
                List<Rol> todos = _dal.ListarRoles();
                if (GenerariaCiclo(rolId, nuevoPadreId.Value, todos))
                    throw new InvalidOperationException(
                        "No se puede asignar ese padre: generaría una referencia circular.");
            }

            _dal.CambiarPadre(rolId, nuevoPadreId);
            _bitacora.RegistrarAccion(actor, "CAMBIO_JERARQUIA_PERFIL:" + rolId);
            _integridad.RecalcularRol();
        }

        /// <summary>
        /// Indica si colgar <paramref name="rolId"/> bajo
        /// <paramref name="candidatoPadreId"/> cerraría un ciclo: recorre los
        /// ancestros del candidato y falla si se encuentra con el propio rol.
        /// </summary>
        public static bool GenerariaCiclo(int rolId, int candidatoPadreId, List<Rol> todos)
        {
            var porId = todos.ToDictionary(r => r.Id);
            var visitados = new HashSet<int>();
            int? actual = candidatoPadreId;
            while (actual.HasValue)
            {
                if (actual.Value == rolId) return true;
                if (!visitados.Add(actual.Value)) return true; // ya había un ciclo en los datos
                actual = porId.TryGetValue(actual.Value, out Rol r) ? r.PadreId : null;
            }
            return false;
        }
    }
}
