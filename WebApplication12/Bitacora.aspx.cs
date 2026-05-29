using System;

namespace CAPAS_Web
{
    public partial class Bitacora : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            if (!IsPostBack)
            {
                gvBitacora.DataSource = new BLL.BitacoraBLL().Listar();
                gvBitacora.DataBind();
            }
        }
    }
}