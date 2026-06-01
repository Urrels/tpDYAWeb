<%@ Page Title="Bitácora" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Bitacora.aspx.cs" Inherits="CAPAS_Web.Bitacora" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h4 class="mb-4"><i class="bi bi-clipboard-data me-2"></i>Bitácora del Sistema</h4>

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
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>
