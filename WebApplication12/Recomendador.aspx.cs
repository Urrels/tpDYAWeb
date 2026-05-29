using System;
using System.Linq;

namespace CAPAS_Web
{
    public partial class Recomendador : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            if (!IsPostBack) Cargar();
        }

        private void Cargar()
        {
            int id = (Session["Usuario"] as BE.USUARIO).Id;
            var cursadas = new BLL.AlumnoMateriaBLL().Listar(id);
            var recomendadas = new BLL.MateriaBLL().RecomendarMaterias(cursadas);

            rptRecomendadas.DataSource = recomendadas;
            rptRecomendadas.DataBind();
            pnlVacio.Visible = recomendadas.Count == 0;

            gvAprobadas.DataSource = cursadas.Where(am => am.Estado == "Aprobada").ToList();
            gvAprobadas.DataBind();
        }
    }
}