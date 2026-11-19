using System;

namespace BLL
{
    public static class ValidacionLongitud
    {
        public const int MaxNombreMateria = 100;
        public const int MaxCodigoMateria = 20;
        public const int MaxModalidadMateria = 20;
        public const int MaxDescripcionEvento = 170;
        public const int MaxDescripcionPeriodo = 170;

        private static void Verificar(string valor, int maximo, string mensaje)
        {
            if (valor != null && valor.Trim().Length > maximo)
                throw new InvalidOperationException(mensaje);
        }

        public static void Materia(BE.MATERIA m)
        {
            Verificar(m.Nombre, MaxNombreMateria, "El nombre de la materia no puede superar los 100 caracteres.");
            Verificar(m.Codigo, MaxCodigoMateria, "El código de la materia no puede superar los 20 caracteres.");
            Verificar(m.Modalidad, MaxModalidadMateria, "La modalidad de la materia no puede superar los 20 caracteres.");
        }

        public static void Evento(BE.EVENTO_ACADEMICO e) =>
            Verificar(e.Descripcion, MaxDescripcionEvento, "La descripción del evento no puede superar los 170 caracteres.");

        public static void Periodo(BE.PERIODO_ACADEMICO p) =>
            Verificar(p.Descripcion, MaxDescripcionPeriodo, "La descripción del período no puede superar los 170 caracteres.");
    }
}
