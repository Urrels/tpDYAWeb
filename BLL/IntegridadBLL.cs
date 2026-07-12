using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace BLL
{
    /// <summary>
    /// Resultado consolidado de una verificación de integridad (DVH/DVV).
    /// <see cref="IdsUsuariosAfectados"/> solo se completa para errores
    /// detectados en la tabla USUARIO (es el único caso donde tiene sentido
    /// identificar a un usuario puntual afectado).
    /// </summary>
    public class ResultadoIntegridad
    {
        public bool EsValido { get; set; } = true;
        public List<string> Errores { get; set; } = new List<string>();
        public List<int> IdsUsuariosAfectados { get; set; } = new List<int>();
    }

    /// <summary>
    /// Motor genérico de dígitos verificadores horizontal (DVH, por fila) y
    /// vertical (DVV, por columna) para las 9 tablas de negocio del sistema.
    /// Patrón portado del TP hermano "ingenieria_software" (ya aprobado por
    /// el docente): <see cref="BE.CalculadorDVH"/> hace el cálculo puro,
    /// esta clase orquesta lectura/escritura vía <see cref="DAL.IntegridadDAL"/>.
    /// </summary>
    public class IntegridadBLL
    {
        private readonly DAL.IntegridadDAL _dal = new DAL.IntegridadDAL();

        /// <summary>
        /// Snapshot de una fila en el último estado válido conocido de una
        /// tabla — se serializa como JSON (lista de estas) y se guarda en
        /// SNAPSHOT_INTEGRIDAD cada vez que se recalcula la tabla. Permite
        /// diagnosticar campo a campo qué cambió cuando se detecta una
        /// alteración, en vez de solo saber que "algo" está mal.
        /// </summary>
        private class FilaSnapshot
        {
            public int Id { get; set; }
            public string[] Atributos { get; set; }
        }

        // Orden canónico de atributos por tabla — fijo, no debe cambiar una
        // vez que haya datos guardados (cambiar el orden invalida todos los
        // DVH/DVV ya calculados).
        private static readonly string[] ColumnasUsuario =
            { "ID", "USUARIO", "PASS", "DIRECCION", "ROL", "INTENTOS_FALLIDOS", "BLOQUEADO" };

        private static readonly string[] ColumnasBitacora =
            { "ID", "USUARIO", "ACCION", "FECHA", "CRITICIDAD" };

        private static readonly string[] ColumnasMateria =
            { "ID", "NOMBRE", "CODIGO", "MODALIDAD", "PESO", "ACTIVA" };

        private static readonly string[] ColumnasCorrelativa =
            { "ID", "ID_MATERIA", "ID_CORRELATIVA" };

        private static readonly string[] ColumnasAlumnoMateria =
            { "ID", "ID_USUARIO", "ID_MATERIA", "ESTADO",
              "NOTA_PARCIAL1", "NOTA_PARCIAL2", "NOTA_RECUPERATORIO", "NOTA_FINAL",
              "FECHA_FINAL", "FECHA_RECUPERATORIO" };

        private static readonly string[] ColumnasEventoAcademico =
            { "ID", "ID_MATERIA", "ID_USUARIO", "TIPO", "DESCRIPCION", "FECHA", "PESO" };

        private static readonly string[] ColumnasPeriodoAcademico =
            { "ID", "ANIO", "CUATRIMESTRE", "DESCRIPCION", "FECHA_INICIO", "FECHA_FIN" };

        private static readonly string[] ColumnasInscripcion =
            { "ID", "ID_USUARIO", "ID_PERIODO", "FECHA_INSCRIPCION" };

        private static readonly string[] ColumnasInscripcionDetalle =
            { "ID", "ID_INSCRIPCION", "ID_MATERIA" };

        // =====================================================
        // Motor genérico
        // =====================================================

        private void VerificarTabla(string nombreTabla, string[] columnas, DataTable datos,
            Func<DataRow, string[]> extraerAtributos, ResultadoIntegridad resultado)
        {
            var todasFilas = new List<string[]>();
            bool huboErrorDvh = false;

            foreach (DataRow fila in datos.Rows)
            {
                string[] atributos = extraerAtributos(fila);
                todasFilas.Add(atributos);

                int id = Convert.ToInt32(fila["ID"]);
                int dvhCalculado = BE.CalculadorDVH.Calcular(atributos);
                int dvhGuardado = Convert.ToInt32(fila["DVH"]);

                if (dvhCalculado != dvhGuardado)
                {
                    // No se agrega el mensaje acá todavía — se difiere a
                    // DiagnosticarDiferencias() al final, que compara contra
                    // el snapshot y puede decir exactamente qué campo cambió
                    // en vez de solo "el DVH no coincide".
                    resultado.EsValido = false;
                    huboErrorDvh = true;
                    if (nombreTabla == "USUARIO")
                        resultado.IdsUsuariosAfectados.Add(id);
                }
            }

            var dvvGuardados = _dal.ObtenerDVV(nombreTabla);

            if (datos.Rows.Count == 0 && dvvGuardados.Count == 0)
                return; // tabla vacía, nada que verificar

            if (datos.Rows.Count > 0 && dvvGuardados.Count == 0)
            {
                resultado.EsValido = false;
                resultado.Errores.Add($"{nombreTabla}: dígitos verificadores verticales ausentes.");
                return;
            }

            for (int c = 0; c < columnas.Length; c++)
            {
                int dvvCalculado = BE.CalculadorDVH.CalcularVertical(todasFilas, c);
                if (!dvvGuardados.TryGetValue(columnas[c], out int dvvGuardado))
                {
                    resultado.EsValido = false;
                    resultado.Errores.Add($"{nombreTabla}: dígito verificador vertical ausente para la columna {columnas[c]}.");
                    continue;
                }
                if (dvvCalculado != dvvGuardado)
                {
                    resultado.EsValido = false;
                    resultado.Errores.Add($"{nombreTabla}: DVV inválido en la columna {columnas[c]} " +
                        $"(esperado {dvvCalculado}, guardado {dvvGuardado}).");
                }
            }

            if (!resultado.EsValido)
            {
                int erroresAntes = resultado.Errores.Count;
                DiagnosticarDiferencias(nombreTabla, columnas, datos, extraerAtributos, resultado);

                // Si hubo una fila con DVH inválido y el diagnóstico contra el
                // snapshot no agregó ningún mensaje explicativo (porque no hay
                // snapshot todavía, o porque el propio DVH fue alterado
                // directamente sin tocar ningún campo), no dejamos Errores sin
                // una pista de qué tabla/motivo está fallando.
                if (huboErrorDvh && resultado.Errores.Count == erroresAntes)
                {
                    resultado.Errores.Add($"{nombreTabla}: hay al menos un registro con DVH inválido, pero no se " +
                        $"pudo identificar el campo alterado (dígitos verificadores ausentes o no inicializados " +
                        $"para la tabla, o el propio DVH fue modificado directamente). Recalculá la integridad de " +
                        $"la tabla para refinar el diagnóstico.");
                }
            }
        }

        private void RecalcularTabla(string nombreTabla, string[] columnas, DataTable datos,
            Func<DataRow, string[]> extraerAtributos, Action<int, int> actualizarDVH)
        {
            var todasFilas = new List<string[]>();

            foreach (DataRow fila in datos.Rows)
            {
                string[] atributos = extraerAtributos(fila);
                todasFilas.Add(atributos);

                int id = Convert.ToInt32(fila["ID"]);
                int dvh = BE.CalculadorDVH.Calcular(atributos);
                actualizarDVH(id, dvh);
            }

            for (int c = 0; c < columnas.Length; c++)
            {
                int dvv = BE.CalculadorDVH.CalcularVertical(todasFilas, c);
                _dal.ActualizarDVV(nombreTabla, columnas[c], dvv);
            }

            // El estado que acabamos de recalcular es, por definición, el
            // último estado válido conocido — se guarda como snapshot para
            // que la próxima verificación pueda diagnosticar campo a campo.
            GuardarSnapshot(nombreTabla, datos, extraerAtributos);
        }

        /// <summary>
        /// Compara el snapshot del último estado válido conocido de la tabla
        /// contra los datos actuales y agrega a <paramref name="resultado"/>
        /// un mensaje preciso por cada alta, baja o modificación de campo
        /// detectada. Solo tiene sentido llamarlo cuando VerificarTabla ya
        /// determinó que la tabla no es válida (no se paga este costo en el
        /// camino feliz). Si todavía no existe snapshot para la tabla (nunca
        /// se recalculó), no agrega nada — no hay nada contra qué comparar.
        /// </summary>
        private void DiagnosticarDiferencias(string nombreTabla, string[] columnas, DataTable datosActuales,
            Func<DataRow, string[]> extraerAtributos, ResultadoIntegridad resultado)
        {
            List<FilaSnapshot> snapshot = ObtenerSnapshot(nombreTabla);
            if (snapshot.Count == 0)
                return; // tabla nunca recalculada todavía: no hay snapshot con qué comparar

            var porIdSnapshot = new Dictionary<int, string[]>();
            foreach (var f in snapshot)
                porIdSnapshot[f.Id] = f.Atributos;

            var porIdActual = new Dictionary<int, string[]>();
            foreach (DataRow fila in datosActuales.Rows)
                porIdActual[Convert.ToInt32(fila["ID"])] = extraerAtributos(fila);

            foreach (var id in porIdActual.Keys)
            {
                if (!porIdSnapshot.ContainsKey(id))
                    resultado.Errores.Add($"{nombreTabla}: se agregó el registro {id} " +
                        "(no estaba en el último estado válido conocido).");
            }

            foreach (var id in porIdSnapshot.Keys)
            {
                if (!porIdActual.ContainsKey(id))
                    resultado.Errores.Add($"{nombreTabla}: se eliminó el registro {id}.");
            }

            foreach (var id in porIdActual.Keys)
            {
                if (!porIdSnapshot.TryGetValue(id, out string[] antes))
                    continue; // ya reportado arriba como alta

                string[] actual = porIdActual[id];
                int largo = Math.Min(columnas.Length, Math.Min(antes.Length, actual.Length));
                for (int i = 0; i < largo; i++)
                {
                    if (antes[i] != actual[i])
                        resultado.Errores.Add($"{nombreTabla}: se modificó el campo {columnas[i]} del registro {id} " +
                            $"(antes: '{antes[i]}', ahora: '{actual[i]}').");
                }
            }
        }

        /// <summary>
        /// Guarda el estado recién confirmado como bueno (recién recalculado)
        /// como snapshot de referencia para el próximo diagnóstico.
        /// </summary>
        private void GuardarSnapshot(string nombreTabla, DataTable datos, Func<DataRow, string[]> extraerAtributos)
        {
            var snapshot = new List<FilaSnapshot>();
            foreach (DataRow fila in datos.Rows)
                snapshot.Add(new FilaSnapshot { Id = Convert.ToInt32(fila["ID"]), Atributos = extraerAtributos(fila) });

            string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(snapshot);
            _dal.GuardarSnapshot(nombreTabla, json);
        }

        /// <summary>
        /// Snapshot del último estado válido conocido de la tabla, o lista
        /// vacía si todavía no se guardó ninguno.
        /// </summary>
        private List<FilaSnapshot> ObtenerSnapshot(string nombreTabla)
        {
            string json = _dal.ObtenerSnapshot(nombreTabla);
            if (string.IsNullOrEmpty(json))
                return new List<FilaSnapshot>();
            return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<List<FilaSnapshot>>(json);
        }

        // =====================================================
        // Conversión determinística de valores de columna a string
        // (misma regla siempre — necesaria para que el DVH/DVV sea estable)
        // =====================================================

        private static string StrStr(object valor) =>
            valor == null || valor == DBNull.Value ? string.Empty : valor.ToString();

        private static string IntStr(object valor) =>
            valor == null || valor == DBNull.Value ? string.Empty : Convert.ToInt32(valor).ToString(CultureInfo.InvariantCulture);

        private static string DecStr(object valor) =>
            valor == null || valor == DBNull.Value ? string.Empty : Convert.ToDecimal(valor).ToString(CultureInfo.InvariantCulture);

        private static string DateStr(object valor) =>
            valor == null || valor == DBNull.Value ? string.Empty : Convert.ToDateTime(valor).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        private static string BitStr(object valor) =>
            valor == null || valor == DBNull.Value ? string.Empty : (Convert.ToBoolean(valor) ? "1" : "0");

        private static string Base64Str(object valor) =>
            valor == null || valor == DBNull.Value ? string.Empty : Convert.ToBase64String((byte[])valor);

        // =====================================================
        // Extractores de atributos por tabla (mismo orden que ColumnasX)
        // =====================================================

        private static string[] ExtraerUsuario(DataRow r) => new[]
        {
            IntStr(r["ID"]), StrStr(r["USUARIO"]), StrStr(r["PASS"]), Base64Str(r["DIRECCION"]), StrStr(r["ROL"]),
            IntStr(r["INTENTOS_FALLIDOS"]), BitStr(r["BLOQUEADO"])
        };

        private static string[] ExtraerBitacora(DataRow r) => new[]
        {
            IntStr(r["ID"]), StrStr(r["USUARIO"]), StrStr(r["ACCION"]), DateStr(r["FECHA"]), StrStr(r["CRITICIDAD"])
        };

        private static string[] ExtraerMateria(DataRow r) => new[]
        {
            IntStr(r["ID"]), StrStr(r["NOMBRE"]), StrStr(r["CODIGO"]), StrStr(r["MODALIDAD"]), IntStr(r["PESO"]), BitStr(r["ACTIVA"])
        };

        private static string[] ExtraerCorrelativa(DataRow r) => new[]
        {
            IntStr(r["ID"]), IntStr(r["ID_MATERIA"]), IntStr(r["ID_CORRELATIVA"])
        };

        private static string[] ExtraerAlumnoMateria(DataRow r) => new[]
        {
            IntStr(r["ID"]), IntStr(r["ID_USUARIO"]), IntStr(r["ID_MATERIA"]), StrStr(r["ESTADO"]),
            DecStr(r["NOTA_PARCIAL1"]), DecStr(r["NOTA_PARCIAL2"]), DecStr(r["NOTA_RECUPERATORIO"]), DecStr(r["NOTA_FINAL"]),
            DateStr(r["FECHA_FINAL"]), DateStr(r["FECHA_RECUPERATORIO"])
        };

        private static string[] ExtraerEventoAcademico(DataRow r) => new[]
        {
            IntStr(r["ID"]), IntStr(r["ID_MATERIA"]), IntStr(r["ID_USUARIO"]), StrStr(r["TIPO"]), StrStr(r["DESCRIPCION"]), DateStr(r["FECHA"]), IntStr(r["PESO"])
        };

        private static string[] ExtraerPeriodoAcademico(DataRow r) => new[]
        {
            IntStr(r["ID"]), IntStr(r["ANIO"]), IntStr(r["CUATRIMESTRE"]), StrStr(r["DESCRIPCION"]), DateStr(r["FECHA_INICIO"]), DateStr(r["FECHA_FIN"])
        };

        private static string[] ExtraerInscripcion(DataRow r) => new[]
        {
            IntStr(r["ID"]), IntStr(r["ID_USUARIO"]), IntStr(r["ID_PERIODO"]), DateStr(r["FECHA_INSCRIPCION"])
        };

        private static string[] ExtraerInscripcionDetalle(DataRow r) => new[]
        {
            IntStr(r["ID"]), IntStr(r["ID_INSCRIPCION"]), IntStr(r["ID_MATERIA"])
        };

        // =====================================================
        // Wrappers públicos por tabla
        // =====================================================

        public ResultadoIntegridad VerificarUsuario()
        {
            var resultado = new ResultadoIntegridad();
            VerificarTabla("USUARIO", ColumnasUsuario, _dal.ListarParaIntegridad("USUARIO"), ExtraerUsuario, resultado);
            return resultado;
        }
        public void RecalcularUsuario()
        {
            RecalcularTabla("USUARIO", ColumnasUsuario, _dal.ListarParaIntegridad("USUARIO"), ExtraerUsuario,
                (id, dvh) => _dal.ActualizarDVH("USUARIO", id, dvh));
        }

        public ResultadoIntegridad VerificarBitacora()
        {
            var resultado = new ResultadoIntegridad();
            VerificarTabla("BITACORA", ColumnasBitacora, _dal.ListarParaIntegridad("BITACORA"), ExtraerBitacora, resultado);
            return resultado;
        }
        public void RecalcularBitacora()
        {
            RecalcularTabla("BITACORA", ColumnasBitacora, _dal.ListarParaIntegridad("BITACORA"), ExtraerBitacora,
                (id, dvh) => _dal.ActualizarDVH("BITACORA", id, dvh));
        }

        public ResultadoIntegridad VerificarMateria()
        {
            var resultado = new ResultadoIntegridad();
            VerificarTabla("MATERIA", ColumnasMateria, _dal.ListarParaIntegridad("MATERIA"), ExtraerMateria, resultado);
            return resultado;
        }
        public void RecalcularMateria()
        {
            RecalcularTabla("MATERIA", ColumnasMateria, _dal.ListarParaIntegridad("MATERIA"), ExtraerMateria,
                (id, dvh) => _dal.ActualizarDVH("MATERIA", id, dvh));
        }

        public ResultadoIntegridad VerificarCorrelativa()
        {
            var resultado = new ResultadoIntegridad();
            VerificarTabla("CORRELATIVA", ColumnasCorrelativa, _dal.ListarParaIntegridad("CORRELATIVA"), ExtraerCorrelativa, resultado);
            return resultado;
        }
        public void RecalcularCorrelativa()
        {
            RecalcularTabla("CORRELATIVA", ColumnasCorrelativa, _dal.ListarParaIntegridad("CORRELATIVA"), ExtraerCorrelativa,
                (id, dvh) => _dal.ActualizarDVH("CORRELATIVA", id, dvh));
        }

        /// <summary>
        /// Calcula el DVH de una fila de ALUMNO_MATERIA a partir del objeto de
        /// negocio (no de un DataRow) — usado por AlumnoMateriaBLL.ActualizarNotas()
        /// para fijar am.DVH ANTES de escribir la fila (ALUMNO_MATERIA_ACTUALIZAR
        /// recibe @dvh como parámetro). Debe producir exactamente el mismo
        /// resultado que ExtraerAlumnoMateria + ColumnasAlumnoMateria aplicado
        /// sobre la misma fila ya guardada, por eso usa las mismas reglas de
        /// conversión determinística (invariant culture, mismo formato de fecha).
        /// </summary>
        public int CalcularDVHAlumnoMateria(BE.ALUMNO_MATERIA am)
        {
            string[] atributos =
            {
                am.Id.ToString(CultureInfo.InvariantCulture),
                am.IdUsuario.ToString(CultureInfo.InvariantCulture),
                am.IdMateria.ToString(CultureInfo.InvariantCulture),
                am.Estado ?? string.Empty,
                am.NotaParcial1.HasValue ? am.NotaParcial1.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                am.NotaParcial2.HasValue ? am.NotaParcial2.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                am.NotaRecuperatorio.HasValue ? am.NotaRecuperatorio.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                am.NotaFinal.HasValue ? am.NotaFinal.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                am.FechaFinal.HasValue ? am.FechaFinal.Value.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) : string.Empty,
                am.FechaRecuperatorio.HasValue ? am.FechaRecuperatorio.Value.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) : string.Empty
            };
            return BE.CalculadorDVH.Calcular(atributos);
        }

        public ResultadoIntegridad VerificarAlumnoMateria()
        {
            var resultado = new ResultadoIntegridad();
            VerificarTabla("ALUMNO_MATERIA", ColumnasAlumnoMateria, _dal.ListarParaIntegridad("ALUMNO_MATERIA"), ExtraerAlumnoMateria, resultado);
            return resultado;
        }
        public void RecalcularAlumnoMateria()
        {
            RecalcularTabla("ALUMNO_MATERIA", ColumnasAlumnoMateria, _dal.ListarParaIntegridad("ALUMNO_MATERIA"), ExtraerAlumnoMateria,
                (id, dvh) => _dal.ActualizarDVH("ALUMNO_MATERIA", id, dvh));
        }

        public ResultadoIntegridad VerificarEventoAcademico()
        {
            var resultado = new ResultadoIntegridad();
            VerificarTabla("EVENTO_ACADEMICO", ColumnasEventoAcademico, _dal.ListarParaIntegridad("EVENTO_ACADEMICO"), ExtraerEventoAcademico, resultado);
            return resultado;
        }
        public void RecalcularEventoAcademico()
        {
            RecalcularTabla("EVENTO_ACADEMICO", ColumnasEventoAcademico, _dal.ListarParaIntegridad("EVENTO_ACADEMICO"), ExtraerEventoAcademico,
                (id, dvh) => _dal.ActualizarDVH("EVENTO_ACADEMICO", id, dvh));
        }

        public ResultadoIntegridad VerificarPeriodoAcademico()
        {
            var resultado = new ResultadoIntegridad();
            VerificarTabla("PERIODO_ACADEMICO", ColumnasPeriodoAcademico, _dal.ListarParaIntegridad("PERIODO_ACADEMICO"), ExtraerPeriodoAcademico, resultado);
            return resultado;
        }
        public void RecalcularPeriodoAcademico()
        {
            RecalcularTabla("PERIODO_ACADEMICO", ColumnasPeriodoAcademico, _dal.ListarParaIntegridad("PERIODO_ACADEMICO"), ExtraerPeriodoAcademico,
                (id, dvh) => _dal.ActualizarDVH("PERIODO_ACADEMICO", id, dvh));
        }

        public ResultadoIntegridad VerificarInscripcion()
        {
            var resultado = new ResultadoIntegridad();
            VerificarTabla("INSCRIPCION", ColumnasInscripcion, _dal.ListarParaIntegridad("INSCRIPCION"), ExtraerInscripcion, resultado);
            return resultado;
        }
        public void RecalcularInscripcion()
        {
            RecalcularTabla("INSCRIPCION", ColumnasInscripcion, _dal.ListarParaIntegridad("INSCRIPCION"), ExtraerInscripcion,
                (id, dvh) => _dal.ActualizarDVH("INSCRIPCION", id, dvh));
        }

        public ResultadoIntegridad VerificarInscripcionDetalle()
        {
            var resultado = new ResultadoIntegridad();
            VerificarTabla("INSCRIPCION_DETALLE", ColumnasInscripcionDetalle, _dal.ListarParaIntegridad("INSCRIPCION_DETALLE"), ExtraerInscripcionDetalle, resultado);
            return resultado;
        }
        public void RecalcularInscripcionDetalle()
        {
            RecalcularTabla("INSCRIPCION_DETALLE", ColumnasInscripcionDetalle, _dal.ListarParaIntegridad("INSCRIPCION_DETALLE"), ExtraerInscripcionDetalle,
                (id, dvh) => _dal.ActualizarDVH("INSCRIPCION_DETALLE", id, dvh));
        }

        // =====================================================
        // Agregados — para el panel Webmaster
        // =====================================================

        public ResultadoIntegridad VerificarTodasLasTablas()
        {
            var consolidado = new ResultadoIntegridad();
            var parciales = new[]
            {
                VerificarUsuario(), VerificarBitacora(), VerificarMateria(), VerificarCorrelativa(),
                VerificarAlumnoMateria(), VerificarEventoAcademico(), VerificarPeriodoAcademico(),
                VerificarInscripcion(), VerificarInscripcionDetalle()
            };

            foreach (var parcial in parciales)
            {
                if (!parcial.EsValido) consolidado.EsValido = false;
                consolidado.Errores.AddRange(parcial.Errores);
                consolidado.IdsUsuariosAfectados.AddRange(parcial.IdsUsuariosAfectados);
            }
            return consolidado;
        }

        /// <summary>
        /// No pedido explícitamente en el patrón de referencia, pero necesario
        /// para poblar por primera vez DIGITO_VERIFICADOR_VERTICAL luego de
        /// instalar este mecanismo sobre una base ya cargada con datos (de lo
        /// contrario VerificarTodasLasTablas() reporta "ausentes" para las 9
        /// tablas hasta que se recalculen al menos una vez).
        /// </summary>
        public void RecalcularTodasLasTablas()
        {
            RecalcularUsuario();
            RecalcularBitacora();
            RecalcularMateria();
            RecalcularCorrelativa();
            RecalcularAlumnoMateria();
            RecalcularEventoAcademico();
            RecalcularPeriodoAcademico();
            RecalcularInscripcion();
            RecalcularInscripcionDetalle();
        }

        // =====================================================
        // Restore — devuelve las 9 tablas al último estado guardado en
        // SNAPSHOT_INTEGRIDAD. Dos fases (ver detalle en cada método) para
        // no violar ninguna Foreign Key entre tablas.
        // =====================================================

        /// <summary>
        /// Restaura las 9 tablas de negocio al último estado guardado en el
        /// snapshot de integridad: filas agregadas externamente se borran,
        /// filas eliminadas externamente se re-insertan (con su ID original,
        /// vía IDENTITY_INSERT) y filas modificadas vuelven a los valores del
        /// snapshot. Se hace en dos fases para no violar ninguna FK:
        ///   1) Borrado, en orden hijo→padre (inverso a la dependencia FK).
        ///   2) Upsert (modificados + eliminados que vuelven), en orden
        ///      padre→hijo.
        /// Al final se recalculan DVH/DVV de las 9 tablas (y se regenera el
        /// snapshot) para que queden consistentes con el estado restaurado.
        ///
        /// LIMITACIÓN CONOCIDA Y ACEPTADA: cada llamada a <see cref="DAL.IntegridadDAL"/>
        /// abre y cierra su propia conexión (patrón Acceso actual: abrir →
        /// ejecutar → cerrar en finally, por llamada). No hay una única
        /// transacción SQL que envuelva las 9 tablas de punta a punta —
        /// hacerlo sería un cambio arquitectural mayor, fuera de alcance de
        /// esta entrega. Si una tabla falla a mitad del restore, las tablas
        /// ya procesadas quedan restauradas y las restantes no: la base
        /// puede quedar parcialmente restaurada. No se resuelve ni se oculta
        /// acá; queda documentado como limitación conocida.
        /// </summary>
        public void RestaurarTodasLasTablas()
        {
            // Fase 1: borrado de filas agregadas externamente — hijo→padre.
            BorrarAgregadosExternamente("INSCRIPCION_DETALLE");
            BorrarAgregadosExternamente("INSCRIPCION");
            BorrarAgregadosExternamente("EVENTO_ACADEMICO");
            BorrarAgregadosExternamente("ALUMNO_MATERIA");
            BorrarAgregadosExternamente("CORRELATIVA");
            BorrarAgregadosExternamente("BITACORA");
            BorrarAgregadosExternamente("PERIODO_ACADEMICO");
            BorrarAgregadosExternamente("MATERIA");
            BorrarAgregadosExternamente("USUARIO");

            // Fase 2: upsert de modificados + eliminados que vuelven — padre→hijo.
            RestaurarUpsertUsuario();
            RestaurarUpsertMateria();
            RestaurarUpsertPeriodoAcademico();
            RestaurarUpsertBitacora();
            RestaurarUpsertCorrelativa();
            RestaurarUpsertAlumnoMateria();
            RestaurarUpsertEventoAcademico();
            RestaurarUpsertInscripcion();
            RestaurarUpsertInscripcionDetalle();

            // El estado restaurado debe quedar con DVH/DVV (y snapshot) al día.
            RecalcularTodasLasTablas();
        }

        /// <summary>
        /// Fase de borrado: cualquier ID presente en los datos actuales pero
        /// ausente del snapshot fue agregado externamente (por fuera del
        /// mecanismo normal de la aplicación) — se borra.
        /// </summary>
        private void BorrarAgregadosExternamente(string nombreTabla)
        {
            var idsSnapshot = new HashSet<int>();
            foreach (var f in ObtenerSnapshot(nombreTabla))
                idsSnapshot.Add(f.Id);

            DataTable actuales = _dal.ListarParaIntegridad(nombreTabla);
            foreach (DataRow fila in actuales.Rows)
            {
                int id = Convert.ToInt32(fila["ID"]);
                if (!idsSnapshot.Contains(id))
                    _dal.EliminarPorId(nombreTabla, id);
            }
        }

        // -----------------------------------------------------
        // Rehidratación — inversa de ExtraerX. Convierte cada string del
        // snapshot de vuelta a su tipo real antes de pasarlo como SqlParameter.
        // -----------------------------------------------------

        private static int ParseInt(string s) => int.Parse(s, CultureInfo.InvariantCulture);

        private static decimal? ParseDecNullable(string s) =>
            string.IsNullOrEmpty(s) ? (decimal?)null : decimal.Parse(s, CultureInfo.InvariantCulture);

        private static DateTime? ParseFechaNullable(string s) =>
            string.IsNullOrEmpty(s) ? (DateTime?)null : DateTime.ParseExact(s, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        private static bool ParseBit(string s) => s == "1";

        private static byte[] ParseBase64(string s) =>
            string.IsNullOrEmpty(s) ? null : Convert.FromBase64String(s);

        private static string ParseStrNullable(string s) => string.IsNullOrEmpty(s) ? null : s;

        // Objetos simples con los valores ya tipados de una fila del
        // snapshot, uno por tabla, mismo orden que ColumnasX/ExtraerX.

        private class FilaUsuarioRestaurada
        {
            public int Id;
            public string Usuario;
            public string Pass;
            public byte[] Direccion;
            public string Rol;
        }

        private class FilaBitacoraRestaurada
        {
            public int Id;
            public string Usuario;
            public string Accion;
            public DateTime Fecha;
        }

        private class FilaMateriaRestaurada
        {
            public int Id;
            public string Nombre;
            public string Codigo;
            public string Modalidad;
            public int Peso;
            public bool Activa;
        }

        private class FilaCorrelativaRestaurada
        {
            public int Id;
            public int IdMateria;
            public int IdCorrelativa;
        }

        private class FilaAlumnoMateriaRestaurada
        {
            public int Id;
            public int IdUsuario;
            public int IdMateria;
            public string Estado;
            public decimal? NotaParcial1;
            public decimal? NotaParcial2;
            public decimal? NotaRecuperatorio;
            public decimal? NotaFinal;
            public DateTime? FechaFinal;
            public DateTime? FechaRecuperatorio;
        }

        private class FilaEventoAcademicoRestaurada
        {
            public int Id;
            public int IdMateria;
            public int IdUsuario;
            public string Tipo;
            public string Descripcion;
            public DateTime Fecha;
            public int Peso;
        }

        private class FilaPeriodoAcademicoRestaurada
        {
            public int Id;
            public int Anio;
            public int Cuatrimestre;
            public string Descripcion;
            public DateTime? FechaInicio;
            public DateTime? FechaFin;
        }

        private class FilaInscripcionRestaurada
        {
            public int Id;
            public int IdUsuario;
            public int IdPeriodo;
            public DateTime FechaInscripcion;
        }

        private class FilaInscripcionDetalleRestaurada
        {
            public int Id;
            public int IdInscripcion;
            public int IdMateria;
        }

        private static FilaUsuarioRestaurada RehidratarUsuario(string[] a) => new FilaUsuarioRestaurada
        {
            Id = ParseInt(a[0]),
            Usuario = a[1],
            Pass = a[2],
            Direccion = ParseBase64(a[3]),
            Rol = a[4]
        };

        private static FilaBitacoraRestaurada RehidratarBitacora(string[] a) => new FilaBitacoraRestaurada
        {
            Id = ParseInt(a[0]),
            Usuario = a[1],
            Accion = a[2],
            Fecha = ParseFechaNullable(a[3]).Value
        };

        private static FilaMateriaRestaurada RehidratarMateria(string[] a) => new FilaMateriaRestaurada
        {
            Id = ParseInt(a[0]),
            Nombre = a[1],
            Codigo = a[2],
            Modalidad = a[3],
            Peso = ParseInt(a[4]),
            Activa = ParseBit(a[5])
        };

        private static FilaCorrelativaRestaurada RehidratarCorrelativa(string[] a) => new FilaCorrelativaRestaurada
        {
            Id = ParseInt(a[0]),
            IdMateria = ParseInt(a[1]),
            IdCorrelativa = ParseInt(a[2])
        };

        private static FilaAlumnoMateriaRestaurada RehidratarAlumnoMateria(string[] a) => new FilaAlumnoMateriaRestaurada
        {
            Id = ParseInt(a[0]),
            IdUsuario = ParseInt(a[1]),
            IdMateria = ParseInt(a[2]),
            Estado = a[3],
            NotaParcial1 = ParseDecNullable(a[4]),
            NotaParcial2 = ParseDecNullable(a[5]),
            NotaRecuperatorio = ParseDecNullable(a[6]),
            NotaFinal = ParseDecNullable(a[7]),
            FechaFinal = ParseFechaNullable(a[8]),
            FechaRecuperatorio = ParseFechaNullable(a[9])
        };

        private static FilaEventoAcademicoRestaurada RehidratarEventoAcademico(string[] a) => new FilaEventoAcademicoRestaurada
        {
            Id = ParseInt(a[0]),
            IdMateria = ParseInt(a[1]),
            IdUsuario = ParseInt(a[2]),
            Tipo = a[3],
            Descripcion = ParseStrNullable(a[4]),
            Fecha = ParseFechaNullable(a[5]).Value,
            Peso = ParseInt(a[6])
        };

        private static FilaPeriodoAcademicoRestaurada RehidratarPeriodoAcademico(string[] a) => new FilaPeriodoAcademicoRestaurada
        {
            Id = ParseInt(a[0]),
            Anio = ParseInt(a[1]),
            Cuatrimestre = ParseInt(a[2]),
            Descripcion = ParseStrNullable(a[3]),
            FechaInicio = ParseFechaNullable(a[4]),
            FechaFin = ParseFechaNullable(a[5])
        };

        private static FilaInscripcionRestaurada RehidratarInscripcion(string[] a) => new FilaInscripcionRestaurada
        {
            Id = ParseInt(a[0]),
            IdUsuario = ParseInt(a[1]),
            IdPeriodo = ParseInt(a[2]),
            FechaInscripcion = ParseFechaNullable(a[3]).Value
        };

        private static FilaInscripcionDetalleRestaurada RehidratarInscripcionDetalle(string[] a) => new FilaInscripcionDetalleRestaurada
        {
            Id = ParseInt(a[0]),
            IdInscripcion = ParseInt(a[1]),
            IdMateria = ParseInt(a[2])
        };

        // -----------------------------------------------------
        // Fase de upsert — un método por tabla (firma de columnas distinta
        // por tabla), llamado en orden padre→hijo desde RestaurarTodasLasTablas().
        // -----------------------------------------------------

        private void RestaurarUpsertUsuario()
        {
            foreach (var f in ObtenerSnapshot("USUARIO"))
            {
                var u = RehidratarUsuario(f.Atributos);
                _dal.RestaurarFilaUsuario(u.Id, u.Usuario, u.Pass, u.Direccion, u.Rol);
            }
        }

        private void RestaurarUpsertMateria()
        {
            foreach (var f in ObtenerSnapshot("MATERIA"))
            {
                var m = RehidratarMateria(f.Atributos);
                _dal.RestaurarFilaMateria(m.Id, m.Nombre, m.Codigo, m.Modalidad, m.Peso, m.Activa);
            }
        }

        private void RestaurarUpsertPeriodoAcademico()
        {
            foreach (var f in ObtenerSnapshot("PERIODO_ACADEMICO"))
            {
                var pa = RehidratarPeriodoAcademico(f.Atributos);
                _dal.RestaurarFilaPeriodoAcademico(pa.Id, pa.Anio, pa.Cuatrimestre, pa.Descripcion, pa.FechaInicio, pa.FechaFin);
            }
        }

        private void RestaurarUpsertBitacora()
        {
            foreach (var f in ObtenerSnapshot("BITACORA"))
            {
                var b = RehidratarBitacora(f.Atributos);
                _dal.RestaurarFilaBitacora(b.Id, b.Usuario, b.Accion, b.Fecha);
            }
        }

        private void RestaurarUpsertCorrelativa()
        {
            foreach (var f in ObtenerSnapshot("CORRELATIVA"))
            {
                var c = RehidratarCorrelativa(f.Atributos);
                _dal.RestaurarFilaCorrelativa(c.Id, c.IdMateria, c.IdCorrelativa);
            }
        }

        private void RestaurarUpsertAlumnoMateria()
        {
            foreach (var f in ObtenerSnapshot("ALUMNO_MATERIA"))
            {
                var am = RehidratarAlumnoMateria(f.Atributos);
                _dal.RestaurarFilaAlumnoMateria(am.Id, am.IdUsuario, am.IdMateria, am.Estado,
                    am.NotaParcial1, am.NotaParcial2, am.NotaRecuperatorio, am.NotaFinal,
                    am.FechaFinal, am.FechaRecuperatorio);
            }
        }

        private void RestaurarUpsertEventoAcademico()
        {
            foreach (var f in ObtenerSnapshot("EVENTO_ACADEMICO"))
            {
                var ev = RehidratarEventoAcademico(f.Atributos);
                _dal.RestaurarFilaEventoAcademico(ev.Id, ev.IdMateria, ev.IdUsuario, ev.Tipo, ev.Descripcion, ev.Fecha, ev.Peso);
            }
        }

        private void RestaurarUpsertInscripcion()
        {
            foreach (var f in ObtenerSnapshot("INSCRIPCION"))
            {
                var i = RehidratarInscripcion(f.Atributos);
                _dal.RestaurarFilaInscripcion(i.Id, i.IdUsuario, i.IdPeriodo, i.FechaInscripcion);
            }
        }

        private void RestaurarUpsertInscripcionDetalle()
        {
            foreach (var f in ObtenerSnapshot("INSCRIPCION_DETALLE"))
            {
                var d = RehidratarInscripcionDetalle(f.Atributos);
                _dal.RestaurarFilaInscripcionDetalle(d.Id, d.IdInscripcion, d.IdMateria);
            }
        }
    }
}
