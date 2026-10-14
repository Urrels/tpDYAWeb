<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Paginador.ascx.cs" Inherits="CAPAS_Web.Paginador" %>

<div class="d-flex justify-content-between align-items-center mt-3">
    <div class="d-flex gap-2">
        <asp:Button ID="btnPrimera" runat="server" Text="Primera" CausesValidation="false"
                    CssClass="btn btn-outline-secondary btn-sm" OnClick="btnPrimera_Click"/>
        <asp:Button ID="btnAnterior" runat="server" Text="Anterior" CausesValidation="false"
                    CssClass="btn btn-outline-secondary btn-sm" OnClick="btnAnterior_Click"/>
    </div>
    <asp:Label ID="lblPagina" runat="server" CssClass="text-muted small"/>
    <div class="d-flex gap-2">
        <asp:Button ID="btnSiguiente" runat="server" Text="Siguiente" CausesValidation="false"
                    CssClass="btn btn-outline-secondary btn-sm" OnClick="btnSiguiente_Click"/>
        <asp:Button ID="btnUltima" runat="server" Text="Última" CausesValidation="false"
                    CssClass="btn btn-outline-secondary btn-sm" OnClick="btnUltima_Click"/>
    </div>
</div>
