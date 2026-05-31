<%@ Page Title="Eventos" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Eventos.aspx.cs" Inherits="CAPAS_Web.Eventos" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="d-flex justify-content-between align-items-center mb-3">
        <h4><i class="bi bi-calendar-event me-2"></i>Eventos Académicos</h4>
        <asp:Button ID="btnNuevo" runat="server" Text="+ Nuevo Evento"
                    CssClass="btn btn-primary btn-sm" OnClick="btnNuevo_Click"/>
    </div>

    <asp:Label ID="lblMsg" runat="server" CssClass="alert d-block mb-3" Visible="false"/>

    <asp:Panel ID="pnlForm" runat="server" Visible="false" CssClass="card shadow-sm border-0 mb-4">
        <div class="card-body p-4">
            <h6 class="card-section-title"><asp:Label ID="lblTituloForm" runat="server" Text="Nuevo Evento"/></h6>
            <asp:HiddenField ID="hfIdEvento" runat="server" Value="0"/>
            <div class="row g-3">
                <div class="col-md-4">
                    <label class="form-label">Materia</label>
                    <asp:DropDownList ID="ddlMateria" runat="server" CssClass="form-select"/>
                </div>
                <div class="col-md-3">
                    <label class="form-label">Tipo</label>
                    <asp:DropDownList ID="ddlTipo" runat="server" CssClass="form-select">
                        <asp:ListItem Value="Parcial">Parcial</asp:ListItem>
                        <asp:ListItem Value="Final">Final</asp:ListItem>
                        <asp:ListItem Value="TP">TP / Entrega</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="col-md-3">
                    <label class="form-label">Fecha</label>
                    <asp:TextBox ID="txtFecha" runat="server" CssClass="form-control" TextMode="DateTimeLocal"/>
                </div>
                <div class="col-md-2">
                    <label class="form-label">Peso (1-5)</label>
                    <asp:DropDownList ID="ddlPeso" runat="server" CssClass="form-select">
                        <asp:ListItem Value="1">1</asp:ListItem>
                        <asp:ListItem Value="2">2</asp:ListItem>
                        <asp:ListItem Value="3" Selected="True">3</asp:ListItem>
                        <asp:ListItem Value="4">4</asp:ListItem>
                        <asp:ListItem Value="5">5</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="col-12">
                    <label class="form-label">Descripción</label>
                    <asp:TextBox ID="txtDescripcion" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="2"/>
                </div>
            </div>
            <div class="mt-3 d-flex gap-2">
                <asp:Button ID="btnGuardar" runat="server" Text="Guardar"
                            CssClass="btn btn-success" OnClick="btnGuardar_Click"/>
                <asp:Button ID="btnCancelar" runat="server" Text="Cancelar"
                            CssClass="btn btn-secondary" OnClick="btnCancelar_Click"/>
            </div>
        </div>
    </asp:Panel>

    <div class="card shadow-sm border-0 table-card">
        <asp:GridView ID="gvEventos" runat="server"
                      CssClass="table table-hover"
                      AutoGenerateColumns="false"
                      HeaderStyle-CssClass=""
                      DataKeyNames="Id"
                      OnRowCommand="gvEventos_RowCommand"
                      GridLines="None">
            <Columns>
                <asp:BoundField DataField="NombreMateria" HeaderText="Materia"/>
                <asp:BoundField DataField="Tipo"          HeaderText="Tipo"   ItemStyle-Width="90"/>
                <asp:BoundField DataField="Descripcion"   HeaderText="Descripción"/>
                <asp:BoundField DataField="Fecha"         HeaderText="Fecha"
                                DataFormatString="{0:dd/MM/yyyy HH:mm}" ItemStyle-Width="150"/>
                <asp:BoundField DataField="Peso"          HeaderText="Peso"   ItemStyle-Width="60"/>
                <asp:TemplateField HeaderText="Acciones"  ItemStyle-Width="150">
                    <ItemTemplate>
                        <asp:LinkButton CommandName="Editar"   CommandArgument='<%# Eval("Id") %>'
                                        runat="server" CssClass="btn btn-warning btn-sm me-1">
                            <i class="bi bi-pencil"></i>
                        </asp:LinkButton>
                        <asp:LinkButton CommandName="Eliminar" CommandArgument='<%# Eval("Id") %>'
                                        runat="server" CssClass="btn btn-danger btn-sm"
                                        OnClientClick="return confirm('¿Eliminar evento?')">
                            <i class="bi bi-trash"></i>
                        </asp:LinkButton>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>
