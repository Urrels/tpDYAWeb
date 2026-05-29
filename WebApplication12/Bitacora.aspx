<%@ Page Title="Bitácora" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Bitacora.aspx.cs" Inherits="CAPAS_Web.Bitacora" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="d-flex justify-content-between align-items-center mb-3">
        <h4><i class="bi bi-clipboard-data me-2"></i>Bitácora del Sistema</h4>
    </div>

    <asp:GridView ID="gvBitacora" runat="server"
                  CssClass="table table-striped table-bordered table-hover"
                  AutoGenerateColumns="false"
                  HeaderStyle-CssClass="table-dark"
                  GridLines="None">
        <Columns>
            <asp:BoundField DataField="Id"      HeaderText="ID"         ItemStyle-Width="60"/>
            <asp:BoundField DataField="Usuario" HeaderText="Usuario"/>
            <asp:BoundField DataField="Accion"  HeaderText="Acción"/>
            <asp:BoundField DataField="Fecha"   HeaderText="Fecha y Hora"
                            DataFormatString="{0:dd/MM/yyyy HH:mm:ss}"/>
        </Columns>
    </asp:GridView>
</asp:Content>
