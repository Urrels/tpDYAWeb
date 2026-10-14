<%@ Page Title="Asignar perfiles" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="AsignarPerfiles.aspx.cs" Inherits="CAPAS_Web.AsignarPerfiles" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h4 class="mb-4"><i class="bi bi-people-fill me-2"></i>Asignar perfiles a usuarios</h4>

    <asp:Label ID="lblMsg" runat="server" CssClass="alert d-block mb-3" Visible="false"/>

    <div class="card shadow-sm border-0">
        <div class="card-body">
            <label class="form-label" for="<%= ddlUsuario.ClientID %>">Usuario</label>
            <asp:DropDownList ID="ddlUsuario" runat="server" CssClass="form-select mb-3" AutoPostBack="true"
                              OnSelectedIndexChanged="ddlUsuario_SelectedIndexChanged"/>

            <h6>Perfiles asignados</h6>
            <asp:CheckBoxList ID="cblRoles" runat="server" CssClass="mb-2" RepeatColumns="1" CellPadding="2"/>
            <asp:Button ID="btnGuardar" runat="server" Text="Guardar asignación"
                        CssClass="btn btn-primary btn-sm" OnClick="btnGuardar_Click"/>

            <hr/>
            <h6>Permisos efectivos del usuario <span class="text-muted small">(según lo guardado)</span></h6>
            <asp:BulletedList ID="blEfectivos" runat="server" CssClass="small"/>
            <asp:Label ID="lblSinPermisos" runat="server" Visible="false"
                       Text="<p class='text-muted small'>El usuario no tiene permisos.</p>"/>
        </div>
    </div>
</asp:Content>
