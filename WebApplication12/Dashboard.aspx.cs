using System;
using System.Linq;
using System.Web.Script.Serialization;

namespace CAPAS_Web
{
    public partial class Dashboard : PaginaBase
    {
        protected override string PermisoRequerido => BE.Permisos.VerDashboard;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) CargarDashboard();
        }

        private void CargarDashboard()
        {
            var u = Session["Usuario"] as BE.USUARIO;
            var bll = new BLL.AlumnoMateriaBLL();
            var cursadas = bll.Listar(u.Id);
            var resumen = bll.ObtenerResumen(u.Id);

            lblTotalMaterias.Text = resumen.TotalMaterias.ToString();
            lblAprobadas.Text = resumen.Aprobadas.ToString();
            lblCursando.Text = resumen.Cursando.ToString();
            lblFinalPendiente.Text = resumen.FinalPendiente.ToString();

            var serial = new JavaScriptSerializer();

            var datosNotas = new
            {
                labels = cursadas.Select(am => am.CodigoMateria).ToArray(),
                parcial1 = cursadas.Select(am => am.NotaParcial1 ?? 0).ToArray(),
                parcial2 = cursadas.Select(am => am.NotaParcial2 ?? 0).ToArray(),
                final = cursadas.Select(am => am.NotaFinal ?? 0).ToArray()
            };
            hfDatosNotas.Value = serial.Serialize(datosNotas);

            var datosEstados = new
            {
                valores = new[]
                {
                    cursadas.Count(am => am.Estado == "Aprobada"),
                    cursadas.Count(am => am.Estado == "Cursando"),
                    cursadas.Count(am => am.Estado == "FinalPendiente")
                }
            };
            hfDatosEstados.Value = serial.Serialize(datosEstados);
        }
    }
}