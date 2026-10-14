<%@ Page Title="Idiomas" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Idiomas.aspx.cs" Inherits="CAPAS_Web.Idiomas" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h4 class="mb-3"><i class="bi bi-translate me-2"></i><asp:Literal ID="litTitulo" runat="server" Text="Gestión de idiomas"/></h4>

    <asp:Label ID="lblMsg" runat="server" CssClass="alert d-block mb-3" Visible="false"/>

    <div class="card shadow-sm border-0 mb-4">
        <div class="card-body p-3">
            <h6 class="card-section-title"><asp:Literal ID="litIdiomas" runat="server" Text="Idiomas"/></h6>
            <div class="d-flex flex-wrap align-items-center gap-2 mb-3">
                <asp:TextBox ID="txtNombre" runat="server" MaxLength="50" CssClass="form-control form-control-sm w-auto"/>
                <asp:Button ID="btnAgregar" runat="server" Text="Agregar idioma" CssClass="btn btn-primary btn-sm"
                            OnClick="btnAgregar_Click"/>
                <asp:Button ID="btnRenombrar" runat="server" Text="Renombrar el seleccionado" CssClass="btn btn-outline-secondary btn-sm"
                            OnClick="btnRenombrar_Click"/>
            </div>
            <asp:GridView ID="gvIdiomas" runat="server" CssClass="table table-hover" GridLines="None"
                          AutoGenerateColumns="false" DataKeyNames="Id"
                          OnRowCommand="gvIdiomas_RowCommand" OnRowDataBound="gvIdiomas_RowDataBound">
                <Columns>
                    <asp:BoundField DataField="Nombre" HeaderText="Idioma"/>
                    <asp:BoundField HeaderText="Habilitado"/>
                    <asp:BoundField HeaderText="Predeterminado"/>
                    <asp:TemplateField HeaderText="Acciones" ItemStyle-Width="380">
                        <ItemTemplate>
                            <asp:LinkButton ID="lnkSeleccionar" runat="server" CommandName="Seleccionar"
                                            CommandArgument='<%# Eval("Id") %>' CssClass="btn btn-info btn-sm me-1"/>
                            <asp:LinkButton ID="lnkAlternar" runat="server" CommandName="Alternar"
                                            CommandArgument='<%# Eval("Id") %>' CssClass="btn btn-warning btn-sm me-1"/>
                            <asp:LinkButton ID="lnkEliminar" runat="server" CommandName="Eliminar"
                                            CommandArgument='<%# Eval("Id") %>' CssClass="btn btn-danger btn-sm"/>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </div>

    <asp:Panel ID="pnlTraducciones" runat="server" Visible="false" CssClass="card shadow-sm border-0 mb-4">
        <div class="card-body p-3">
            <h6 class="card-section-title">
                <asp:Literal ID="litTraducciones" runat="server" Text="Traducciones del idioma seleccionado"/>:
                <asp:Label ID="lblIdiomaSeleccionado" runat="server" CssClass="fw-bold"/>
            </h6>
            <div class="d-flex flex-wrap align-items-center gap-2 mb-3">
                <asp:TextBox ID="txtFiltro" runat="server" CssClass="form-control form-control-sm w-auto"/>
                <asp:Button ID="btnFiltrar" runat="server" Text="Filtrar por clave" CssClass="btn btn-outline-secondary btn-sm"
                            OnClick="btnFiltrar_Click"/>
                <asp:Button ID="btnGuardar" runat="server" Text="Guardar traducciones" CssClass="btn btn-success btn-sm"
                            OnClick="btnGuardar_Click"/>
            </div>
            <asp:GridView ID="gvTraducciones" runat="server" CssClass="table table-sm" GridLines="None"
                          AutoGenerateColumns="false" DataKeyNames="Id">
                <Columns>
                    <asp:BoundField DataField="Clave" HeaderText="Clave"/>
                    <asp:BoundField DataField="TextoDefault" HeaderText="Texto por defecto"/>
                    <asp:TemplateField HeaderText="Traducción">
                        <ItemTemplate>
                            <asp:TextBox ID="txtTraduccion" runat="server" MaxLength="500" CssClass="form-control form-control-sm"
                                         Text='<%# Eval("TextoTraduccion") %>'/>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </asp:Panel>
</asp:Content>
