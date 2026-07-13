using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    public class MateriaBLL
    {
        private readonly DAL.MateriaDAL _dal = new DAL.MateriaDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();
        private readonly IntegridadBLL _integridad = new IntegridadBLL();

        public int Insertar(BE.MATERIA m, string usuarioAccion)
        {
            int id = _dal.Insertar(m);
            if (id > 0)
            {
                _bitacora.RegistrarAccion(usuarioAccion, $"ALTA_MATERIA:{m.Nombre}");
                _integridad.RecalcularMateria();
            }
            return id;
        }

        public bool Actualizar(BE.MATERIA m, string usuarioAccion)
        {
            bool ok = _dal.Actualizar(m);
            if (ok)
            {
                _bitacora.RegistrarAccion(usuarioAccion, $"EDICION_MATERIA:{m.Nombre}");
                _integridad.RecalcularMateria();
            }
            return ok;
        }

        public bool Eliminar(int id, string usuarioAccion)
        {
            BE.MATERIA m = _dal.Obtener(id);
            bool ok = _dal.Eliminar(id);
            if (ok)
            {
                _bitacora.RegistrarAccion(usuarioAccion, $"BAJA_MATERIA:{m?.Nombre}");
                _integridad.RecalcularMateria();
            }
            return ok;
        }

        public List<BE.MATERIA> Listar() => _dal.Listar();

        public BE.MATERIA Obtener(int id)
        {
            var m = _dal.Obtener(id);
            if (m != null) m.Correlativas = _dal.ListarCorrelativas(id);
            return m;
        }

        public List<BE.MATERIA> ListarCorrelativas(int idMateria) =>
            _dal.ListarCorrelativas(idMateria);
    }
}
