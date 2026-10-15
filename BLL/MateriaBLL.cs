using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    public class MateriaBLL
    {
        private readonly DAL.MateriaDAL _dal = new DAL.MateriaDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();
        private readonly IntegridadBLL _integridad = new IntegridadBLL();

        public const string MensajeCodigoDuplicado = "El código de materia ya existe.";

        /// <summary>
        /// El CODIGO es UNIQUE en la base (sin distinguir mayúsculas): se compara igual para avisar antes
        /// de insertar. <paramref name="idExcluir"/> es la materia que se está editando (0 si es nueva).
        /// </summary>
        public static bool CodigoDuplicado(IEnumerable<BE.MATERIA> existentes, string codigo, int idExcluir)
        {
            string buscado = (codigo ?? "").Trim();
            return existentes.Any(x => x.Id != idExcluir
                && string.Equals((x.Codigo ?? "").Trim(), buscado, System.StringComparison.OrdinalIgnoreCase));
        }

        public int Insertar(BE.MATERIA m, string usuarioAccion)
        {
            if (CodigoDuplicado(_dal.Listar(), m.Codigo, 0))
                throw new System.InvalidOperationException(MensajeCodigoDuplicado);
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
            if (CodigoDuplicado(_dal.Listar(), m.Codigo, m.Id))
                throw new System.InvalidOperationException(MensajeCodigoDuplicado);
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

        public BE.PaginaResultado<BE.MATERIA> ListarPaginado(int pagina, int tamanio) =>
            Paginacion.ConsultarAjustando(pagina, p => _dal.ListarPaginado(p, tamanio));

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
