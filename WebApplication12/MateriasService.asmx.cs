using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Services;

namespace CAPAS_Web
{
    public class MateriaDto
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Modalidad { get; set; }
        public int Peso { get; set; }
        public string[] Correlativas { get; set; }
    }

    [WebService(Namespace = "http://tpdyaweb.uai.edu.ar/", Description = "Consulta del plan de estudios (materias y correlativas).")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.Web.Script.Services.ScriptService]
    public class MateriasService : WebService
    {
        private readonly BLL.MateriaBLL _bll = new BLL.MateriaBLL();

        [WebMethod(EnableSession = true, Description = "Lista las materias activas con sus correlativas.")]
        public List<MateriaDto> ListarMaterias()
        {
            ExigirPermiso();
            return _bll.Listar().OrderBy(m => m.Id).Select(Convertir).ToList();
        }

        [WebMethod(EnableSession = true, Description = "Devuelve una materia activa por su código, o null si no existe.")]
        public MateriaDto ObtenerMateria(string codigo)
        {
            ExigirPermiso();
            if (string.IsNullOrWhiteSpace(codigo)) return null;
            BE.MATERIA m = _bll.Listar().FirstOrDefault(x =>
                string.Equals(x.Codigo, codigo.Trim(), StringComparison.OrdinalIgnoreCase));
            return m == null ? null : Convertir(m);
        }

        [WebMethod(EnableSession = true, Description = "Resumen académico del alumno de la sesión: cantidades por estado y promedio ponderado.")]
        public BLL.ResumenAcademico ObtenerResumenAcademico()
        {
            BE.USUARIO usuario = ExigirPermiso(BE.Permisos.VerDashboard);
            return new BLL.AlumnoMateriaBLL().ObtenerResumen(usuario.Id);
        }

        private MateriaDto Convertir(BE.MATERIA m) => new MateriaDto
        {
            Codigo = m.Codigo,
            Nombre = m.Nombre,
            Modalidad = m.Modalidad,
            Peso = m.Peso,
            Correlativas = _bll.ListarCorrelativas(m.Id).OrderBy(c => c.Id).Select(c => c.Codigo).ToArray()
        };

        private BE.USUARIO ExigirPermiso(string permiso = BE.Permisos.GestionarMaterias)
        {
            var usuario = Session["Usuario"] as BE.USUARIO;
            if (usuario == null || BLL.PerfilBLL.SesionRevocada(usuario)
                || !new BLL.PerfilBLL().RefrescarPermisosSiHaceFalta(usuario))
                throw new UnauthorizedAccessException("Sesión no iniciada o vencida.");
            if (!usuario.TienePermiso(permiso))
                throw new UnauthorizedAccessException("No tenés permiso para esta consulta.");
            return usuario;
        }
    }
}
