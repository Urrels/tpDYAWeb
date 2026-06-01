using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    public class EventoBLL
    {
        private readonly DAL.EventoDAL _dal = new DAL.EventoDAL();
        private readonly BitacoraBLL _bitacora = new BitacoraBLL();

        public bool Insertar(BE.EVENTO_ACADEMICO e, string usuarioAccion)
        {
            bool ok = _dal.Insertar(e);
            if (ok) _bitacora.RegistrarAccion(usuarioAccion, $"ALTA_EVENTO:{e.Tipo}:{e.Fecha:dd/MM/yyyy}");
            return ok;
        }

        public bool Actualizar(BE.EVENTO_ACADEMICO e, string usuarioAccion)
        {
            bool ok = _dal.Actualizar(e);
            if (ok) _bitacora.RegistrarAccion(usuarioAccion, $"EDICION_EVENTO:{e.Id}");
            return ok;
        }

        public bool Eliminar(int id, string usuarioAccion)
        {
            bool ok = _dal.Eliminar(id);
            if (ok) _bitacora.RegistrarAccion(usuarioAccion, $"BAJA_EVENTO:{id}");
            return ok;
        }

        public List<BE.EVENTO_ACADEMICO> ListarPorUsuario(int idUsuario) =>
            _dal.ListarPorUsuario(idUsuario);

        public List<BE.EVENTO_ACADEMICO> ListarPorMes(int idUsuario, int anio, int mes)
        {
            var eventos = _dal.ListarPorMes(idUsuario, anio, mes);
            AplicarMapaDeCalor(eventos);
            return eventos;
        }

        public void AplicarMapaDeCalor(List<BE.EVENTO_ACADEMICO> eventos)
        {
            var porSemana = eventos
                .GroupBy(e => ObtenerSemana(e.Fecha))
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Peso));

            foreach (var ev in eventos)
            {
                int semana      = ObtenerSemana(ev.Fecha);
                int cargaSemana = porSemana[semana];

                ev.ColorCalor = cargaSemana <= 5  ? "Verde"
                              : cargaSemana <= 10 ? "Amarillo"
                              : "Rojo";
            }
        }

        private int ObtenerSemana(DateTime fecha)
        {
            return System.Globalization.CultureInfo.CurrentCulture
                .Calendar.GetWeekOfYear(fecha,
                    System.Globalization.CalendarWeekRule.FirstDay,
                    DayOfWeek.Monday);
        }
    }
}
