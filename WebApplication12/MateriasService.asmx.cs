using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Services;

namespace CAPAS_Web
{
    /// <summary>Materia tal como la expone el servicio: sin ids internos, con las correlativas por código.</summary>
    public class MateriaDto
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Modalidad { get; set; }
        public int Peso { get; set; }
        public string[] Correlativas { get; set; }
    }

    /// <summary>
    /// Servicio web de consulta del plan de estudios (solo lectura). No tiene un inicio de sesión
    /// propio: usa la sesión del sitio (el cliente primero ingresa por Login.aspx y envía la misma
    /// cookie), así valen las mismas reglas que en las páginas: sesión revocada, permisos al día
    /// y permiso "Gestionar materias".
    /// </summary>
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

        private MateriaDto Convertir(BE.MATERIA m) => new MateriaDto
        {
            Codigo = m.Codigo,
            Nombre = m.Nombre,
            Modalidad = m.Modalidad,
            Peso = m.Peso,
            Correlativas = _bll.ListarCorrelativas(m.Id).OrderBy(c => c.Id).Select(c => c.Codigo).ToArray()
        };

        /// <summary>Mismas comprobaciones que PaginaBase, pero fallando con una excepción en vez de redirigir.</summary>
        private void ExigirPermiso()
        {
            var usuario = Session["Usuario"] as BE.USUARIO;
            if (usuario == null || BLL.PerfilBLL.SesionRevocada(usuario)
                || !new BLL.PerfilBLL().RefrescarPermisosSiHaceFalta(usuario))
                throw new UnauthorizedAccessException("Sesión no iniciada o vencida.");
            if (!usuario.TienePermiso(BE.Permisos.GestionarMaterias))
                throw new UnauthorizedAccessException("No tenés permiso para consultar las materias.");
        }
    }
}
