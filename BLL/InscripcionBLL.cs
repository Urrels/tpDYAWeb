using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    public class PeriodoBLL
    {
        private readonly DAL.PeriodoDAL _dal = new DAL.PeriodoDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();

        public List<BE.PERIODO_ACADEMICO> Listar() => _dal.Listar();

        public BE.PERIODO_ACADEMICO ObtenerActual() => _dal.ObtenerActual();

        public int Insertar(BE.PERIODO_ACADEMICO p, string usuarioAccion)
        {
            int id = _dal.Insertar(p);
            if (id > 0) _bitacora.RegistrarAccion(usuarioAccion, $"ALTA_PERIODO:{p.Etiqueta}");
            return id;
        }

        public bool Actualizar(BE.PERIODO_ACADEMICO p, string usuarioAccion)
        {
            bool ok = _dal.Actualizar(p);
            if (ok) _bitacora.RegistrarAccion(usuarioAccion, $"MODIF_PERIODO:{p.Etiqueta}");
            return ok;
        }
    }

    public class InscripcionBLL
    {
        private readonly DAL.InscripcionDAL _dal      = new DAL.InscripcionDAL();
        private readonly DAL.PeriodoDAL     _perioDal = new DAL.PeriodoDAL();
        private readonly DAL.AlumnoMateriaDAL _amDal  = new DAL.AlumnoMateriaDAL();
        private readonly DAL.MateriaDAL     _matDal   = new DAL.MateriaDAL();
        private readonly BitacoraBLL _bitacora         = new BitacoraBLL();

        public BE.PERIODO_ACADEMICO ObtenerPeriodoActual() => _perioDal.ObtenerActual();

        public bool YaInscripto(int idUsuario, int idPeriodo) =>
            _dal.ExisteEnPeriodo(idUsuario, idPeriodo);

        public (List<BE.MATERIA> disponibles, List<BE.MATERIA> noDisponibles)
            ClasificarMaterias(int idUsuario)
        {
            var cursadas = _amDal.Listar(idUsuario);
            var aprobadas   = cursadas.Where(am => am.Estado == "Aprobada").Select(am => am.IdMateria).ToHashSet();
            var yaInscripto = cursadas.Select(am => am.IdMateria).ToHashSet();

            var disponibles   = new List<BE.MATERIA>();
            var noDisponibles = new List<BE.MATERIA>();

            foreach (var materia in _matDal.Listar())
            {
                if (yaInscripto.Contains(materia.Id)) continue;
                var correlativas = _matDal.ListarCorrelativas(materia.Id);
                if (correlativas.All(c => aprobadas.Contains(c.Id)))
                    disponibles.Add(materia);
                else
                {
                    materia.Correlativas = correlativas;
                    noDisponibles.Add(materia);
                }
            }

            return (disponibles, noDisponibles);
        }

        public (bool ok, string mensaje, int cantidad)
            Inscribir(int idUsuario, int idPeriodo, List<int> idsMaterias, string usuarioAccion)
        {
            if (idsMaterias == null || idsMaterias.Count == 0)
                return (false, "Seleccioná al menos una materia.", 0);

            var aprobadas = _amDal.Listar(idUsuario)
                                  .Where(am => am.Estado == "Aprobada")
                                  .Select(am => am.IdMateria)
                                  .ToHashSet();

            var todasLasMaterias = _matDal.Listar();

            foreach (int idMateria in idsMaterias)
            {
                var correlativas = _matDal.ListarCorrelativas(idMateria);
                if (!correlativas.All(c => aprobadas.Contains(c.Id)))
                {
                    string nombre = todasLasMaterias.FirstOrDefault(m => m.Id == idMateria)?.Nombre
                                    ?? idMateria.ToString();
                    return (false,
                        $"No podés inscribirte a '{nombre}': tenés correlativas pendientes.",
                        0);
                }
            }

            int idInscripcion = _dal.ObtenerIdPorPeriodo(idUsuario, idPeriodo);
            if (idInscripcion <= 0)
                idInscripcion = _dal.InsertarCabecera(idUsuario, idPeriodo);
            if (idInscripcion <= 0)
                return (false, "Error al registrar la inscripción.", 0);

            foreach (int idMateria in idsMaterias)
            {
                _dal.InsertarDetalle(idInscripcion, idMateria);
                _amDal.Insertar(new BE.ALUMNO_MATERIA
                {
                    IdUsuario = idUsuario,
                    IdMateria = idMateria,
                    Estado    = "Cursando"
                });
            }

            new AlumnoMateriaBLL().ActualizarDVV(idUsuario, usuarioAccion);
            _bitacora.RegistrarAccion(usuarioAccion,
                $"INSCRIPCION_CUATRIMESTRAL:PERIODO_{idPeriodo}:{idsMaterias.Count}_MATERIAS");

            return (true,
                $"✔ Inscripción completada: {idsMaterias.Count} materia(s) registrada(s).",
                idsMaterias.Count);
        }

        public List<BE.INSCRIPCION> ListarHistorial(int idUsuario) =>
            _dal.ListarPorUsuario(idUsuario);
    }
}
