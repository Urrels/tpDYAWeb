<%@ Page Title="Bitácora" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Bitacora.aspx.cs" Inherits="CAPAS_Web.Bitacora" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h4 class="mb-4"><i class="bi bi-clipboard-data me-2"></i>Bitácora del Sistema</h4>

    <div class="card shadow-sm border-0 mb-4">
        <div class="card-body p-4">
            <h6 class="card-section-title mb-3">Filtros</h6>
            <div class="row g-3 align-items-end">
                <div class="col-md-3">
                    <label class="form-label">Usuario</label>
                    <asp:DropDownList ID="ddlUsuario" runat="server" CssClass="form-select"/>
                </div>
                <div class="col-md-3">
                    <label class="form-label">Criticidad</label>
                    <asp:DropDownList ID="ddlCriticidad" runat="server" CssClass="form-select">
                        <asp:ListItem Value="" Text="Todas"/>
                        <asp:ListItem Value="Baja" Text="Baja"/>
                        <asp:ListItem Value="Media" Text="Media"/>
                        <asp:ListItem Value="Alta" Text="Alta"/>
                    </asp:DropDownList>
                </div>
                <div class="col-md-2">
                    <label class="form-label">Fecha desde</label>
                    <asp:TextBox ID="txtFechaDesde" runat="server" CssClass="form-control" TextMode="Date"/>
                </div>
                <div class="col-md-2">
                    <label class="form-label">Fecha hasta</label>
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
                            <%# Eval("Criticidad") %>
                        </span>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>

    <div class="d-flex justify-content-between align-items-center mt-3">
        <asp:Button ID="btnAnterior" runat="server" Text="Anterior" CausesValidation="false"
                    CssClass="btn btn-outline-secondary btn-sm" OnClick="btnAnterior_Click"/>
        <asp:Label ID="lblPagina" runat="server" CssClass="text-muted small"/>
        <asp:Button ID="btnSiguiente" runat="server" Text="Siguiente" CausesValidation="false"
                    CssClass="btn btn-outline-secondary btn-sm" OnClick="btnSiguiente_Click"/>
    </div>
</asp:Content>
