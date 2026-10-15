using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using BE;

namespace BLL
{
    public class PerfilBLL
    {
        private readonly DAL.PerfilDAL _dal = new DAL.PerfilDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();
        private readonly IntegridadBLL _integridad = new IntegridadBLL();

        private static int _versionPermisos;

        public static int VersionPermisos => Volatile.Read(ref _versionPermisos);

        private static void InvalidarPermisos() => Interlocked.Increment(ref _versionPermisos);

        public List<Rol> ObtenerArbol()
        {
            return ConstruirArbol(out _);
        }

        public Rol ObtenerRol(int rolId)
        {
            ConstruirArbol(out Dictionary<int, Rol> mapa);
            return mapa.TryGetValue(rolId, out Rol rol) ? rol : null;
        }

        public List<Rol> ListarRoles() => _dal.ListarRoles();

        private static bool EsGestionable(Dictionary<int, Rol> mapa, HashSet<string> permisosActor, int rolId)
        {
            var visitados = new HashSet<int>();
            int? actual = rolId;
            while (actual.HasValue)
            {
                if (!mapa.TryGetValue(actual.Value, out Rol rol) || !visitados.Add(rol.Id)) return false;
                if (!Cubre(permisosActor, rol.ObtenerPermisos())) return false;
                actual = rol.PadreId;
            }
            return true;
        }

        private static Dictionary<int, Rol> RolesGestionables(EstadoPerfiles estado, HashSet<string> permisosActor)
        {
            Dictionary<int, Rol> mapa = ConstruirMapaDeEstado(estado);
            return mapa.Values.Where(r => EsGestionable(mapa, permisosActor, r.Id)).ToDictionary(r => r.Id);
        }

        public List<Rol> ListarRolesGestionablesPor(BE.USUARIO actor)
        {
            EstadoPerfiles estado = CargarEstado();
            return RolesGestionables(estado, ExigirGestorDePerfiles(estado, actor)).Values.ToList();
        }

        public List<Rol> ObtenerArbolGestionablePor(BE.USUARIO actor)
        {
            EstadoPerfiles estado = CargarEstado();
            Dictionary<int, Rol> gestionables = RolesGestionables(estado, ExigirGestorDePerfiles(estado, actor));
            return gestionables.Values.Where(r => !r.PadreId.HasValue).OrderBy(r => r.Id).ToList();
        }

        public Rol ObtenerRolGestionablePor(int rolId, BE.USUARIO actor)
        {
            EstadoPerfiles estado = CargarEstado();
            Dictionary<int, Rol> gestionables = RolesGestionables(estado, ExigirGestorDePerfiles(estado, actor));
            return gestionables.TryGetValue(rolId, out Rol rol) ? rol : null;
        }

        public List<Permiso> ListarPermisosOtorgablesPor(BE.USUARIO actor)
        {
            HashSet<string> permisos = ExigirGestorDePerfiles(CargarEstado(), actor);
            return _dal.ListarPermisosDisponibles().Where(p => Cubre(permisos, new[] { p.Nombre })).ToList();
        }

        public List<Permiso> ListarPermisosDisponibles() => _dal.ListarPermisosDisponibles();

