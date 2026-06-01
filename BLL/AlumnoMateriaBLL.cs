using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    public class ResultadoIntegridad
    {
        public int    IdUsuario     { get; set; }
        public string NombreUsuario { get; set; }
        public bool   Ok            { get; set; }
    }

    public class AlumnoMateriaBLL
    {
        private readonly DAL.AlumnoMateriaDAL _dal = new DAL.AlumnoMateriaDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();

        public List<BE.ALUMNO_MATERIA> Listar(int idUsuario) => _dal.Listar(idUsuario);

        public bool Inscribir(BE.ALUMNO_MATERIA am, string usuarioAccion)
        {
            bool ok = _dal.Insertar(am);
            if (ok) _bitacora.RegistrarAccion(usuarioAccion, $"INSCRIPCION_MATERIA:{am.IdMateria}");
            return ok;
        }

        public bool ActualizarNotas(BE.ALUMNO_MATERIA am, string usuarioAccion)
        {
            am.DVH = CalcularDVH(am);
            bool ok = _dal.Actualizar(am);

            if (ok)
            {
                _bitacora.RegistrarAccion(usuarioAccion, $"ACTUALIZACION_NOTAS:{am.IdMateria}");
                ActualizarDVV(am.IdUsuario, usuarioAccion);
            }
            return ok;
        }

        public int CalcularDVH(BE.ALUMNO_MATERIA am)
        {
            int p1   = (int)((am.NotaParcial1      ?? 0) * 10);
            int p2   = (int)((am.NotaParcial2      ?? 0) * 10);
            int rec  = (int)((am.NotaRecuperatorio ?? 0) * 10);
            int fin  = (int)((am.NotaFinal         ?? 0) * 10);
            int peso = am.PesoMateria;
            return (p1 + p2 + rec + fin + peso) % 10;
        }

        public void ActualizarDVV(int idUsuario, string usuarioAccion)
        {
            var lista = _dal.Listar(idUsuario);
            int dvv = lista.Sum(am => am.DVH) % 10;
            _dal.GuardarDVV(idUsuario, dvv);
        }

        public bool VerificarIntegridad(int idUsuario)
        {
            var lista    = _dal.Listar(idUsuario);
            int dvvReal  = lista.Sum(am => am.DVH) % 10;
            int dvvGuard = _dal.ObtenerDVV(idUsuario);
            return dvvReal == dvvGuard;
        }

        public List<ResultadoIntegridad> VerificarTodos()
        {
            var alumnos = new DAL.UsuarioDAL().ListarAlumnos();
            var resultado = new List<ResultadoIntegridad>();
            foreach (var u in alumnos)
                resultado.Add(new ResultadoIntegridad
                {
                    IdUsuario     = u.Id,
                    NombreUsuario = u.Usuario,
                    Ok            = VerificarIntegridad(u.Id)
                });
            return resultado;
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
