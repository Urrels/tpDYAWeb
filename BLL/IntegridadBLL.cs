using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace BLL
{
    public class ResultadoIntegridad
    {
        public bool EsValido { get; set; } = true;
        public List<string> Errores { get; set; } = new List<string>();
        public List<int> IdsUsuariosAfectados { get; set; } = new List<int>();
    }

    public class IntegridadBLL
    {
        private readonly DAL.IntegridadDAL _dal = new DAL.IntegridadDAL();

        private class FilaSnapshot
        {
            public int Id { get; set; }
            public string[] Atributos { get; set; }
        }

        private static readonly string[] ColumnasUsuario =
            { "ID", "USUARIO", "PASS", "DIRECCION", "INTENTOS_FALLIDOS", "BLOQUEADO", "ACTIVO" };

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

        private static readonly string[] ColumnasRol =
            { "ID", "NOMBRE", "PADRE_ID", "PROTEGIDO" };

        private static readonly string[] ColumnasPermiso =
            { "ID", "NOMBRE" };

        private static readonly string[] ColumnasRolPermiso =
            { "ID", "ROL_ID", "PERMISO_ID" };

        private static readonly string[] ColumnasUsuarioPerfil =
            { "ID", "USUARIO_ID", "ROL_ID" };

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
                    resultado.EsValido = false;
                    huboErrorDvh = true;
                    if (nombreTabla == "USUARIO")
                        resultado.IdsUsuariosAfectados.Add(id);
                }
            }

            var dvvGuardados = _dal.ObtenerDVV(nombreTabla);

            if (datos.Rows.Count == 0 && dvvGuardados.Count == 0)
                return;

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

            GuardarSnapshot(nombreTabla, datos, extraerAtributos);
        }

        private void DiagnosticarDiferencias(string nombreTabla, string[] columnas, DataTable datosActuales,
            Func<DataRow, string[]> extraerAtributos, ResultadoIntegridad resultado)
        {
            List<FilaSnapshot> snapshot = ObtenerSnapshot(nombreTabla);
            if (snapshot.Count == 0)
                return;

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
                    continue;

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

        private void GuardarSnapshot(string nombreTabla, DataTable datos, Func<DataRow, string[]> extraerAtributos)
        {
            var snapshot = new List<FilaSnapshot>();
            foreach (DataRow fila in datos.Rows)
                snapshot.Add(new FilaSnapshot { Id = Convert.ToInt32(fila["ID"]), Atributos = extraerAtributos(fila) });

            string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(snapshot);
            _dal.GuardarSnapshot(nombreTabla, json);
        }

        private List<FilaSnapshot> ObtenerSnapshot(string nombreTabla)
        {
            string json = _dal.ObtenerSnapshot(nombreTabla);
            if (string.IsNullOrEmpty(json))
                return new List<FilaSnapshot>();
            return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<List<FilaSnapshot>>(json);
        }

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

        private static string[] ExtraerUsuario(DataRow r) => new[]
        {
            IntStr(r["ID"]), StrStr(r["USUARIO"]), StrStr(r["PASS"]), Base64Str(r["DIRECCION"]),
            IntStr(r["INTENTOS_FALLIDOS"]), BitStr(r["BLOQUEADO"]), BitStr(r["ACTIVO"])
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

        private static string[] ExtraerRol(DataRow r) => new[]
        {
            IntStr(r["ID"]), StrStr(r["NOMBRE"]), IntStr(r["PADRE_ID"]), BitStr(r["PROTEGIDO"])
        };

        private static string[] ExtraerPermiso(DataRow r) => new[]
        {
            IntStr(r["ID"]), StrStr(r["NOMBRE"])
        };

        private static string[] ExtraerRolPermiso(DataRow r) => new[]
        {
            IntStr(r["ID"]), IntStr(r["ROL_ID"]), IntStr(r["PERMISO_ID"])
        };

        private static string[] ExtraerUsuarioPerfil(DataRow r) => new[]
        {
            IntStr(r["ID"]), IntStr(r["USUARIO_ID"]), IntStr(r["ROL_ID"])
        };

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

        public ResultadoIntegridad VerificarRol()
        {
            var resultado = new ResultadoIntegridad();
            VerificarTabla("ROL", ColumnasRol, _dal.ListarParaIntegridad("ROL"), ExtraerRol, resultado);
            return resultado;
        }
        public void RecalcularRol()
        {
            RecalcularTabla("ROL", ColumnasRol, _dal.ListarParaIntegridad("ROL"), ExtraerRol,
                (id, dvh) => _dal.ActualizarDVH("ROL", id, dvh));
        }

        public ResultadoIntegridad VerificarPermiso()
        {
            var resultado = new ResultadoIntegridad();
            VerificarTabla("PERMISO", ColumnasPermiso, _dal.ListarParaIntegridad("PERMISO"), ExtraerPermiso, resultado);
            return resultado;
        }
        public void RecalcularPermiso()
        {
            RecalcularTabla("PERMISO", ColumnasPermiso, _dal.ListarParaIntegridad("PERMISO"), ExtraerPermiso,
                (id, dvh) => _dal.ActualizarDVH("PERMISO", id, dvh));
        }

        public ResultadoIntegridad VerificarRolPermiso()
        {
            var resultado = new ResultadoIntegridad();
            VerificarTabla("ROL_PERMISO", ColumnasRolPermiso, _dal.ListarParaIntegridad("ROL_PERMISO"), ExtraerRolPermiso, resultado);
            return resultado;
        }
        public void RecalcularRolPermiso()
        {
            RecalcularTabla("ROL_PERMISO", ColumnasRolPermiso, _dal.ListarParaIntegridad("ROL_PERMISO"), ExtraerRolPermiso,
                (id, dvh) => _dal.ActualizarDVH("ROL_PERMISO", id, dvh));
        }

        public ResultadoIntegridad VerificarUsuarioPerfil()
        {
            var resultado = new ResultadoIntegridad();
            VerificarTabla("USUARIO_PERFIL", ColumnasUsuarioPerfil, _dal.ListarParaIntegridad("USUARIO_PERFIL"), ExtraerUsuarioPerfil, resultado);
            return resultado;
        }
        public void RecalcularUsuarioPerfil()
        {
            RecalcularTabla("USUARIO_PERFIL", ColumnasUsuarioPerfil, _dal.ListarParaIntegridad("USUARIO_PERFIL"), ExtraerUsuarioPerfil,
                (id, dvh) => _dal.ActualizarDVH("USUARIO_PERFIL", id, dvh));
        }

        public ResultadoIntegridad VerificarTodasLasTablas()
        {
            var consolidado = new ResultadoIntegridad();
            var parciales = new[]
            {
                VerificarUsuario(), VerificarBitacora(), VerificarMateria(), VerificarCorrelativa(),
                VerificarAlumnoMateria(), VerificarEventoAcademico(), VerificarPeriodoAcademico(),
                VerificarInscripcion(), VerificarInscripcionDetalle(),
                VerificarRol(), VerificarPermiso(), VerificarRolPermiso(), VerificarUsuarioPerfil()
            };

            foreach (var parcial in parciales)
            {
                if (!parcial.EsValido) consolidado.EsValido = false;
                consolidado.Errores.AddRange(parcial.Errores);
                consolidado.IdsUsuariosAfectados.AddRange(parcial.IdsUsuariosAfectados);
            }
            return consolidado;
        }

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
            RecalcularRol();
            RecalcularPermiso();
            RecalcularRolPermiso();
            RecalcularUsuarioPerfil();
        }

        private static readonly object _lockInit = new object();
        private static bool _datosBaseInicializados;
        private static string _errorInicializacion;

        public enum ModoInicializacion
        {
            Ninguna,
            Perfiles,
            Todas
        }

        private static ModoInicializacion? _modoInicial;

        private ModoInicializacion DeterminarModoInicial()
        {
            if (new DAL.SiembraDAL().ExisteMarcadorSiembra()) return ModoInicializacion.Ninguna;
            return _dal.ListarParaIntegridad("USUARIO").Rows.Count == 0
                ? ModoInicializacion.Todas
                : ModoInicializacion.Perfiles;
        }

        public static string ErrorInicializacionDatosBase
        {
            get { lock (_lockInit) { return _errorInicializacion; } }
        }

        public static void IntentarInicializarDatosBase()
        {
            lock (_lockInit)
            {
                if (_datosBaseInicializados) return;
                try
                {
                    var integridad = new IntegridadBLL();
                    if (_modoInicial == null) _modoInicial = integridad.DeterminarModoInicial();
                    new SiembraBLL().SembrarDatosBase();
                    integridad.InicializarTablasNuncaCalculadas(_modoInicial.Value);
                    _datosBaseInicializados = true;
                    _errorInicializacion = null;
                }
                catch (Exception ex)
                {
                    bool esNuevo = _errorInicializacion == null;
                    _errorInicializacion = ex.Message;
                    if (esNuevo)
                    {
                        try { new BitacoraBLL().RegistrarAccion("SISTEMA", "ERROR_INICIALIZAR_DATOS_BASE"); }
                        catch {  }
                    }
                }
            }
        }

        public void InicializarTablasNuncaCalculadas(ModoInicializacion modo)
        {
            if (modo == ModoInicializacion.Ninguna) return;

            InicializarSiNuncaCalculada("USUARIO", RecalcularUsuario);
            InicializarSiNuncaCalculada("ROL", RecalcularRol);
            InicializarSiNuncaCalculada("PERMISO", RecalcularPermiso);
            InicializarSiNuncaCalculada("ROL_PERMISO", RecalcularRolPermiso);
            InicializarSiNuncaCalculada("USUARIO_PERFIL", RecalcularUsuarioPerfil);
            if (modo != ModoInicializacion.Todas) return;

            InicializarSiNuncaCalculada("BITACORA", RecalcularBitacora);
            InicializarSiNuncaCalculada("MATERIA", RecalcularMateria);
            InicializarSiNuncaCalculada("CORRELATIVA", RecalcularCorrelativa);
            InicializarSiNuncaCalculada("ALUMNO_MATERIA", RecalcularAlumnoMateria);
            InicializarSiNuncaCalculada("EVENTO_ACADEMICO", RecalcularEventoAcademico);
            InicializarSiNuncaCalculada("PERIODO_ACADEMICO", RecalcularPeriodoAcademico);
            InicializarSiNuncaCalculada("INSCRIPCION", RecalcularInscripcion);
            InicializarSiNuncaCalculada("INSCRIPCION_DETALLE", RecalcularInscripcionDetalle);
        }

        private void InicializarSiNuncaCalculada(string nombreTabla, Action recalcular)
        {
            if (_dal.ObtenerDVV(nombreTabla).Count == 0 && string.IsNullOrEmpty(_dal.ObtenerSnapshot(nombreTabla)))
                recalcular();
        }

        public string ExportarBackupJson()
        {
            var tablas = new[]
            {
                "USUARIO", "BITACORA", "MATERIA", "CORRELATIVA", "ALUMNO_MATERIA",
                "EVENTO_ACADEMICO", "PERIODO_ACADEMICO", "INSCRIPCION", "INSCRIPCION_DETALLE",
                "ROL", "PERMISO", "ROL_PERMISO", "USUARIO_PERFIL"
            };

            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var tablasData = new Dictionary<string, object>();

            foreach (var tabla in tablas)
            {
                string json = _dal.ObtenerSnapshot(tabla);
                tablasData[tabla] = string.IsNullOrEmpty(json)
                    ? new object[0]
                    : serializer.DeserializeObject(json);
            }

            var backup = new Dictionary<string, object>
            {
                { "FechaBackup", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                { "Tablas", tablasData }
            };

            return serializer.Serialize(backup);
        }

        public List<string> RestaurarDesdeBackupJson(string backupJson)
        {
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var backup = (Dictionary<string, object>)serializer.DeserializeObject(backupJson);

            if (!backup.TryGetValue("Tablas", out object tablasObj))
                throw new ArgumentException("El archivo no tiene el formato de backup esperado (falta 'Tablas').");

            var tablasData = (Dictionary<string, object>)tablasObj;
            var tablasBase = new[]
            {
                "USUARIO", "BITACORA", "MATERIA", "CORRELATIVA", "ALUMNO_MATERIA",
                "EVENTO_ACADEMICO", "PERIODO_ACADEMICO", "INSCRIPCION", "INSCRIPCION_DETALLE"
            };
            var tablasPerfiles = new[] { "ROL", "PERMISO", "ROL_PERMISO", "USUARIO_PERFIL" };

            foreach (var tabla in tablasBase)
                if (!tablasData.ContainsKey(tabla))
                    throw new ArgumentException($"El backup no contiene datos para la tabla {tabla}.");

            var omitidas = new HashSet<string>();
            var avisos = new List<string>();
            if (tablasPerfiles.Any(t => !tablasData.ContainsKey(t)))
            {
                foreach (var t in tablasPerfiles) omitidas.Add(t);
                avisos.Add("El backup es anterior a los perfiles dinámicos: no incluye ROL, PERMISO, ROL_PERMISO ni " +
                           "USUARIO_PERFIL. Esas tablas se dejaron como estaban.");
            }

            var aGuardar = new Dictionary<string, string>();
            var rolesPorUsuarioViejo = new Dictionary<int, string>();
            foreach (var tabla in tablasBase.Concat(tablasPerfiles.Where(t => !omitidas.Contains(t))))
            {
                object filas = tablasData[tabla];
                if (tabla == "USUARIO")
                    filas = AdaptarUsuarioDeBackup(serializer, filas, rolesPorUsuarioViejo, avisos);
                aGuardar[tabla] = serializer.Serialize(filas);
            }

            foreach (var par in aGuardar)
                _dal.GuardarSnapshot(par.Key, par.Value);

            avisos.AddRange(RestaurarTablas(omitidas));

            if (rolesPorUsuarioViejo.Count > 0 && omitidas.Count > 0)
                avisos.AddRange(AsignarPerfilesDesdeRolViejo(rolesPorUsuarioViejo));

            return avisos;
        }

        private static object AdaptarUsuarioDeBackup(System.Web.Script.Serialization.JavaScriptSerializer serializer,
            object filas, Dictionary<int, string> rolesViejos, List<string> avisos)
        {
            var lista = serializer.Deserialize<List<FilaSnapshot>>(serializer.Serialize(filas));
            int largoActual = ColumnasUsuario.Length;
            bool adaptado = false;
            foreach (var f in lista)
            {
                int n = f.Atributos == null ? -1 : f.Atributos.Length;

                if (n == largoActual && !int.TryParse(f.Atributos[4], out _))
                {
                    rolesViejos[f.Id] = f.Atributos[4];
                    var sinRol = new List<string>(f.Atributos);
                    sinRol.RemoveAt(4);
                    sinRol.Add("1");
                    f.Atributos = sinRol.ToArray();
                    adaptado = true;
                }
                else if (n == largoActual - 1)
                {
                    f.Atributos = f.Atributos.Concat(new[] { "1" }).ToArray();
                    adaptado = true;
                }
                else if (n != largoActual)
                    throw new ArgumentException("El backup tiene un formato de USUARIO que no se reconoce; no se restauró nada.");
            }
            if (adaptado)
                avisos.Add("El backup tiene un formato anterior de USUARIO: se adaptó (los usuarios quedan activos" +
                           (rolesViejos.Count > 0 ? " y los que no tienen perfil se reasignan según su rol de entonces" : "") + ").");
            return lista;
        }

        private List<string> AsignarPerfilesDesdeRolViejo(Dictionary<int, string> rolesViejos)
        {
            var avisos = new List<string>();
            var perfilDal = new DAL.PerfilDAL();
            var idsRol = perfilDal.ListarRoles().ToDictionary(r => r.Nombre, r => r.Id);
            foreach (var par in rolesViejos)
            {
                if (perfilDal.ListarRolesDeUsuario(par.Key).Count > 0) continue;
                string nombre = par.Value == "Admin" ? "Administrador" : par.Value == "Webmaster" ? "Webmaster" : "Alumno";
                if (idsRol.TryGetValue(nombre, out int rolId))
                    perfilDal.AsignarPerfil(par.Key, rolId);
                else
                    avisos.Add($"No se encontró el perfil {nombre} para el usuario {par.Key}.");
            }
            RecalcularUsuarioPerfil();
            return avisos;
        }

        public List<string> RestaurarTodasLasTablas()
        {
            return RestaurarTablas(new HashSet<string>());
        }

        private List<string> RestaurarTablas(HashSet<string> omitidas)
        {
            var avisos = new List<string>();

            foreach (var tabla in new[]
            {
                "USUARIO_PERFIL", "ROL_PERMISO", "INSCRIPCION_DETALLE", "INSCRIPCION", "EVENTO_ACADEMICO",
                "ALUMNO_MATERIA", "CORRELATIVA", "BITACORA", "PERIODO_ACADEMICO", "MATERIA",
                "ROL", "PERMISO", "USUARIO"
            })
                if (!omitidas.Contains(tabla))
                    BorrarAgregadosExternamente(tabla, avisos);

            RestaurarUpsertUsuario();
            if (!omitidas.Contains("ROL")) RestaurarUpsertRol();
            if (!omitidas.Contains("PERMISO")) RestaurarUpsertPermiso();
            RestaurarUpsertMateria();
            RestaurarUpsertPeriodoAcademico();
            RestaurarUpsertBitacora();
            RestaurarUpsertCorrelativa();
            RestaurarUpsertAlumnoMateria();
            RestaurarUpsertEventoAcademico();
            RestaurarUpsertInscripcion();
            RestaurarUpsertInscripcionDetalle();
            if (!omitidas.Contains("ROL_PERMISO")) RestaurarUpsertRolPermiso();
            if (!omitidas.Contains("USUARIO_PERFIL")) RestaurarUpsertUsuarioPerfil();

            RecalcularTodasLasTablas();
            return avisos;
        }

        private void BorrarAgregadosExternamente(string nombreTabla, List<string> avisos)
        {
            var idsSnapshot = new HashSet<int>();
            foreach (var f in ObtenerSnapshot(nombreTabla))
                idsSnapshot.Add(f.Id);

            DataTable actuales = _dal.ListarParaIntegridad(nombreTabla);

            if (idsSnapshot.Count == 0 && actuales.Rows.Count > 0)
            {
                avisos.Add($"{nombreTabla}: el snapshot está vacío, no se borraron las {actuales.Rows.Count} fila(s) actuales.");
                return;
            }

            foreach (DataRow fila in actuales.Rows)
            {
                int id = Convert.ToInt32(fila["ID"]);
                if (!idsSnapshot.Contains(id))
                    _dal.EliminarPorId(nombreTabla, id);
            }
        }

        private static int ParseInt(string s) => int.Parse(s, CultureInfo.InvariantCulture);

        private static decimal? ParseDecNullable(string s) =>
            string.IsNullOrEmpty(s) ? (decimal?)null : decimal.Parse(s, CultureInfo.InvariantCulture);

        private static DateTime? ParseFechaNullable(string s) =>
            string.IsNullOrEmpty(s) ? (DateTime?)null : DateTime.ParseExact(s, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        private static bool ParseBit(string s) => s == "1";

        private static byte[] ParseBase64(string s) =>
            string.IsNullOrEmpty(s) ? null : Convert.FromBase64String(s);

        private static string ParseStrNullable(string s) => string.IsNullOrEmpty(s) ? null : s;

        private class FilaUsuarioRestaurada
        {
            public int Id;
            public string Usuario;
            public string Pass;
            public byte[] Direccion;
            public bool Activo;
        }

        private class FilaRolRestaurada
        {
            public int Id;
            public string Nombre;
            public int? PadreId;
            public bool Protegido;
        }

        private class FilaPermisoRestaurada
        {
            public int Id;
            public string Nombre;
        }

        private class FilaRolPermisoRestaurada
        {
            public int Id;
            public int RolId;
            public int PermisoId;
        }

        private class FilaUsuarioPerfilRestaurada
        {
            public int Id;
            public int UsuarioId;
            public int RolId;
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
            Activo = ParseBit(a[6])
        };

        private static FilaRolRestaurada RehidratarRol(string[] a) => new FilaRolRestaurada
        {
            Id = ParseInt(a[0]),
            Nombre = a[1],
            PadreId = string.IsNullOrEmpty(a[2]) ? (int?)null : ParseInt(a[2]),
            Protegido = ParseBit(a[3])
        };

        private static FilaPermisoRestaurada RehidratarPermiso(string[] a) => new FilaPermisoRestaurada
        {
            Id = ParseInt(a[0]),
            Nombre = a[1]
        };

        private static FilaRolPermisoRestaurada RehidratarRolPermiso(string[] a) => new FilaRolPermisoRestaurada
        {
            Id = ParseInt(a[0]),
            RolId = ParseInt(a[1]),
            PermisoId = ParseInt(a[2])
        };

        private static FilaUsuarioPerfilRestaurada RehidratarUsuarioPerfil(string[] a) => new FilaUsuarioPerfilRestaurada
        {
            Id = ParseInt(a[0]),
            UsuarioId = ParseInt(a[1]),
            RolId = ParseInt(a[2])
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

        private void RestaurarUpsertUsuario()
        {
            foreach (var f in ObtenerSnapshot("USUARIO"))
            {
                var u = RehidratarUsuario(f.Atributos);
                _dal.RestaurarFilaUsuario(u.Id, u.Usuario, u.Pass, u.Direccion, u.Activo);
            }
        }

        private void RestaurarUpsertRol()
        {
            var filas = ObtenerSnapshot("ROL").Select(f => RehidratarRol(f.Atributos)).ToList();
            var idsRestantes = new HashSet<int>(filas.Select(r => r.Id));
            var pendientes = new List<FilaRolRestaurada>(filas);

            while (pendientes.Count > 0)
            {
                var listos = pendientes
                    .Where(r => !r.PadreId.HasValue || !idsRestantes.Contains(r.PadreId.Value))
                    .ToList();
                if (listos.Count == 0) listos = pendientes.ToList();

                foreach (var r in listos)
                {
                    _dal.RestaurarFilaRol(r.Id, r.Nombre, r.PadreId, r.Protegido);
                    idsRestantes.Remove(r.Id);
                    pendientes.Remove(r);
                }
            }
        }

        private void RestaurarUpsertPermiso()
        {
            foreach (var f in ObtenerSnapshot("PERMISO"))
            {
                var p = RehidratarPermiso(f.Atributos);
                _dal.RestaurarFilaPermiso(p.Id, p.Nombre);
            }
        }

        private void RestaurarUpsertRolPermiso()
        {
            foreach (var f in ObtenerSnapshot("ROL_PERMISO"))
            {
                var rp = RehidratarRolPermiso(f.Atributos);
                _dal.RestaurarFilaRolPermiso(rp.Id, rp.RolId, rp.PermisoId);
            }
        }

        private void RestaurarUpsertUsuarioPerfil()
        {
            foreach (var f in ObtenerSnapshot("USUARIO_PERFIL"))
            {
                var up = RehidratarUsuarioPerfil(f.Atributos);
                _dal.RestaurarFilaUsuarioPerfil(up.Id, up.UsuarioId, up.RolId);
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