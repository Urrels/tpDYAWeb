using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.BLL
{
    [TestClass]
    public class DuplicadosTests
    {
        private static List<global::BE.MATERIA> Materias() => new List<global::BE.MATERIA>
        {
            new global::BE.MATERIA { Id = 1, Codigo = "MAT101" },
            new global::BE.MATERIA { Id = 2, Codigo = "FIS201" }
        };

        [TestMethod]
        public void MateriaNueva_ConCodigoExistente_EsDuplicada()
        {
            Assert.IsTrue(global::BLL.MateriaBLL.CodigoDuplicado(Materias(), "MAT101", 0));
        }

        [TestMethod]
        public void Codigo_SeComparaSinMayusculasNiEspacios()
        {
            Assert.IsTrue(global::BLL.MateriaBLL.CodigoDuplicado(Materias(), "  mat101 ", 0));
        }

        [TestMethod]
        public void MateriaEditada_ConSuPropioCodigo_NoEsDuplicada()
        {
            Assert.IsFalse(global::BLL.MateriaBLL.CodigoDuplicado(Materias(), "MAT101", 1));
        }

        [TestMethod]
        public void MateriaEditada_ConElCodigoDeOtra_EsDuplicada()
        {
            Assert.IsTrue(global::BLL.MateriaBLL.CodigoDuplicado(Materias(), "FIS201", 1));
        }

        [TestMethod]
        public void CodigoNuevo_NoEsDuplicado()
        {
            Assert.IsFalse(global::BLL.MateriaBLL.CodigoDuplicado(Materias(), "QUI301", 0));
        }

        private static List<global::BE.PERIODO_ACADEMICO> Periodos() => new List<global::BE.PERIODO_ACADEMICO>
        {
            new global::BE.PERIODO_ACADEMICO { Id = 1, Anio = 2026, Cuatrimestre = 1 },
            new global::BE.PERIODO_ACADEMICO { Id = 2, Anio = 2026, Cuatrimestre = 2 }
        };

        [TestMethod]
        public void PeriodoNuevo_ConMismoAnioYCuatrimestre_EsDuplicado()
        {
            var p = new global::BE.PERIODO_ACADEMICO { Id = 0, Anio = 2026, Cuatrimestre = 1 };
            Assert.IsTrue(global::BLL.PeriodoBLL.PeriodoDuplicado(Periodos(), p));
        }

        [TestMethod]
        public void PeriodoNuevo_ConOtroCuatrimestre_NoEsDuplicado()
        {
            var p = new global::BE.PERIODO_ACADEMICO { Id = 0, Anio = 2027, Cuatrimestre = 1 };
            Assert.IsFalse(global::BLL.PeriodoBLL.PeriodoDuplicado(Periodos(), p));
        }

        [TestMethod]
        public void PeriodoEditado_ConSusPropiosDatos_NoEsDuplicado()
        {
            var p = new global::BE.PERIODO_ACADEMICO { Id = 1, Anio = 2026, Cuatrimestre = 1 };
            Assert.IsFalse(global::BLL.PeriodoBLL.PeriodoDuplicado(Periodos(), p));
        }

        [TestMethod]
        public void PeriodoEditado_ConLosDatosDeOtro_EsDuplicado()
        {
            var p = new global::BE.PERIODO_ACADEMICO { Id = 1, Anio = 2026, Cuatrimestre = 2 };
            Assert.IsTrue(global::BLL.PeriodoBLL.PeriodoDuplicado(Periodos(), p));
        }
    }
}
