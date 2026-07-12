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

        // Orden canónico de atributos por tabla — fijo, no debe cambiar una
        // vez que haya datos guardados (cambiar el orden invalida todos los
        // DVH/DVV ya calculados).
        private static readonly string[] ColumnasUsuario =
            { "ID", "USUARIO", "PASS", "DIRECCION", "ROL" };

        private static readonly string[] ColumnasBitacora =
            { "ID", "USUARIO", "ACCION", "FECHA" };

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

            foreach (DataRow fila in datos.Rows)
            {
                string[] atributos = extraerAtributos(fila);
                todasFilas.Add(atributos);

                int id = Convert.ToInt32(fila["ID"]);
                int dvhCalculado = BE.CalculadorDVH.Calcular(atributos);
                int dvhGuardado = Convert.ToInt32(fila["DVH"]);

                if (dvhCalculado != dvhGuardado)
                {
                    resultado.EsValido = false;
                    resultado.Errores.Add($"{nombreTabla}: fila ID={id} tiene un DVH inválido " +
                        $"(esperado {dvhCalculado}, guardado {dvhGuardado}).");
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
            IntStr(r["ID"]), StrStr(r["USUARIO"]), StrStr(r["PASS"]), Base64Str(r["DIRECCION"]), StrStr(r["ROL"])
        };

        private static string[] ExtraerBitacora(DataRow r) => new[]
        {
            IntStr(r["ID"]), StrStr(r["USUARIO"]), StrStr(r["ACCION"]), DateStr(r["FECHA"])
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
    }
}
