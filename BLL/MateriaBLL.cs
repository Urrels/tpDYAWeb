using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    public class MateriaBLL
    {
        private readonly DAL.MateriaDAL _dal = new DAL.MateriaDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();

        public int Insertar(BE.MATERIA m, string usuarioAccion)
        {
            int id = _dal.Insertar(m);
            if (id > 0) _bitacora.RegistrarAccion(usuarioAccion, $"ALTA_MATERIA:{m.Nombre}");
            return id;
        }

        public bool Actualizar(BE.MATERIA m, string usuarioAccion)
        {
            bool ok = _dal.Actualizar(m);
            if (ok) _bitacora.RegistrarAccion(usuarioAccion, $"EDICION_MATERIA:{m.Nombre}");
            return ok;
        }

        public bool Eliminar(int id, string usuarioAccion)
        {
            BE.MATERIA m = _dal.Obtener(id);
            bool ok = _dal.Eliminar(id);
            if (ok) _bitacora.RegistrarAccion(usuarioAccion, $"BAJA_MATERIA:{m?.Nombre}");
            return ok;
        }

        public List<BE.MATERIA> Listar() => _dal.Listar();

        public BE.MATERIA Obtener(int id)
        {
            var m = _dal.Obtener(id);
            if (m != null) m.Correlativas = _dal.ListarCorrelativas(id);
            return m;
        }

        public void AgregarCorrelativa(int idMateria, int idCorrelativa) =>
            _dal.InsertarCorrelativa(idMateria, idCorrelativa);

        public void QuitarCorrelativa(int idMateria, int idCorrelativa) =>
            _dal.EliminarCorrelativa(idMateria, idCorrelativa);

        public List<BE.MATERIA> ListarCorrelativas(int idMateria) =>
            _dal.ListarCorrelativas(idMateria);

        /// <summary>
        /// RF05 - Recomendador: devuelve materias que el alumno puede cursar
        /// el próximo cuatrimestre validando correlativas en BD.
        /// </summary>
        public List<BE.MATERIA> RecomendarMaterias(List<BE.ALUMNO_MATERIA> cursadas)
        {
            // IDs de materias aprobadas
            var aprobadas = cursadas
                .Where(am => am.Estado == "Aprobada")
                .Select(am => am.IdMateria)
                .ToHashSet();

            // IDs ya inscripto (cualquier estado)
            var yaInscripto = cursadas
                .Select(am => am.IdMateria)
                .ToHashSet();

            var todasLasMaterias = _dal.Listar();
            var recomendadas = new List<BE.MATERIA>();

            foreach (var materia in todasLasMaterias)
            {
                if (yaInscripto.Contains(materia.Id)) continue;

                // Verificar correlativas en BD
                var correlativas = _dal.ListarCorrelativas(materia.Id);
                bool cumpleCorrelativas = correlativas.All(c => aprobadas.Contains(c.Id));

                if (cumpleCorrelativas)
                    recomendadas.Add(materia);
            }

            return recomendadas;
        }
    }
}
