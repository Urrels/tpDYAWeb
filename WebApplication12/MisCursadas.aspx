<%@ Page Title="Mis Cursadas" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="MisCursadas.aspx.cs" Inherits="CAPAS_Web.MisCursadas" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="mb-3">
        <h4><i class="bi bi-journal-check me-2"></i><asp:Literal ID="litMisCursadas" runat="server" Text="Mis Cursadas"/></h4>
    </div>

    <div class="card shadow-sm border-0 table-card">
   <asp:GridView ID="gvCursadas" runat="server"
              CssClass="table table-hover"
              AutoGenerateColumns="false"
              HeaderStyle-CssClass=""
              DataKeyNames="Id"
              GridLines="None">
    <Columns>
        <asp:BoundField DataField="CodigoMateria"     HeaderText="Código"      ItemStyle-Width="80"/>
        <asp:BoundField DataField="NombreMateria"     HeaderText="Materia"/>
        <asp:TemplateField HeaderText="Modalidad" ItemStyle-Width="110"><ItemTemplate><%# CAPAS_Web.Textos.ValorH(Eval("Modalidad")) %></ItemTemplate></asp:TemplateField>
        <asp:TemplateField HeaderText="Estado" ItemStyle-Width="130"><ItemTemplate><%# CAPAS_Web.Textos.ValorH(Eval("Estado")) %></ItemTemplate></asp:TemplateField>
        <asp:BoundField DataField="NotaParcial1"      HeaderText="Parcial 1"   ItemStyle-Width="80"/>
        <asp:BoundField DataField="NotaParcial2"      HeaderText="Parcial 2"   ItemStyle-Width="80"/>
        <asp:BoundField DataField="NotaRecuperatorio" HeaderText="Recup."      ItemStyle-Width="80"/>
        <asp:BoundField DataField="NotaFinal"         HeaderText="Final"       ItemStyle-Width="80"/>
        <asp:BoundField DataField="DVH"               HeaderText="DVH"         ItemStyle-Width="60"/>
        <asp:TemplateField HeaderText="Riesgo" ItemStyle-Width="80"><ItemTemplate><%# CAPAS_Web.Textos.ValorH(Eval("NivelRiesgo")) %></ItemTemplate></asp:TemplateField>
<asp:TemplateField HeaderText="Situación" ItemStyle-Width="300">
    <ItemTemplate>
        <span class='<%# ObtenerCssRiesgo(Eval("NivelRiesgo").ToString()) %>'>
            <%# CAPAS_Web.Textos.MH((string)Eval("MensajeRiesgo")) %>
        </span>
    </ItemTemplate>
</asp:TemplateField>
    </Columns>
</asp:GridView>
    </div>

</asp:Content>
