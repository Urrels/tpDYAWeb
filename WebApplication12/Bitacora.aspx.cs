using System;

namespace CAPAS_Web
{
    public partial class Bitacora : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) Response.Redirect("~/Login.aspx");
            var u = Session["Usuario"] as BE.USUARIO;
            if (!u.EsAdmin) Response.Redirect("~/Menu.aspx");
            if (!IsPostBack)
            {
                gvBitacora.DataSource = new BLL.BitacoraBLL().Listar();
                gvBitacora.DataBind();
            }
        }
    }
}