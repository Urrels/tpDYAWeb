<%@ Page Title="Materias" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Materias.aspx.cs" Inherits="CAPAS_Web.Materias" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="d-flex justify-content-between align-items-center mb-3">
        <h4><i class="bi bi-book-fill me-2"></i>Gestión de Materias</h4>
        <asp:Button ID="btnNueva" runat="server" Text="+ Nueva Materia"
                    CssClass="btn btn-primary btn-sm" OnClick="btnNueva_Click"/>
    </div>

    <asp:Label ID="lblMsg" runat="server" CssClass="alert d-block mb-3" Visible="false"/>

    <asp:Panel ID="pnlForm" runat="server" Visible="false" CssClass="card shadow-sm border-0 mb-4">
        <div class="card-body p-4">
            <h6 class="card-section-title"><asp:Label ID="lblTituloForm" runat="server" Text="Nueva Materia"/></h6>
            <asp:HiddenField ID="hfIdMateria" runat="server" Value="0"/>
            <div class="row g-3">
                <div class="col-md-6">
                    <label class="form-label">Nombre</label>
                    <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control"/>
                </div>
                <div class="col-md-3">
                    <label class="form-label">Código</label>
                    <asp:TextBox ID="txtCodigo" runat="server" CssClass="form-control"/>
                </div>
                <div class="col-md-3">
                    <label class="form-label">Modalidad</label>
                    <asp:DropDownList ID="ddlModalidad" runat="server" CssClass="form-select">
                        <asp:ListItem Value="Presencial">Presencial</asp:ListItem>
                        <asp:ListItem Value="Virtual">Virtual</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="col-md-3">
                    <label class="form-label">Peso (complejidad 1-5)</label>
                    <asp:DropDownList ID="ddlPeso" runat="server" CssClass="form-select">
                        <asp:ListItem Value="1">1 - Muy baja</asp:ListItem>
                        <asp:ListItem Value="2">2 - Baja</asp:ListItem>
                        <asp:ListItem Value="3" Selected="True">3 - Media</asp:ListItem>
                        <asp:ListItem Value="4">4 - Alta</asp:ListItem>
                        <asp:ListItem Value="5">5 - Muy alta</asp:ListItem>
                    </asp:DropDownList>
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
        <asp:GridView ID="gvMaterias" runat="server"
                      CssClass="table table-hover"
                      AutoGenerateColumns="false"
                      HeaderStyle-CssClass=""
                      DataKeyNames="Id"
                      OnRowCommand="gvMaterias_RowCommand"
                      GridLines="None">
            <Columns>
                <asp:BoundField DataField="Codigo"    HeaderText="Código"    ItemStyle-Width="80"/>
                <asp:BoundField DataField="Nombre"    HeaderText="Nombre"/>
                <asp:BoundField DataField="Modalidad" HeaderText="Modalidad" ItemStyle-Width="120"/>
                <asp:BoundField DataField="Peso"      HeaderText="Peso"      ItemStyle-Width="60"/>
                <asp:TemplateField HeaderText="Acciones" ItemStyle-Width="200">
                    <ItemTemplate>
                        <asp:LinkButton CommandName="Editar" CommandArgument='<%# Eval("Id") %>'
                                        runat="server" CssClass="btn btn-warning btn-sm me-1">
                            <i class="bi bi-pencil"></i> Editar
                        </asp:LinkButton>
                        <asp:LinkButton CommandName="Eliminar" CommandArgument='<%# Eval("Id") %>'
                                        runat="server" CssClass="btn btn-danger btn-sm"
                                        OnClientClick="return confirm('¿Eliminar esta materia?')">
                            <i class="bi bi-trash"></i> Baja
                        </asp:LinkButton>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>
