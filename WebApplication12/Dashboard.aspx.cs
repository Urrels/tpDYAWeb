using System;
using System.Linq;
using System.Web.Script.Serialization;

namespace CAPAS_Web
{
    public partial class Dashboard : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            if ((Session["Usuario"] as BE.USUARIO).EsAdmin) Response.Redirect("~/Materias.aspx");
            if (!IsPostBack) CargarDashboard();
        }

        private void CargarDashboard()
        {
            var u = Session["Usuario"] as BE.USUARIO;
            var cursadas = new BLL.AlumnoMateriaBLL().Listar(u.Id);

            lblTotalMaterias.Text = cursadas.Count.ToString();
            lblAprobadas.Text = cursadas.Count(am => am.Estado == "Aprobada").ToString();
            lblCursando.Text = cursadas.Count(am => am.Estado == "Cursando").ToString();
            lblFinalPendiente.Text = cursadas.Count(am => am.Estado == "FinalPendiente").ToString();

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