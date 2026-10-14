using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    ///  - Quien gestiona perfiles no puede otorgar, quitar ni tocar permisos
    ///    privilegiados (Permisos.Privilegiados) que no posee (ver "Regla de escalada").
    ///
    /// También concentra el alta, modificación y baja lógica de usuarios porque
    /// comparten con los perfiles el lock de escritura y la guarda del último
    /// operador (un usuario inactivo o bloqueado no cuenta como operador).
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

        // Un perfil es gestionable por el actor si él y todos sus ancestros son visibles (el
        // actor cubre sus permisos efectivos). Un cambio en un perfil altera los permisos
        // efectivos de toda su cadena de ancestros, por eso no alcanza con mirar al perfil ni
        // a su padre inmediato: la visibilidad es monótona hacia abajo (si un perfil es visible,
        // también lo son sus descendientes), no hacia arriba, y un padre visible puede colgar
        // de un abuelo oculto. Equivale a que la raíz de la cadena sea visible.
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

        // Los perfiles gestionables por el actor, siempre a partir de un mismo estado en memoria.
        private static Dictionary<int, Rol> RolesGestionables(EstadoPerfiles estado, HashSet<string> permisosActor)
        {
            Dictionary<int, Rol> mapa = ConstruirMapaDeEstado(estado);
            return mapa.Values.Where(r => EsGestionable(mapa, permisosActor, r.Id)).ToDictionary(r => r.Id);
        }

        /// <summary>
        /// Perfiles que el actor puede ver y modificar en Perfiles.aspx: los que él y todos sus
        /// ancestros cubren. Un perfil que cuelga de uno oculto no se muestra.
        /// </summary>
        public List<Rol> ListarRolesGestionablesPor(BE.USUARIO actor)
        {
            EstadoPerfiles estado = CargarEstado();
            return RolesGestionables(estado, ExigirGestorDePerfiles(estado, actor)).Values.ToList();
        }

        /// <summary>
        /// Árbol de los perfiles gestionables. Si un perfil es visible lo es todo su subárbol, así
        /// que lo gestionable son los subárboles completos de las raíces visibles (sin huérfanos).
        /// </summary>
        public List<Rol> ObtenerArbolGestionablePor(BE.USUARIO actor)
        {
            EstadoPerfiles estado = CargarEstado();
            Dictionary<int, Rol> gestionables = RolesGestionables(estado, ExigirGestorDePerfiles(estado, actor));
            return gestionables.Values.Where(r => !r.PadreId.HasValue).OrderBy(r => r.Id).ToList();
        }

        /// <summary>El perfil con su subárbol, o null si no existe o el actor no puede gestionarlo.</summary>
        public Rol ObtenerRolGestionablePor(int rolId, BE.USUARIO actor)
        {
            EstadoPerfiles estado = CargarEstado();
            Dictionary<int, Rol> gestionables = RolesGestionables(estado, ExigirGestorDePerfiles(estado, actor));
            return gestionables.TryGetValue(rolId, out Rol rol) ? rol : null;
        }

        /// <summary>Permisos del catálogo que el actor puede otorgar (para mostrar solo esos en pantalla).</summary>
        public List<Permiso> ListarPermisosOtorgablesPor(BE.USUARIO actor)
        {
            HashSet<string> permisos = ExigirGestorDePerfiles(CargarEstado(), actor);
            return _dal.ListarPermisosDisponibles().Where(p => Cubre(permisos, new[] { p.Nombre })).ToList();
        }

        public List<Permiso> ListarPermisosDisponibles() => _dal.ListarPermisosDisponibles();

        /// <summary>IDs de los permisos asignados directamente a un perfil que el actor puede gestionar.</summary>
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
        /// <returns>false si el usuario fue dado de baja: la sesión debe cerrarse.</returns>
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
            // Usuarios bloqueados o dados de baja: no cuentan como operadores disponibles.
            public HashSet<int> Bloqueados;
            // Todos los usuarios que existen (para verificar al actor).
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

        // Árbol nuevo a partir del estado: los Roles del estado solo traen datos planos.
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

        // Un perfil que el actor no puede ver se trata igual que uno inexistente: el mismo mensaje.
        private const string MensajePerfilNoExiste = "El perfil no existe.";

        private static Rol BuscarRol(EstadoPerfiles estado, int rolId)
        {
            Rol rol = estado.Roles.FirstOrDefault(r => r.Id == rolId);
            if (rol == null) throw new InvalidOperationException(MensajePerfilNoExiste);
            return rol;
        }

        // Como BuscarRol, pero también falla si el actor no puede gestionar el perfil: él y toda su
        // cadena de ancestros deben ser visibles (ver EsGestionable), porque modificarlo cambia los
        // permisos efectivos de los ancestros.
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

        // La bitácora guarda ACCION en 100 caracteres: se corta el detalle (nunca el
        // prefijo) y no se deja un par surrogate partido al final.
        private static string Accion(string prefijo, string detalle)
        {
            string texto = prefijo + ":" + detalle;
            if (texto.Length <= 100) return texto;
            texto = texto.Substring(0, 100);
            return char.IsHighSurrogate(texto[texto.Length - 1]) ? texto.Substring(0, 99) : texto;
        }

        // =====================================================
        // Escritura
        // =====================================================

        // Serializa las escrituras: la guarda de "último operador" simula el
        // resultado antes de escribir, y sin este lock dos operadores podrían
        // quitarse el permiso mutuamente en paralelo. Alcanza con un único
        // proceso de aplicación (pool único); con varios haría falta bloqueo en la base.
        private static readonly object _lockEscritura = new object();

        /// <summary>
        /// Pasos posteriores a la escritura ya confirmada: recalcula la integridad
        /// primero (para acortar la ventana con DVH desactualizado), luego registra
        /// en bitácora, e invalida los permisos de las sesiones siempre. Si algún
        /// paso falla se intentan igual los demás y al final se informa el error.
        /// Queda una ventana residual: entre el commit y el recálculo los DVH están
        /// desactualizados (el motor de integridad no participa de la transacción,
        /// igual que en el resto del proyecto).
        /// </summary>
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
                // Visible = el actor cubre los permisos efectivos del perfil (y los de su subárbol).
                Rol rol = BuscarRolGestionable(estado, permisosActor, rolId);
                ExigirNoProtegido(rol, "mover");

                if (nuevoPadreId.HasValue)
                {
                    BuscarRolGestionable(estado, permisosActor, nuevoPadreId.Value);
                    if (GenerariaCiclo(rolId, nuevoPadreId.Value, estado.Roles))
                        throw new InvalidOperationException(
                            "No se puede asignar ese padre: generaría una referencia circular.");
                }

                rol.PadreId = nuevoPadreId; // simulación sobre la copia en memoria
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

        /// <summary>Reemplaza los permisos directos del perfil (en una sola transacción).</summary>
        public void GuardarPermisosDeRol(int rolId, List<int> permisoIds, BE.USUARIO actor)
        {
            lock (_lockEscritura)
            {
                EstadoPerfiles estado = CargarEstado();
                HashSet<string> permisosActor = ExigirGestorDePerfiles(estado, actor);
                // Visible = ya cubre lo que el perfil concede (propio o heredado); abajo se exige cubrir lo que se le va a conceder.
                Rol rol = BuscarRolGestionable(estado, permisosActor, rolId);
                ExigirNoProtegido(rol, "modificar sus permisos");

                Dictionary<int, string> catalogo = _dal.ListarPermisosDisponibles().ToDictionary(p => p.Id, p => p.Nombre);
                var ids = permisoIds.Distinct().ToList();
                if (ids.Any(id => !catalogo.ContainsKey(id)))
                    throw new InvalidOperationException("Se indicó un permiso que no existe.");

                List<string> otorgados = ids.Select(id => catalogo[id]).ToList();
                // Un permiso que el actor no puede otorgar se trata como inexistente (no figura en su listado).
                if (!otorgados.All(n => Cubre(permisosActor, new[] { n })))
                    throw new InvalidOperationException("Se indicó un permiso que no existe.");

                estado.PermisosPorRol[rolId] = otorgados;
                ExigirOperadorDePerfiles(estado);

                _dal.GuardarPermisosDeRol(rolId, ids);
                PostEscritura(actor.Usuario, Accion("MODIF_PERMISOS_PERFIL", rol.Nombre), _integridad.RecalcularRolPermiso);
            }
        }

        /// <summary>
        /// Reemplaza los perfiles del usuario (en una sola transacción). Es la
        /// operación de AsignarPerfiles.aspx: exige que quien actúa tenga
        /// "Gestionar perfiles", comprobado acá y no solo en la página.
        /// </summary>
        public void AsignarPerfilesAUsuario(int usuarioId, List<int> rolIds, BE.USUARIO actor)
        {
            lock (_lockEscritura)
            {
                EstadoPerfiles estado = CargarEstado();
                HashSet<string> permisosActor = PermisosDelActor(estado, actor);
                if (!permisosActor.Contains(Permisos.GestionarPerfiles))
                    throw new InvalidOperationException("No tenés permiso para asignar perfiles.");
                // Misma regla de escalada que en Usuarios: sobre el usuario y sobre los perfiles que cambian.
                ExigirPuedeActuarSobre(estado, permisosActor, usuarioId);
                var actuales = new HashSet<int>(estado.Asignaciones.Where(a => a.Key == usuarioId).Select(a => a.Value));
                var nuevos = new HashSet<int>(rolIds ?? new List<int>());
                ExigirPuedeAsignar(estado, permisosActor, actuales.Union(nuevos).Except(actuales.Intersect(nuevos)));
                AsignarPerfilesInterno(estado, usuarioId, rolIds, actor.Usuario);
            }
        }

        // Escritura de la asignación; el llamador ya tomó el lock y verificó los permisos del actor.
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

        // =====================================================
        // Usuarios (alta, modificación, baja lógica)
        //
        // Regla de escalada: nadie puede otorgar, quitar ni actuar sobre algo que
        // exceda sus propios permisos. Se comprueba siempre acá (la página solo lo
        // refleja en pantalla). "Cubrir" significa:
        //  - Sin "Gestionar perfiles": el actor posee todos los permisos efectivos
        //    del perfil o usuario involucrado (subconjunto).
        //  - Con "Gestionar perfiles": alcanza con poseer los permisos privilegiados
        //    (Permisos.Privilegiados) involucrados; los demás los administra libremente,
        //    para que Administrador gestione alumnos sin tener sus permisos.
        // Se aplica a: asignar o quitar un perfil (permisos efectivos del perfil);
        // actuar sobre un usuario (editar, restablecer contraseña, baja, reactivación,
        // asignar perfiles; sus permisos efectivos); y a los perfiles (otorgar un permiso,
        // modificar, mover o eliminar uno cuyos permisos efectivos incluyen privilegiados).
        // Nadie cambia sus propios perfiles ni su contraseña desde Usuarios.
        // =====================================================

        private const string PoliticaContrasena = @"^(?=.*[A-Z])(?=.*\d).{6,}$";
        private const string MensajePoliticaContrasena = "La contraseña debe tener 6+ caracteres, 1 mayúscula y 1 número.";

        // VARBINARY(256) con AES-CBC/PKCS7: el texto cifrado ocupa un múltiplo de 16 bytes.
        private const int MaxBytesDireccionCifrada = 256;
        public const int MaxCaracteresDireccion = 100;

        private readonly DAL.UsuarioDAL _usuarioDal = new DAL.UsuarioDAL();

        // Épocas de sesión: cada login recibe un número creciente; revocar a un usuario
        // anota la época actual y toda sesión suya iniciada antes deja de ser válida.
        private static long _epoca;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, long> _revocaciones =
            new System.Collections.Concurrent.ConcurrentDictionary<int, long>();

        /// <summary>Número de época para una sesión que se está iniciando.</summary>
        public static long NuevaEpocaDeSesion() => Interlocked.Increment(ref _epoca);

        private static void RevocarSesionesDe(int usuarioId, string nombreUsuario)
        {
            _revocaciones[usuarioId] = Interlocked.Increment(ref _epoca);
            BE.SessionRegistry.Instancia.ForzarCierre(nombreUsuario);
        }

        /// <summary>true si las sesiones de este usuario fueron revocadas después de iniciada la suya.</summary>
        public static bool SesionRevocada(BE.USUARIO usuario) =>
            _revocaciones.TryGetValue(usuario.Id, out long revocadaEn) && revocadaEn > usuario.EpocaSesion;

        private static HashSet<string> PermisosDelActor(EstadoPerfiles estado, BE.USUARIO actor)
        {
            if (actor == null) throw new InvalidOperationException("No hay un usuario que realice la operación.");
            // El actor se verifica contra la base, no contra lo que dice su sesión:
            // un usuario dado de baja, bloqueado o inexistente no puede operar.
            if (!estado.Existentes.Contains(actor.Id) || estado.Bloqueados.Contains(actor.Id))
                throw new InvalidOperationException("Tu cuenta no está habilitada para realizar esta operación.");
            return PermisosEfectivosPorUsuario(estado).TryGetValue(actor.Id, out HashSet<string> permisos)
                ? permisos : new HashSet<string>();
        }

        // Operaciones sobre perfiles: exigen "Gestionar perfiles" en la BLL, no solo en la página.
        private static HashSet<string> ExigirGestorDePerfiles(EstadoPerfiles estado, BE.USUARIO actor)
        {
            HashSet<string> permisos = PermisosDelActor(estado, actor);
            if (!permisos.Contains(Permisos.GestionarPerfiles))
                throw new InvalidOperationException("No tenés permiso para gestionar perfiles.");
            return permisos;
        }

        // Regla de escalada: ¿el actor cubre estos permisos? Con "Gestionar perfiles" alcanza
        // con poseer los privilegiados involucrados (el resto lo administra libremente, así
        // Administrador sigue gestionando alumnos aunque no tenga sus permisos). Sin él, el
        // actor debe poseer todos.
        private static bool Cubre(HashSet<string> permisosActor, IEnumerable<string> permisosObjetivo)
        {
            if (permisosActor.Contains(Permisos.GestionarPerfiles))
                permisosObjetivo = permisosObjetivo.Where(Permisos.EsPrivilegiado);
            return permisosActor.IsSupersetOf(permisosObjetivo);
        }

        // Permisos efectivos del perfil (incluye los de sus perfiles hijos).
        private static HashSet<string> PermisosDeRol(EstadoPerfiles estado, int rolId) =>
            ConstruirMapaDeEstado(estado).TryGetValue(rolId, out Rol rol)
                ? new HashSet<string>(rol.ObtenerPermisos()) : new HashSet<string>();

        // Permisos del actor para las operaciones de gestión de usuarios: además exige "Gestionar usuarios".
        private static HashSet<string> PermisosDelGestor(EstadoPerfiles estado, BE.USUARIO actor)
        {
            HashSet<string> permisos = PermisosDelActor(estado, actor);
            if (!permisos.Contains(Permisos.GestionarUsuarios))
                throw new InvalidOperationException("No tenés permiso para gestionar usuarios.");
            return permisos;
        }

        // Lecturas de usuarios y perfiles asignables: las usan Usuarios.aspx (Gestionar usuarios)
        // y AsignarPerfiles.aspx (Gestionar perfiles), así que alcanza con cualquiera de los dos.
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

        // IDs de los perfiles que el actor puede asignar o quitar.
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
                    // Mismo mensaje que para un perfil inexistente: no se revela que existe.
                    throw new InvalidOperationException("Se indicó un perfil que no existe.");
                }
        }

        private static bool PuedeActuarSobre(EstadoPerfiles estado, HashSet<string> permisosActor, int usuarioId)
        {
            // No hay excepción por perfil protegido: los permisos de un perfil protegido ya
            // están en los efectivos del usuario, así que quien no los posee (o, si gestiona
            // perfiles, no posee los privilegiados) queda fuera, y quien sí los posee no
            // escala nada al actuar sobre alguien con esos mismos permisos.
            return Cubre(permisosActor, PermisosDeUsuario(estado, usuarioId));
        }

        // Un usuario que el actor no puede ver se trata igual que uno inexistente: el mismo
        // mensaje, para que los errores no revelen que existe.
        private const string MensajeUsuarioNoExiste = "El usuario no existe.";

        private static void ExigirPuedeActuarSobre(EstadoPerfiles estado, HashSet<string> permisosActor, int usuarioId)
        {
            if (!estado.Existentes.Contains(usuarioId) || !PuedeActuarSobre(estado, permisosActor, usuarioId))
                throw new InvalidOperationException(MensajeUsuarioNoExiste);
        }

        /// <summary>Perfiles que el actor puede asignar (para mostrar solo esos en pantalla).</summary>
        public List<int> ListarRolesAsignablesPor(BE.USUARIO actor)
        {
            EstadoPerfiles estado = CargarEstado();
            return RolesAsignables(estado, PermisosDeLectorDeUsuarios(estado, actor));
        }

        /// <summary>IDs de los usuarios sobre los que el actor puede operar (para mostrar solo esas acciones).</summary>
        public HashSet<int> ListarUsuariosActuablesPor(BE.USUARIO actor)
        {
            EstadoPerfiles estado = CargarEstado();
            HashSet<string> permisos = PermisosDeLectorDeUsuarios(estado, actor);
            return new HashSet<int>(_usuarioDal.ListarTodos().Select(u => u.Id)
                .Where(id => PuedeActuarSobre(estado, permisos, id)));
        }

        /// <summary>
        /// Usuarios para la grilla de gestión, con sus perfiles y estado. Solo incluye los
        /// que el actor puede ver (los mismos sobre los que puede operar): los demás no
        /// aparecen, así no se revela que existen.
        /// </summary>
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

        /// <summary>
        /// Misma lista que <see cref="ListarUsuariosVisiblesPor(BE.USUARIO)"/>, paginada. Se pagina en memoria
        /// y DESPUÉS de filtrar por visibilidad: esa regla vive en el Composite (C#), no en SQL. Paginar
        /// antes en la base daría páginas incompletas y un total que delataría a los usuarios ocultos.
        /// </summary>
        public BE.PaginaResultado<BE.USUARIO> ListarUsuariosVisiblesPor(BE.USUARIO actor, int pagina, int tamanio)
        {
            List<BE.USUARIO> visibles = ListarUsuariosVisiblesPor(actor).OrderBy(u => u.Usuario).ToList();
            if (tamanio < 1) tamanio = 10;
            var resultado = new BE.PaginaResultado<BE.USUARIO> { TotalFilas = visibles.Count, Tamanio = tamanio };
            resultado.Pagina = Math.Max(1, Math.Min(pagina, resultado.TotalPaginas));
            resultado.Filas = visibles.Skip((resultado.Pagina - 1) * tamanio).Take(tamanio).ToList();
            return resultado;
        }

        // Lecturas por usuario: exigen que el actor pueda ver al usuario (mismo error que si no existiera).
        private BE.USUARIO ExigirVisible(int usuarioId, BE.USUARIO actor, bool soloGestorDeUsuarios, out EstadoPerfiles estado)
        {
            estado = CargarEstado();
            HashSet<string> permisos = soloGestorDeUsuarios
                ? PermisosDelGestor(estado, actor) : PermisosDeLectorDeUsuarios(estado, actor);
            BE.USUARIO u = BuscarUsuario(_usuarioDal.ListarTodos(), usuarioId);
            ExigirPuedeActuarSobre(estado, permisos, usuarioId);
            return u;
        }

        /// <summary>IDs de los perfiles asignados a un usuario que el actor puede ver.</summary>
        public List<int> ObtenerIdsRolesDeUsuarioPor(int usuarioId, BE.USUARIO actor)
        {
            ExigirVisible(usuarioId, actor, false, out _);
            return _dal.ListarRolesDeUsuario(usuarioId);
        }

        /// <summary>Permisos efectivos de un usuario que el actor puede ver.</summary>
        public List<string> ObtenerPermisosEfectivosPor(int usuarioId, BE.USUARIO actor)
        {
            ExigirVisible(usuarioId, actor, false, out _);
            return ObtenerPermisosEfectivos(usuarioId);
        }

        /// <summary>Dirección ya desencriptada de un usuario que el actor puede ver (null si no tiene).</summary>
        public string ObtenerDireccion(string usuario, BE.USUARIO actor)
        {
            // La dirección solo la ve quien gestiona usuarios (Usuarios.aspx), nunca un lector cualquiera.
            PermisosDelGestor(CargarEstado(), actor);
            BE.USUARIO u = _usuarioDal.ListarTodos().FirstOrDefault(x =>
                string.Equals(x.Usuario, usuario, StringComparison.OrdinalIgnoreCase));
            if (u == null) throw new InvalidOperationException(MensajeUsuarioNoExiste);
            ExigirVisible(u.Id, actor, true, out _);
            return _usuarioDal.ObtenerPorNombre(u.Usuario)?.DireccionVisible;
        }

        // Desbloqueo: exige "Desbloquear usuarios" y que el actor pueda ver al usuario.
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

        /// <summary>Usuarios bloqueados que el actor puede ver (y por lo tanto desbloquear).</summary>
        public List<BE.USUARIO> ListarBloqueadosVisiblesPor(BE.USUARIO actor)
        {
            EstadoPerfiles estado = CargarEstado();
            HashSet<string> permisos = PermisosDelActor(estado, actor);
            if (!permisos.Contains(Permisos.DesbloquearUsuarios))
                throw new InvalidOperationException("No tenés permiso para desbloquear usuarios.");
            return _usuarioDal.ListarBloqueados().Where(u => PuedeActuarSobre(estado, permisos, u.Id)).ToList();
        }

        /// <summary>Desbloquea una cuenta bloqueada por intentos fallidos.</summary>
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

        // La dirección se guarda cifrada en VARBINARY(256): se valida el tamaño real del cifrado.
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

        /// <summary>
        /// Modifica perfiles y dirección (dirección vacía = sin cambios). Todo se valida
        /// antes de escribir; si después de guardar los perfiles falla la dirección, el
        /// mensaje lo dice con precisión.
        /// </summary>
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

        /// <summary>
        /// Baja lógica (activo = false) o reactivación. Nadie se da de baja a sí mismo ni
        /// deja al sistema sin operadores. Al dar de baja se revocan sus sesiones.
        /// </summary>
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
                    estado.Bloqueados.Add(usuarioId); // simulación: deja de ser operador
                    ExigirOperadorDePerfiles(estado);
                }

                _usuarioDal.ActualizarEstado(usuarioId, activo);
                if (!activo) RevocarSesionesDe(usuarioId, u.Usuario);
                PostEscritura(actor.Usuario, Accion(activo ? "REACTIVAR_USUARIO" : "BAJA_USUARIO", u.Usuario),
                    _integridad.RecalcularUsuario);
            }
        }

        /// <summary>
        /// Restablece la contraseña de otro usuario. Cierra sus sesiones abiertas: si la
        /// cuenta estaba comprometida, la sesión del atacante deja de valer.
        /// </summary>
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
