using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// Calculador de dígitos verificadores horizontal (DVH, por fila) y vertical
    /// (DVV, por columna) usado para detectar alteraciones no autorizadas de datos
    /// almacenados directamente en la base (por fuera de la aplicación).
    /// Fórmula portada tal cual del TP hermano "ingenieria_software" (ya aprobada).
    /// </summary>
    public static class CalculadorDVH
    {
        public static int Calcular(string[] atributos)
        {
            int dvh = 0;
            for (int i = 0; i < atributos.Length; i++)
            {
                string val = atributos[i] ?? string.Empty;
                for (int j = 0; j < val.Length; j++)
                    dvh += val[j] * (i + 1) * (j + 1);
            }
            return dvh;
        }

        public static int CalcularVertical(List<string[]> filas, int colIdx)
        {
            int dvv = 0;
            for (int k = 0; k < filas.Count; k++)
            {
                string val = (filas[k] != null && colIdx < filas[k].Length)
                    ? filas[k][colIdx] ?? string.Empty
                    : string.Empty;
                for (int j = 0; j < val.Length; j++)
                    dvv += val[j] * (k + 1) * (j + 1);
            }
            return dvv;
        }
    }
}
