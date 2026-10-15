using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    public class AlumnoMateriaBLL
    {
        private readonly DAL.AlumnoMateriaDAL _dal = new DAL.AlumnoMateriaDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();
        private readonly IntegridadBLL _integridad = new IntegridadBLL();

        public List<BE.ALUMNO_MATERIA> Listar(int idUsuario) => _dal.Listar(idUsuario);

        public const string MensajeNotaInvalida = "Las notas deben ser números entre 0 y 10.";

        public static bool TryParsearNota(string texto, out decimal? nota)
        {
            nota = null;
            if (string.IsNullOrWhiteSpace(texto)) return true;
            if (!decimal.TryParse(texto.Trim(),
                    System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint,
                    System.Globalization.CultureInfo.CurrentCulture, out decimal valor)
                || valor < 0 || valor > 10)
                return false;
            nota = valor;
            return true;
        }

        public bool ActualizarNotas(BE.ALUMNO_MATERIA am, string usuarioAccion)
        {
            am.DVH = _integridad.CalcularDVHAlumnoMateria(am);
            bool ok = _dal.Actualizar(am);

            if (ok)
            {
                _bitacora.RegistrarAccion(usuarioAccion, $"ACTUALIZACION_NOTAS:{am.IdMateria}");
                _integridad.RecalcularAlumnoMateria();
            }
            return ok;
        }

        public List<BE.ALUMNO_MATERIA> ListarConRiesgo(int idUsuario, List<BE.EVENTO_ACADEMICO> eventos)
        {
            var lista = Listar(idUsuario);
            foreach (var am in lista)
                CalcularRiesgo(am, eventos);
            return lista;
        }

        private void CalcularRiesgo(BE.ALUMNO_MATERIA am, List<BE.EVENTO_ACADEMICO> eventos)
        {
            if (am.Estado == "Aprobada")
            {
                am.NivelRiesgo  = "Aprobada";
                am.MensajeRiesgo = "✔ Materia aprobada.";
                return;
            }

            var hoy = DateTime.Today;
            bool examenProximo = eventos.Any(e =>
                e.IdMateria == am.IdMateria &&
                e.Fecha.Date >= hoy &&
                e.Fecha.Date <= hoy.AddDays(7));

            if (am.NotaParcial1.HasValue && am.NotaParcial1 < 4)
            {
                am.NivelRiesgo   = "Alto";
                am.MensajeRiesgo = $"🔴 Parcial 1 desaprobado ({am.NotaParcial1}). Necesitás ir al recuperatorio.";
                return;
            }
            if (am.NotaParcial2.HasValue && am.NotaParcial2 < 4)
            {
                am.NivelRiesgo   = "Alto";
                am.MensajeRiesgo = $"🔴 Parcial 2 desaprobado ({am.NotaParcial2}). Necesitás ir al recuperatorio.";
                return;
            }

            if (am.NotaParcial1.HasValue && am.NotaParcial1 < 6)
            {
                am.NivelRiesgo   = "Medio";
                am.MensajeRiesgo = $"🟡 Parcial 1 con margen justo ({am.NotaParcial1}). Necesitás al menos 6 en el Parcial 2.";
                return;
            }

            if (!am.NotaParcial1.HasValue && examenProximo)
            {
                am.NivelRiesgo   = "Alto";
                am.MensajeRiesgo = "🔴 Tenés un examen en menos de 7 días y no hay notas cargadas.";
                return;
            }

            if (!am.NotaParcial1.HasValue)
            {
                am.NivelRiesgo   = "Medio";
                am.MensajeRiesgo = "🟡 Sin notas cargadas todavía.";
                return;
            }

            am.NivelRiesgo   = "Bajo";
            am.MensajeRiesgo = "🟢 Vas bien. Seguí así.";
        }

        public ResumenAcademico ObtenerResumen(int idUsuario)
        {
            var cursadas = Listar(idUsuario);
            var (promedio, mensaje) = CalcularPromedioPonderado(idUsuario);
            return new ResumenAcademico
            {
                TotalMaterias = cursadas.Count,
                Aprobadas = cursadas.Count(am => am.Estado == "Aprobada"),
                Cursando = cursadas.Count(am => am.Estado == "Cursando"),
                FinalPendiente = cursadas.Count(am => am.Estado == "FinalPendiente"),
                PromedioPonderado = promedio,
                MensajePromedio = mensaje
            };
        }

        public (decimal promedio, string mensaje) CalcularPromedioPonderado(int idUsuario)
        {
            var lista = Listar(idUsuario)
                .Where(am => am.NotaFinal.HasValue)
                .ToList();

            if (!lista.Any())
                return (0, "Sin materias finalizadas todavía.");

            decimal sumaPonderada = lista.Sum(am => (am.NotaFinal ?? 0) * am.PesoMateria);
            decimal sumaPesos     = lista.Sum(am => am.PesoMateria);
            decimal promedio      = sumaPesos > 0 ? sumaPonderada / sumaPesos : 0;

            string msg = promedio >= 7 ? "🟢 Promedio excelente"
                       : promedio >= 5 ? "🟡 Promedio aceptable"
                       : "🔴 Promedio bajo, revisá tu situación";

            return (Math.Round(promedio, 2), msg);
        }
    }
}

namespace BLL
{
    public class ResumenAcademico
    {
        public int TotalMaterias { get; set; }
        public int Aprobadas { get; set; }
        public int Cursando { get; set; }
        public int FinalPendiente { get; set; }
        public decimal PromedioPonderado { get; set; }
        public string MensajePromedio { get; set; }
    }
}
