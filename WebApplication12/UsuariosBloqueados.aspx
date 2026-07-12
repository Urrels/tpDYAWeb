<%@ Page Title="Usuarios Bloqueados" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="UsuariosBloqueados.aspx.cs" Inherits="CAPAS_Web.UsuariosBloqueados" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h4 class="mb-4"><i class="bi bi-unlock-fill me-2"></i>Usuarios Bloqueados</h4>

    <asp:Label ID="lblMsg" runat="server" CssClass="alert d-block mb-3" Visible="false"/>

    <div class="card shadow-sm border-0 table-card">
        <asp:Repeater ID="rptBloqueados" runat="server" OnItemCommand="rptBloqueados_ItemCommand">
            <HeaderTemplate>
                <table class="table table-hover mb-0">
                    <thead>
                        <tr>
                            <th>Usuario</th>
                            <th>Intentos Fallidos</th>
                            <th>Acción</th>
                        </tr>
                    </thead>
                    <tbody>
            </HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td><%# Eval("Usuario") %></td>
                    <td><%# Eval("IntentosFallidos") %></td>
                    <td>
                        <asp:LinkButton CommandName="Desbloquear" CommandArgument='<%# Eval("Usuario") %>'
                                        runat="server" CssClass="btn btn-success btn-sm"
                                        OnClientClick="return confirm('¿Desbloquear este usuario?');">
                            <i class="bi bi-unlock-fill"></i> Desbloquear
                        </asp:LinkButton>
                    </td>
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                    </tbody>
                </table>
            </FooterTemplate>
        </asp:Repeater>
        <asp:Label ID="lblSinBloqueados" runat="server" Visible="false"
                   Text="<p class='text-muted small p-3 mb-0'>No hay usuarios bloqueados actualmente.</p>"/>
    </div>
</asp:Content>
