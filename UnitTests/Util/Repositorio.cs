using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    /// <summary>Ubica los archivos del repositorio (scripts SQL, páginas) a partir de la carpeta donde corren las pruebas.</summary>
    internal static class Repositorio
    {
        private static string _raiz;

        public static string Raiz
        {
            get
            {
                if (_raiz != null) return _raiz;
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                while (dir != null && !File.Exists(Path.Combine(dir.FullName, "WebApplication12.sln")))
                    dir = dir.Parent;
                if (dir == null)
                    Assert.Inconclusive("No se encontró WebApplication12.sln subiendo desde " + AppDomain.CurrentDomain.BaseDirectory);
                return _raiz = dir.FullName;
            }
        }

        public static string Ruta(params string[] partes) => Path.Combine(Raiz, Path.Combine(partes));

        public static string Leer(params string[] partes) => File.ReadAllText(Ruta(partes), System.Text.Encoding.UTF8);

        public static bool TieneBom(params string[] partes)
        {
            byte[] b = File.ReadAllBytes(Ruta(partes));
            return b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF;
        }
    }
}
