using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BE;

namespace BLL
{
    /// <summary>
    /// Perfiles y permisos. Arma el árbol Composite en memoria (Rol contiene
    /// Roles hijos y Permisos hoja) y resuelve los permisos efectivos de un
    /// usuario consultando ese árbol, sin una consulta SQL plana.
    ///
    /// Reglas de gestión:
    ///  - Un perfil protegido no se renombra, no se elimina, no cambia de padre
    ///    ni se le editan los permisos (así Administrador conserva siempre
    ///    "Gestionar perfiles" y Webmaster "Verificar integridad").
    ///  - Ningún cambio puede dejar al sistema sin al menos un usuario cuyos
    ///    permisos efectivos incluyan "Gestionar perfiles".
    ///  - No se elimina un perfil que tenga perfiles hijos o usuarios asignados.
    /// </summary>
    public class PerfilBLL
    {
        private readonly DAL.PerfilDAL _dal = new DAL.PerfilDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();
        private readonly IntegridadBLL _integridad = new IntegridadBLL();

        private static int _versionPermisos;

        /// <summary>
        /// Contador que aumenta con cada cambio de perfiles, permisos o
        /// asignaciones. Las sesiones guardan la versión con la que calcularon
        /// sus permisos y los recalculan cuando ya no coincide.
        /// </summary>
        public static int VersionPermisos => Volatile.Read(ref _versionPermisos);

        private static void InvalidarPermisos() => Interlocked.Increment(ref _versionPermisos);

        // =====================================================
        // Lectura
        // =====================================================

        /// <summary>
        /// Carga todos los roles y permisos una sola vez y devuelve los roles
        /// raíz con sus hijos y permisos ya enganchados.
        /// </summary>
        public List<Rol> ObtenerArbol()
        {
            return ConstruirArbol(out _);
        }

        /// <summary>El rol con su subárbol cargado, o null si no existe.</summary>
        public Rol ObtenerRol(int rolId)
        {
            ConstruirArbol(out Dictionary<int, Rol> mapa);
            return mapa.TryGetValue(rolId, out Rol rol) ? rol : null;
        }

        public List<Rol> ListarRoles() => _dal.ListarRoles();

        public List<Permiso> ListarPermisosDisponibles() => _dal.ListarPermisosDisponibles();

        /// <summary>IDs de los permisos asignados directamente al rol.</summary>
        public List<int> ObtenerIdsPermisosDirectos(int rolId)
        {
            return _dal.ListarPermisosPorRol().Where(p => p.PadreId == rolId).Select(p => p.Id).ToList();
        }

        public List<int> ObtenerIdsRolesDeUsuario(int usuarioId) => _dal.ListarRolesDeUsuario(usuarioId);

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

        /// <summary>
        /// Recalcula los permisos del usuario de la sesión si cambió algún
        /// perfil desde la última vez que se calcularon.
        /// </summary>
        public void RefrescarPermisosSiHaceFalta(BE.USUARIO usuario)
        {
            int version = VersionPermisos;
            if (usuario.VersionPermisos == version) return;
            usuario.Permisos = ObtenerPermisosEfectivos(usuario.Id);
            usuario.VersionPermisos = version;
        }

        /// <summary>IDs de los usuarios cuyos permisos efectivos incluyen el permiso dado.</summary>
        public List<int> ListarUsuariosConPermiso(string nombrePermiso)
        {
            return PermisosEfectivosPorUsuario(CargarEstado())
                .Where(par => par.Value.Contains(nombrePermiso))
                .Select(par => par.Key)
                .ToList();
        }

        // =====================================================
        // Estado en memoria para simular cambios antes de aplicarlos
        // =====================================================

        private class EstadoPerfiles
        {
            public List<Rol> Roles;
            public Dictionary<int, List<string>> PermisosPorRol;
            public List<KeyValuePair<int, int>> Asignaciones;
        }

        private EstadoPerfiles CargarEstado()
        {
            var estado = new EstadoPerfiles
            {
                Roles = _dal.ListarRoles(),
                PermisosPorRol = new Dictionary<int, List<string>>(),
                Asignaciones = _dal.ListarAsignaciones()
            };
            foreach (Permiso p in _dal.ListarPermisosPorRol())
            {
                if (!estado.PermisosPorRol.TryGetValue(p.PadreId.Value, out List<string> lista))
                    estado.PermisosPorRol[p.PadreId.Value] = lista = new List<string>();
                lista.Add(p.Nombre);
            }
            return estado;
        }

