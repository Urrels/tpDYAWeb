<%@ Page Title="Bitácora" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Bitacora.aspx.cs" Inherits="CAPAS_Web.Bitacora" %>
<%@ Register Src="~/Paginador.ascx" TagPrefix="uc" TagName="Paginador" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h4 class="mb-4"><i class="bi bi-clipboard-data me-2"></i><asp:Literal ID="litBitacoraDelSistema" runat="server" Text="Bitácora del Sistema"/></h4>

    <div class="card shadow-sm border-0 mb-4">
        <div class="card-body p-4">
            <h6 class="card-section-title mb-3"><asp:Literal ID="litFiltros" runat="server" Text="Filtros"/></h6>
            <div class="row g-3 align-items-end">
                <div class="col-md-3">
                    <label class="form-label"><asp:Literal ID="litUsuario" runat="server" Text="Usuario"/></label>
                    <asp:DropDownList ID="ddlUsuario" runat="server" CssClass="form-select"/>
                </div>
                <div class="col-md-3">
                    <label class="form-label"><asp:Literal ID="litCriticidad" runat="server" Text="Criticidad"/></label>
                    <asp:DropDownList ID="ddlCriticidad" runat="server" CssClass="form-select">
                        <asp:ListItem Value="" Text="Todas"/>
                        <asp:ListItem Value="Baja" Text="Baja"/>
                        <asp:ListItem Value="Media" Text="Media"/>
                        <asp:ListItem Value="Alta" Text="Alta"/>
                    </asp:DropDownList>
                </div>
                <div class="col-md-2">
                    <label class="form-label"><asp:Literal ID="litFechaDesde" runat="server" Text="Fecha desde"/></label>
                    <asp:TextBox ID="txtFechaDesde" runat="server" CssClass="form-control" TextMode="Date"/>
                </div>
                <div class="col-md-2">
                    <label class="form-label"><asp:Literal ID="litFechaHasta" runat="server" Text="Fecha hasta"/></label>
                    <asp:TextBox ID="txtFechaHasta" runat="server" CssClass="form-control" TextMode="Date"/>
                </div>
                <div class="col-md-2 d-flex gap-2">
                    <asp:Button ID="btnFiltrar" runat="server" Text="Filtrar"
                                CssClass="btn btn-primary flex-fill" OnClick="btnFiltrar_Click"/>
                    <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar filtros" CausesValidation="false"
                                CssClass="btn btn-outline-secondary flex-fill" OnClick="btnLimpiar_Click"/>
                </div>
            </div>
        </div>
    </div>

    <div class="card shadow-sm border-0 table-card">
        <asp:GridView ID="gvBitacora" runat="server"
                      CssClass="table table-hover table-striped"
                      AutoGenerateColumns="false"
                      HeaderStyle-CssClass=""
                      GridLines="None">
            <Columns>
                <asp:BoundField DataField="Id"      HeaderText="ID"          ItemStyle-Width="60"/>
                <asp:BoundField DataField="Usuario" HeaderText="Usuario"/>
                <asp:BoundField DataField="Accion"  HeaderText="Acción"/>
                <asp:BoundField DataField="Fecha"   HeaderText="Fecha y Hora"
                                DataFormatString="{0:dd/MM/yyyy HH:mm:ss}"/>
                <asp:TemplateField HeaderText="Criticidad" ItemStyle-Width="100">
                    <ItemTemplate>
                        <span class='badge <%# GetCriticidadCssClass((string)Eval("Criticidad")) %>'>
                            <%# CAPAS_Web.Textos.ValorH(Eval("Criticidad")) %>
                        </span>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>

    <uc:Paginador ID="paginador" runat="server" TamanioPagina="50" OnCambioPagina="paginador_CambioPagina"/>
</asp:Content>