        public List<int> ObtenerIdsPermisosDirectosPor(int rolId, BE.USUARIO actor)
        {
            if (ObtenerRolGestionablePor(rolId, actor) == null) throw new InvalidOperationException(MensajePerfilNoExiste);
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

        public bool RefrescarPermisosSiHaceFalta(BE.USUARIO usuario)
        {
            int version = VersionPermisos;
            if (usuario.VersionPermisos == version) return usuario.Activo;

            BE.USUARIO actual = new DAL.UsuarioDAL().ListarTodos().FirstOrDefault(x => x.Id == usuario.Id);
            usuario.Activo = actual != null && actual.Activo;
            usuario.Permisos = usuario.Activo ? ObtenerPermisosEfectivos(usuario.Id) : new List<string>();
            usuario.VersionPermisos = version;
            return usuario.Activo;
        }

        public List<int> ListarUsuariosConPermiso(string nombrePermiso)
        {
            return PermisosEfectivosPorUsuario(CargarEstado())
                .Where(par => par.Value.Contains(nombrePermiso))
                .Select(par => par.Key)
                .ToList();
        }

        private class EstadoPerfiles
        {
            public List<Rol> Roles;
            public Dictionary<int, List<string>> PermisosPorRol;
            public List<KeyValuePair<int, int>> Asignaciones;
            public HashSet<int> Bloqueados;
            public HashSet<int> Existentes;
        }

        private EstadoPerfiles CargarEstado()
        {
            var estado = new EstadoPerfiles
            {
                Roles = _dal.ListarRoles(),
                PermisosPorRol = new Dictionary<int, List<string>>(),
                Asignaciones = _dal.ListarAsignaciones(),
            };
            List<BE.USUARIO> todos = new DAL.UsuarioDAL().ListarTodos();
            estado.Bloqueados = new HashSet<int>(todos.Where(u => u.Bloqueado || !u.Activo).Select(u => u.Id));
            estado.Existentes = new HashSet<int>(todos.Select(u => u.Id));
            foreach (Permiso p in _dal.ListarPermisosPorRol())
            {
                if (!estado.PermisosPorRol.TryGetValue(p.PadreId.Value, out List<string> lista))
                    estado.PermisosPorRol[p.PadreId.Value] = lista = new List<string>();
                lista.Add(p.Nombre);
            }
            return estado;
        }

        private static Dictionary<int, Rol> ConstruirMapaDeEstado(EstadoPerfiles estado)
        {
            var mapa = estado.Roles.ToDictionary(r => r.Id,
                r => new Rol { Id = r.Id, Nombre = r.Nombre, PadreId = r.PadreId, Protegido = r.Protegido });
            foreach (Rol r in mapa.Values)
                if (r.PadreId.HasValue && mapa.TryGetValue(r.PadreId.Value, out Rol padre))
                    padre.Agregar(r);
            foreach (var par in estado.PermisosPorRol)
                if (mapa.TryGetValue(par.Key, out Rol dueno))
                    foreach (string nombre in par.Value)
                        dueno.Agregar(new Permiso { Nombre = nombre, PadreId = par.Key });
            return mapa;
        }

        private static Dictionary<int, HashSet<string>> PermisosEfectivosPorUsuario(EstadoPerfiles estado)
        {
            Dictionary<int, Rol> mapa = ConstruirMapaDeEstado(estado);

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
                .Any(par => par.Value.Contains(Permisos.GestionarPerfiles)
                            && !estadoResultante.Bloqueados.Contains(par.Key));
            if (!hayOperador)
                throw new InvalidOperationException(
                    "El cambio dejaría al sistema sin ningún usuario activo y no bloqueado con permiso para gestionar perfiles.");
        }

        private const string MensajePerfilNoExiste = "El perfil no existe.";

        private static Rol BuscarRol(EstadoPerfiles estado, int rolId)
        {
            Rol rol = estado.Roles.FirstOrDefault(r => r.Id == rolId);
            if (rol == null) throw new InvalidOperationException(MensajePerfilNoExiste);
            return rol;
        }

        private static Rol BuscarRolGestionable(EstadoPerfiles estado, HashSet<string> permisosActor, int rolId)
        {
            Rol rol = BuscarRol(estado, rolId);
            if (!EsGestionable(ConstruirMapaDeEstado(estado), permisosActor, rolId))
                throw new InvalidOperationException(MensajePerfilNoExiste);
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

        private static string Accion(string prefijo, string detalle)
        {
            string texto = prefijo + ":" + detalle;
            if (texto.Length <= 100) return texto;
            texto = texto.Substring(0, 100);
            return char.IsHighSurrogate(texto[texto.Length - 1]) ? texto.Substring(0, 99) : texto;
        }

        private static readonly object _lockEscritura = new object();

        private void PostEscritura(string actor, string accion, params Action[] recalculos)
        {
            Exception primerError = null;
            try
            {
                foreach (Action recalcular in recalculos)
                {
                    try { recalcular(); }
                    catch (Exception ex) { primerError = primerError ?? ex; }
                }
                try { _bitacora.RegistrarAccion(actor, accion); }
                catch (Exception ex) { primerError = primerError ?? ex; }
            }
            finally { InvalidarPermisos(); }

            if (primerError != null)
                throw new InvalidOperationException(
                    "El cambio se guardó, pero falló un paso posterior (integridad o bitácora): " + primerError.Message,
                    primerError);
        }

        public int CrearRol(string nombre, int? padreId, BE.USUARIO actor)
        {
            nombre = NormalizarNombre(nombre);
            lock (_lockEscritura)
            {
                EstadoPerfiles estado = CargarEstado();
                HashSet<string> permisosActor = ExigirGestorDePerfiles(estado, actor);
                if (padreId.HasValue) BuscarRolGestionable(estado, permisosActor, padreId.Value);
                ExigirNombreLibre(estado, nombre, null);

                int id = _dal.InsertarRol(nombre, padreId);
                PostEscritura(actor.Usuario, Accion("ALTA_PERFIL", nombre), _integridad.RecalcularRol);
                return id;
            }
        }

        public void RenombrarRol(int rolId, string nombre, BE.USUARIO actor)
        {
            nombre = NormalizarNombre(nombre);
            lock (_lockEscritura)
            {
                EstadoPerfiles estado = CargarEstado();
                HashSet<string> permisosActor = ExigirGestorDePerfiles(estado, actor);
                Rol rol = BuscarRolGestionable(estado, permisosActor, rolId);
                ExigirNoProtegido(rol, "renombrar");
                ExigirNombreLibre(estado, nombre, rolId);

                _dal.RenombrarRol(rolId, nombre);
                PostEscritura(actor.Usuario, Accion("RENOMBRE_PERFIL", rol.Nombre + ">" + nombre), _integridad.RecalcularRol);
            }
        }

        public void CambiarPadre(int rolId, int? nuevoPadreId, BE.USUARIO actor)
        {
            lock (_lockEscritura)
            {
                EstadoPerfiles estado = CargarEstado();
                HashSet<string> permisosActor = ExigirGestorDePerfiles(estado, actor);
                Rol rol = BuscarRolGestionable(estado, permisosActor, rolId);
                ExigirNoProtegido(rol, "mover");

                if (nuevoPadreId.HasValue)
                {
                    BuscarRolGestionable(estado, permisosActor, nuevoPadreId.Value);
                    if (GenerariaCiclo(rolId, nuevoPadreId.Value, estado.Roles))
                        throw new InvalidOperationException(
                            "No se puede asignar ese padre: generaría una referencia circular.");
                }

                rol.PadreId = nuevoPadreId;
                ExigirOperadorDePerfiles(estado);

                _dal.CambiarPadre(rolId, nuevoPadreId);
                PostEscritura(actor.Usuario, Accion("CAMBIO_JERARQUIA_PERFIL", rol.Nombre), _integridad.RecalcularRol);
            }
        }

        public void EliminarRol(int rolId, BE.USUARIO actor)
        {
            lock (_lockEscritura)
            {
                EstadoPerfiles estado = CargarEstado();
                HashSet<string> permisosActor = ExigirGestorDePerfiles(estado, actor);
                Rol rol = BuscarRolGestionable(estado, permisosActor, rolId);
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
                PostEscritura(actor.Usuario, Accion("BAJA_PERFIL", rol.Nombre),
                    _integridad.RecalcularRolPermiso, _integridad.RecalcularRol);
            }
        }

        public void GuardarPermisosDeRol(int rolId, List<int> permisoIds, BE.USUARIO actor)
        {
            lock (_lockEscritura)
            {
                EstadoPerfiles estado = CargarEstado();
                HashSet<string> permisosActor = ExigirGestorDePerfiles(estado, actor);
                Rol rol = BuscarRolGestionable(estado, permisosActor, rolId);
                ExigirNoProtegido(rol, "modificar sus permisos");

                Dictionary<int, string> catalogo = _dal.ListarPermisosDisponibles().ToDictionary(p => p.Id, p => p.Nombre);
                var ids = permisoIds.Distinct().ToList();
                if (ids.Any(id => !catalogo.ContainsKey(id)))
                    throw new InvalidOperationException("Se indicó un permiso que no existe.");

                List<string> otorgados = ids.Select(id => catalogo[id]).ToList();
                if (!otorgados.All(n => Cubre(permisosActor, new[] { n })))
                    throw new InvalidOperationException("Se indicó un permiso que no existe.");

                estado.PermisosPorRol[rolId] = otorgados;
                ExigirOperadorDePerfiles(estado);

                _dal.GuardarPermisosDeRol(rolId, ids);
                PostEscritura(actor.Usuario, Accion("MODIF_PERMISOS_PERFIL", rol.Nombre), _integridad.RecalcularRolPermiso);
            }
        }

        public void AsignarPerfilesAUsuario(int usuarioId, List<int> rolIds, BE.USUARIO actor)
        {
            lock (_lockEscritura)
            {
                EstadoPerfiles estado = CargarEstado();
                HashSet<string> permisosActor = PermisosDelActor(estado, actor);
                if (!permisosActor.Contains(Permisos.GestionarPerfiles))
                    throw new InvalidOperationException("No tenés permiso para asignar perfiles.");
                ExigirPuedeActuarSobre(estado, permisosActor, usuarioId);
                var actuales = new HashSet<int>(estado.Asignaciones.Where(a => a.Key == usuarioId).Select(a => a.Value));
                var nuevos = new HashSet<int>(rolIds ?? new List<int>());
                ValidarAsignacionDePerfiles(usuarioId, actor, actuales, nuevos);
                ExigirPuedeAsignar(estado, permisosActor, actuales.Union(nuevos).Except(actuales.Intersect(nuevos)));
                AsignarPerfilesInterno(estado, usuarioId, rolIds, actor.Usuario);
            }
        }

        public static void ValidarAsignacionDePerfiles(int usuarioId, BE.USUARIO actor, ISet<int> actuales, ISet<int> nuevos)
        {
            if (nuevos.Count == 0)
                throw new InvalidOperationException("El usuario debe tener al menos un perfil.");
            if (usuarioId == actor.Id && !actuales.SetEquals(nuevos))
                throw new InvalidOperationException("No podés cambiar tus propios perfiles.");
        }

        private void AsignarPerfilesInterno(EstadoPerfiles estado, int usuarioId, List<int> rolIds, string nombreActor)
        {
            var ids = rolIds.Distinct().ToList();
            if (ids.Any(id => estado.Roles.All(r => r.Id != id)))
                throw new InvalidOperationException("Se indicó un perfil que no existe.");

            estado.Asignaciones.RemoveAll(a => a.Key == usuarioId);
            foreach (int id in ids)
                estado.Asignaciones.Add(new KeyValuePair<int, int>(usuarioId, id));
            ExigirOperadorDePerfiles(estado);

            _dal.ReemplazarPerfilesDeUsuario(usuarioId, ids);
            PostEscritura(nombreActor, Accion("ASIGNACION_PERFIL", usuarioId + "/" + string.Join(",", ids)),
                _integridad.RecalcularUsuarioPerfil);
        }

        private const string PoliticaContrasena = @"^(?=.*[A-Z])(?=.*\d).{6,}$";
        private const string MensajePoliticaContrasena = "La contraseña debe tener 6+ caracteres, 1 mayúscula y 1 número.";

        private const int MaxBytesDireccionCifrada = 256;
        public const int MaxCaracteresDireccion = 100;

        private readonly DAL.UsuarioDAL _usuarioDal = new DAL.UsuarioDAL();

        private static long _epoca;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, long> _revocaciones =
            new System.Collections.Concurrent.ConcurrentDictionary<int, long>();

        public static long NuevaEpocaDeSesion() => Interlocked.Increment(ref _epoca);

        private static void RevocarSesionesDe(int usuarioId, string nombreUsuario)
        {
            _revocaciones[usuarioId] = Interlocked.Increment(ref _epoca);
            BE.SessionRegistry.Instancia.ForzarCierre(nombreUsuario);
        }

        public static bool SesionRevocada(BE.USUARIO usuario) =>
            _revocaciones.TryGetValue(usuario.Id, out long revocadaEn) && revocadaEn > usuario.EpocaSesion;

        private static HashSet<string> PermisosDelActor(EstadoPerfiles estado, BE.USUARIO actor)
        {
            if (actor == null) throw new InvalidOperationException("No hay un usuario que realice la operación.");
            if (!estado.Existentes.Contains(actor.Id) || estado.Bloqueados.Contains(actor.Id))
                throw new InvalidOperationException("Tu cuenta no está habilitada para realizar esta operación.");
            return PermisosEfectivosPorUsuario(estado).TryGetValue(actor.Id, out HashSet<string> permisos)
                ? permisos : new HashSet<string>();
        }

        private static HashSet<string> ExigirGestorDePerfiles(EstadoPerfiles estado, BE.USUARIO actor)
        {
            HashSet<string> permisos = PermisosDelActor(estado, actor);
            if (!permisos.Contains(Permisos.GestionarPerfiles))
                throw new InvalidOperationException("No tenés permiso para gestionar perfiles.");
            return permisos;
        }

        private static bool Cubre(HashSet<string> permisosActor, IEnumerable<string> permisosObjetivo)
        {
            if (permisosActor.Contains(Permisos.GestionarPerfiles))
                permisosObjetivo = permisosObjetivo.Where(Permisos.EsPrivilegiado);
            return permisosActor.IsSupersetOf(permisosObjetivo);
        }

        private static HashSet<string> PermisosDeRol(EstadoPerfiles estado, int rolId) =>
            ConstruirMapaDeEstado(estado).TryGetValue(rolId, out Rol rol)
                ? new HashSet<string>(rol.ObtenerPermisos()) : new HashSet<string>();

        private static HashSet<string> PermisosDelGestor(EstadoPerfiles estado, BE.USUARIO actor)
        {
            HashSet<string> permisos = PermisosDelActor(estado, actor);
            if (!permisos.Contains(Permisos.GestionarUsuarios))
                throw new InvalidOperationException("No tenés permiso para gestionar usuarios.");
            return permisos;
        }

        private static HashSet<string> PermisosDeLectorDeUsuarios(EstadoPerfiles estado, BE.USUARIO actor)
        {
            HashSet<string> permisos = PermisosDelActor(estado, actor);
            if (!permisos.Contains(Permisos.GestionarUsuarios) && !permisos.Contains(Permisos.GestionarPerfiles))
                throw new InvalidOperationException("No tenés permiso para ver usuarios.");
            return permisos;
        }

        private static HashSet<string> PermisosDeUsuario(EstadoPerfiles estado, int usuarioId) =>
            PermisosEfectivosPorUsuario(estado).TryGetValue(usuarioId, out HashSet<string> permisos)
                ? permisos : new HashSet<string>();

        private static List<int> RolesAsignables(EstadoPerfiles estado, HashSet<string> permisosActor)
        {
            Dictionary<int, Rol> mapa = ConstruirMapaDeEstado(estado);
            return mapa.Values.Where(r => Cubre(permisosActor, r.ObtenerPermisos())).Select(r => r.Id).ToList();
        }

        private static void ExigirPuedeAsignar(EstadoPerfiles estado, HashSet<string> permisosActor, IEnumerable<int> rolIds)
        {
            List<int> permitidos = RolesAsignables(estado, permisosActor);
            foreach (int rolId in rolIds)
                if (!permitidos.Contains(rolId))
                {
                    throw new InvalidOperationException("Se indicó un perfil que no existe.");
                }
        }

        private static bool PuedeActuarSobre(EstadoPerfiles estado, HashSet<string> permisosActor, int usuarioId)
        {
            return Cubre(permisosActor, PermisosDeUsuario(estado, usuarioId));
        }

        private const string MensajeUsuarioNoExiste = "El usuario no existe.";

        private static void ExigirPuedeActuarSobre(EstadoPerfiles estado, HashSet<string> permisosActor, int usuarioId)
        {
            if (!estado.Existentes.Contains(usuarioId) || !PuedeActuarSobre(estado, permisosActor, usuarioId))
                throw new InvalidOperationException(MensajeUsuarioNoExiste);
        }

        public List<int> ListarRolesAsignablesPor(BE.USUARIO actor)
        {
            EstadoPerfiles estado = CargarEstado();
            return RolesAsignables(estado, PermisosDeLectorDeUsuarios(estado, actor));
        }

        public HashSet<int> ListarUsuariosActuablesPor(BE.USUARIO actor)
        {
            EstadoPerfiles estado = CargarEstado();
            HashSet<string> permisos = PermisosDeLectorDeUsuarios(estado, actor);
            return new HashSet<int>(_usuarioDal.ListarTodos().Select(u => u.Id)
                .Where(id => PuedeActuarSobre(estado, permisos, id)));
        }

        public List<BE.USUARIO> ListarUsuariosVisiblesPor(BE.USUARIO actor)
        {
            EstadoPerfiles estado = CargarEstado();
            HashSet<string> permisos = PermisosDeLectorDeUsuarios(estado, actor);
            List<BE.USUARIO> usuarios = _usuarioDal.ListarTodos()
                .Where(u => PuedeActuarSobre(estado, permisos, u.Id)).ToList();
            Dictionary<int, string> nombresRol = _dal.ListarRoles().ToDictionary(r => r.Id, r => r.Nombre);
            ILookup<int, int> asignaciones = estado.Asignaciones.ToLookup(a => a.Key, a => a.Value);
            foreach (BE.USUARIO u in usuarios)
                u.PerfilesTexto = string.Join(", ", asignaciones[u.Id]
                    .Where(nombresRol.ContainsKey).Select(id => nombresRol[id]).OrderBy(n => n));
            return usuarios;
        }

        public BE.PaginaResultado<BE.USUARIO> ListarUsuariosVisiblesPor(BE.USUARIO actor, int pagina, int tamanio)
        {
            List<BE.USUARIO> visibles = ListarUsuariosVisiblesPor(actor).OrderBy(u => u.Usuario).ToList();
            if (tamanio < 1) tamanio = 10;
            var resultado = new BE.PaginaResultado<BE.USUARIO> { TotalFilas = visibles.Count, Tamanio = tamanio };
            resultado.Pagina = Math.Max(1, Math.Min(pagina, resultado.TotalPaginas));
            resultado.Filas = visibles.Skip((resultado.Pagina - 1) * tamanio).Take(tamanio).ToList();
            return resultado;
        }

        private BE.USUARIO ExigirVisible(int usuarioId, BE.USUARIO actor, bool soloGestorDeUsuarios, out EstadoPerfiles estado)
        {
            estado = CargarEstado();
            HashSet<string> permisos = soloGestorDeUsuarios
                ? PermisosDelGestor(estado, actor) : PermisosDeLectorDeUsuarios(estado, actor);
            BE.USUARIO u = BuscarUsuario(_usuarioDal.ListarTodos(), usuarioId);
            ExigirPuedeActuarSobre(estado, permisos, usuarioId);
            return u;
        }

        public List<int> ObtenerIdsRolesDeUsuarioPor(int usuarioId, BE.USUARIO actor)
        {
            ExigirVisible(usuarioId, actor, false, out _);
            return _dal.ListarRolesDeUsuario(usuarioId);
        }

        public List<string> ObtenerPermisosEfectivosPor(int usuarioId, BE.USUARIO actor)
        {
            ExigirVisible(usuarioId, actor, false, out _);
            return ObtenerPermisosEfectivos(usuarioId);
        }

        public string ObtenerDireccion(string usuario, BE.USUARIO actor)
        {
            PermisosDelGestor(CargarEstado(), actor);
            BE.USUARIO u = _usuarioDal.ListarTodos().FirstOrDefault(x =>
                string.Equals(x.Usuario, usuario, StringComparison.OrdinalIgnoreCase));
            if (u == null) throw new InvalidOperationException(MensajeUsuarioNoExiste);
            ExigirVisible(u.Id, actor, true, out _);
            return _usuarioDal.ObtenerPorNombre(u.Usuario)?.DireccionVisible;
        }

        private BE.USUARIO ExigirDesbloqueable(BE.USUARIO actor, string usuario, out EstadoPerfiles estado)
        {
            estado = CargarEstado();
            HashSet<string> permisos = PermisosDelActor(estado, actor);
            if (!permisos.Contains(Permisos.DesbloquearUsuarios))
                throw new InvalidOperationException("No tenés permiso para desbloquear usuarios.");
            BE.USUARIO u = _usuarioDal.ListarTodos().FirstOrDefault(x =>
                string.Equals(x.Usuario, usuario, StringComparison.OrdinalIgnoreCase));
            if (u == null) throw new InvalidOperationException(MensajeUsuarioNoExiste);
            ExigirPuedeActuarSobre(estado, permisos, u.Id);
            return u;
        }

        public List<BE.USUARIO> ListarBloqueadosVisiblesPor(BE.USUARIO actor)
        {
            EstadoPerfiles estado = CargarEstado();
            HashSet<string> permisos = PermisosDelActor(estado, actor);
            if (!permisos.Contains(Permisos.DesbloquearUsuarios))
                throw new InvalidOperationException("No tenés permiso para desbloquear usuarios.");
            return _usuarioDal.ListarBloqueados().Where(u => PuedeActuarSobre(estado, permisos, u.Id)).ToList();
        }

        public void DesbloquearUsuario(string usuario, BE.USUARIO actor)
        {
            lock (_lockEscritura)
            {
                BE.USUARIO u = ExigirDesbloqueable(actor, usuario, out _);
                _usuarioDal.Desbloquear(u.Usuario);
                PostEscritura(actor.Usuario, "DESBLOQUEO_USUARIO:" + u.Usuario, _integridad.RecalcularUsuario);
            }
        }

        private static void ValidarContrasena(string contrasena, string confirmacion)
        {
            if (string.IsNullOrEmpty(contrasena)) throw new InvalidOperationException("La contraseña es obligatoria.");
            if (contrasena != confirmacion) throw new InvalidOperationException("Las contraseñas no coinciden.");
            if (!Regex.IsMatch(contrasena, PoliticaContrasena)) throw new InvalidOperationException(MensajePoliticaContrasena);
        }

        private static void ValidarDireccion(string direccion)
        {
            if (string.IsNullOrEmpty(direccion)) return;
            byte[] cifrada = BE.AESHelper.Encriptar(direccion);
            if (cifrada != null && cifrada.Length > MaxBytesDireccionCifrada)
                throw new InvalidOperationException(
                    $"La dirección es demasiado larga (máximo {MaxCaracteresDireccion} caracteres; los acentos y símbolos ocupan más).");
        }

        private static BE.USUARIO BuscarUsuario(List<BE.USUARIO> usuarios, int usuarioId)
        {
            BE.USUARIO u = usuarios.FirstOrDefault(x => x.Id == usuarioId);
            if (u == null) throw new InvalidOperationException(MensajeUsuarioNoExiste);
            return u;
        }

        public int CrearUsuario(string usuario, string contrasena, string confirmacion, string direccion,
            List<int> rolIds, BE.USUARIO actor)
        {
            usuario = (usuario ?? "").Trim();
            if (usuario.Length == 0) throw new InvalidOperationException("El nombre de usuario es obligatorio.");
            if (usuario.Length > 50) throw new InvalidOperationException("El nombre de usuario no puede superar los 50 caracteres.");
            ValidarContrasena(contrasena, confirmacion);
            direccion = (direccion ?? "").Trim();
            ValidarDireccion(direccion);
            var ids = (rolIds ?? new List<int>()).Distinct().ToList();
            if (ids.Count == 0) throw new InvalidOperationException("Elegí al menos un perfil.");

            lock (_lockEscritura)
            {
                EstadoPerfiles estado = CargarEstado();
                if (ids.Any(id => estado.Roles.All(r => r.Id != id)))
                    throw new InvalidOperationException("Se indicó un perfil que no existe.");
                ExigirPuedeAsignar(estado, PermisosDelGestor(estado, actor), ids);
                if (_usuarioDal.ListarTodos().Any(u => string.Equals(u.Usuario, usuario, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Ya existe un usuario con ese nombre.");

                int usuarioId = _usuarioDal.InsertarConPerfiles(usuario, HashHelper.HashSHA256(contrasena), direccion, ids);
                PostEscritura(actor.Usuario, Accion("ALTA_USUARIO", usuario),
                    _integridad.RecalcularUsuario, _integridad.RecalcularUsuarioPerfil);
                return usuarioId;
            }
        }

        public void ModificarUsuario(int usuarioId, string direccion, List<int> rolIds, BE.USUARIO actor)
        {
            direccion = (direccion ?? "").Trim();
            ValidarDireccion(direccion);
            var nuevos = new HashSet<int>(rolIds ?? new List<int>());
            if (nuevos.Count == 0) throw new InvalidOperationException("El usuario debe tener al menos un perfil.");

            lock (_lockEscritura)
            {
                EstadoPerfiles estado = CargarEstado();
                HashSet<string> permisosActor = PermisosDelGestor(estado, actor);
                BE.USUARIO u = BuscarUsuario(_usuarioDal.ListarTodos(), usuarioId);
                ExigirPuedeActuarSobre(estado, permisosActor, usuarioId);

                var actuales = new HashSet<int>(estado.Asignaciones.Where(a => a.Key == usuarioId).Select(a => a.Value));
                bool cambianPerfiles = !actuales.SetEquals(nuevos);
                if (cambianPerfiles)
                {
                    if (usuarioId == actor.Id)
                        throw new InvalidOperationException("No podés cambiar tus propios perfiles desde Usuarios.");
                    ExigirPuedeAsignar(estado, permisosActor, actuales.Union(nuevos).Except(actuales.Intersect(nuevos)));
                }

                bool cambiaDireccion = direccion.Length > 0
                    && direccion != (_usuarioDal.ObtenerPorNombre(u.Usuario)?.DireccionVisible ?? "");

                if (cambianPerfiles)
                    AsignarPerfilesInterno(estado, usuarioId, nuevos.ToList(), actor.Usuario);

                if (cambiaDireccion)
                {
                    try
                    {
                        _usuarioDal.ActualizarDireccion(u.Usuario, direccion);
                        PostEscritura(actor.Usuario, Accion("MODIF_USUARIO", u.Usuario), _integridad.RecalcularUsuario);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            (cambianPerfiles ? "Los perfiles se guardaron, pero " : "No se guardó: ")
                            + "falló la actualización de la dirección (" + ex.Message + ").", ex);
                    }
                }
            }
        }

        public void CambiarEstadoUsuario(int usuarioId, bool activo, BE.USUARIO actor)
        {
            lock (_lockEscritura)
            {
                EstadoPerfiles estado = CargarEstado();
                BE.USUARIO u = BuscarUsuario(_usuarioDal.ListarTodos(), usuarioId);
                ExigirPuedeActuarSobre(estado, PermisosDelGestor(estado, actor), usuarioId);
                if (u.Activo == activo) return;

                if (!activo)
                {
                    if (usuarioId == actor.Id)
                        throw new InvalidOperationException("No podés darte de baja a vos mismo.");
                    estado.Bloqueados.Add(usuarioId);
                    ExigirOperadorDePerfiles(estado);
                }

                _usuarioDal.ActualizarEstado(usuarioId, activo);
                if (!activo) RevocarSesionesDe(usuarioId, u.Usuario);
                PostEscritura(actor.Usuario, Accion(activo ? "REACTIVAR_USUARIO" : "BAJA_USUARIO", u.Usuario),
                    _integridad.RecalcularUsuario);
            }
        }

        public void ResetearContrasena(int usuarioId, string contrasena, string confirmacion, BE.USUARIO actor)
        {
            ValidarContrasena(contrasena, confirmacion);
            lock (_lockEscritura)
            {
                EstadoPerfiles estado = CargarEstado();
                BE.USUARIO u = BuscarUsuario(_usuarioDal.ListarTodos(), usuarioId);
                if (usuarioId == actor.Id)
                    throw new InvalidOperationException("Tu propia contraseña se cambia desde Contraseña.");
                ExigirPuedeActuarSobre(estado, PermisosDelGestor(estado, actor), usuarioId);

                if (!_usuarioDal.CambiarContrasena(u.Usuario, HashHelper.HashSHA256(contrasena)))
                    throw new InvalidOperationException("No se pudo restablecer la contraseña.");
                RevocarSesionesDe(usuarioId, u.Usuario);
                PostEscritura(actor.Usuario, Accion("RESET_CONTRASENA", u.Usuario), _integridad.RecalcularUsuario);
            }
        }

        public static bool GenerariaCiclo(int rolId, int candidatoPadreId, List<Rol> todos)
        {
            var porId = todos.ToDictionary(r => r.Id);
            var visitados = new HashSet<int>();
            int? actual = candidatoPadreId;
            while (actual.HasValue)
            {
                if (actual.Value == rolId) return true;
                if (!visitados.Add(actual.Value)) return true;
                actual = porId.TryGetValue(actual.Value, out Rol r) ? r.PadreId : null;
            }
            return false;
        }
    }
}