        private static Dictionary<int, HashSet<string>> PermisosEfectivosPorUsuario(EstadoPerfiles estado)
        {
            // Árbol nuevo a partir del estado: los Roles del estado solo traen datos planos.
            var mapa = estado.Roles.ToDictionary(r => r.Id,
                r => new Rol { Id = r.Id, Nombre = r.Nombre, PadreId = r.PadreId, Protegido = r.Protegido });
            foreach (Rol r in mapa.Values)
                if (r.PadreId.HasValue && mapa.TryGetValue(r.PadreId.Value, out Rol padre))
                    padre.Agregar(r);
            foreach (var par in estado.PermisosPorRol)
                if (mapa.TryGetValue(par.Key, out Rol dueno))
                    foreach (string nombre in par.Value)
                        dueno.Agregar(new Permiso { Nombre = nombre, PadreId = par.Key });

            var resultado = new Dictionary<int, HashSet<string>>();
            foreach (var asignacion in estado.Asignaciones)
            {
                if (!mapa.TryGetValue(asignacion.Value, out Rol rol)) continue;
                if (!resultado.TryGetValue(asignacion.Key, out HashSet<string> permisos))
                    resultado[asignacion.Key] = permisos = new HashSet<string>();
                foreach (string nombre in rol.ObtenerPermisos())
                    permisos.Add(nombre);
            }
            return resultado;
        }

        private static void ExigirOperadorDePerfiles(EstadoPerfiles estadoResultante)
        {
            bool hayOperador = PermisosEfectivosPorUsuario(estadoResultante)
                .Any(par => par.Value.Contains(Permisos.GestionarPerfiles));
            if (!hayOperador)
                throw new InvalidOperationException(
                    "El cambio dejaría al sistema sin ningún usuario con permiso para gestionar perfiles.");
        }

        private static Rol BuscarRol(EstadoPerfiles estado, int rolId)
        {
            Rol rol = estado.Roles.FirstOrDefault(r => r.Id == rolId);
            if (rol == null) throw new InvalidOperationException("El perfil no existe.");
            return rol;
        }

        private static string NormalizarNombre(string nombre)
        {
            nombre = (nombre ?? "").Trim();
            if (nombre.Length == 0) throw new InvalidOperationException("El nombre del perfil es obligatorio.");
            if (nombre.Length > 100) throw new InvalidOperationException("El nombre del perfil no puede superar los 100 caracteres.");
            return nombre;
        }

        private static void ExigirNombreLibre(EstadoPerfiles estado, string nombre, int? ignorarRolId)
        {
            if (estado.Roles.Any(r => r.Id != ignorarRolId
                    && string.Equals(r.Nombre, nombre, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Ya existe un perfil con ese nombre.");
        }

        private static void ExigirNoProtegido(Rol rol, string accion)
        {
            if (rol.Protegido)
                throw new InvalidOperationException($"El perfil {rol.Nombre} es del sistema: no se puede {accion}.");
        }

        // La bitácora guarda ACCION en 100 caracteres.
        private static string Accion(string prefijo, string detalle)
        {
            string texto = prefijo + ":" + detalle;
            return texto.Length <= 100 ? texto : texto.Substring(0, 100);
        }

        // =====================================================
        // Escritura
        // =====================================================

        public int CrearRol(string nombre, int? padreId, string actor)
        {
            nombre = NormalizarNombre(nombre);
            EstadoPerfiles estado = CargarEstado();
            ExigirNombreLibre(estado, nombre, null);
            if (padreId.HasValue) BuscarRol(estado, padreId.Value);

            int id = _dal.InsertarRol(nombre, padreId);
            _bitacora.RegistrarAccion(actor, Accion("ALTA_PERFIL", nombre));
            _integridad.RecalcularRol();
            InvalidarPermisos();
            return id;
        }

        public void RenombrarRol(int rolId, string nombre, string actor)
        {
            nombre = NormalizarNombre(nombre);
            EstadoPerfiles estado = CargarEstado();
            Rol rol = BuscarRol(estado, rolId);
            ExigirNoProtegido(rol, "renombrar");
            ExigirNombreLibre(estado, nombre, rolId);

            _dal.RenombrarRol(rolId, nombre);
            _bitacora.RegistrarAccion(actor, Accion("RENOMBRE_PERFIL", rol.Nombre + ">" + nombre));
            _integridad.RecalcularRol();
            InvalidarPermisos();
        }

        public void CambiarPadre(int rolId, int? nuevoPadreId, string actor)
        {
            EstadoPerfiles estado = CargarEstado();
            Rol rol = BuscarRol(estado, rolId);
            ExigirNoProtegido(rol, "mover");

            if (nuevoPadreId.HasValue)
            {
                BuscarRol(estado, nuevoPadreId.Value);
                if (GenerariaCiclo(rolId, nuevoPadreId.Value, estado.Roles))
                    throw new InvalidOperationException(
                        "No se puede asignar ese padre: generaría una referencia circular.");
            }

            rol.PadreId = nuevoPadreId; // simulación sobre la copia en memoria
            ExigirOperadorDePerfiles(estado);

            _dal.CambiarPadre(rolId, nuevoPadreId);
            _bitacora.RegistrarAccion(actor, Accion("CAMBIO_JERARQUIA_PERFIL", rol.Nombre));
            _integridad.RecalcularRol();
            InvalidarPermisos();
        }

        public void EliminarRol(int rolId, string actor)
        {
            EstadoPerfiles estado = CargarEstado();
            Rol rol = BuscarRol(estado, rolId);
            ExigirNoProtegido(rol, "eliminar");
            if (estado.Roles.Any(r => r.PadreId == rolId))
                throw new InvalidOperationException(
                    "El perfil tiene perfiles hijos: moverlos o eliminarlos primero.");
            if (estado.Asignaciones.Any(a => a.Value == rolId))
                throw new InvalidOperationException(
                    "El perfil tiene usuarios asignados: reasignarlos primero.");

            estado.Roles.Remove(rol);
            estado.PermisosPorRol.Remove(rolId);
            ExigirOperadorDePerfiles(estado);

            _dal.EliminarRol(rolId);
            _bitacora.RegistrarAccion(actor, Accion("BAJA_PERFIL", rol.Nombre));
            _integridad.RecalcularRolPermiso();
            _integridad.RecalcularRol();
            InvalidarPermisos();
        }

        /// <summary>Reemplaza los permisos directos del perfil (en una sola transacción).</summary>
        public void GuardarPermisosDeRol(int rolId, List<int> permisoIds, string actor)
        {
            EstadoPerfiles estado = CargarEstado();
            Rol rol = BuscarRol(estado, rolId);
            ExigirNoProtegido(rol, "modificar sus permisos");

            Dictionary<int, string> catalogo = _dal.ListarPermisosDisponibles().ToDictionary(p => p.Id, p => p.Nombre);
            var ids = permisoIds.Distinct().ToList();
            if (ids.Any(id => !catalogo.ContainsKey(id)))
                throw new InvalidOperationException("Se indicó un permiso que no existe.");

            estado.PermisosPorRol[rolId] = ids.Select(id => catalogo[id]).ToList();
            ExigirOperadorDePerfiles(estado);

            _dal.GuardarPermisosDeRol(rolId, ids);
            _bitacora.RegistrarAccion(actor, Accion("MODIF_PERMISOS_PERFIL", rol.Nombre));
            _integridad.RecalcularRolPermiso();
            InvalidarPermisos();
        }

        /// <summary>Reemplaza los perfiles del usuario (en una sola transacción).</summary>
        public void AsignarPerfilesAUsuario(int usuarioId, List<int> rolIds, string actor)
        {
            EstadoPerfiles estado = CargarEstado();
            var ids = rolIds.Distinct().ToList();
            if (ids.Any(id => estado.Roles.All(r => r.Id != id)))
                throw new InvalidOperationException("Se indicó un perfil que no existe.");

            estado.Asignaciones.RemoveAll(a => a.Key == usuarioId);
            foreach (int id in ids)
                estado.Asignaciones.Add(new KeyValuePair<int, int>(usuarioId, id));
            ExigirOperadorDePerfiles(estado);

            _dal.ReemplazarPerfilesDeUsuario(usuarioId, ids);
            _bitacora.RegistrarAccion(actor, Accion("ASIGNACION_PERFIL", usuarioId + "/" + string.Join(",", ids)));
            _integridad.RecalcularUsuarioPerfil();
            InvalidarPermisos();
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
