<%@ Page Title="Calendario" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Calendario.aspx.cs" Inherits="CAPAS_Web.Calendario" %>

<asp:Content ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        .cal-tabla { border-collapse: collapse; width: 100%; }
        .cal-tabla th {
            background: #212529; color: white;
            padding: 10px; text-align: center;
            font-size: .9rem;
        }
        .cal-celda {
            border: 1px solid #dee2e6;
            vertical-align: top;
            min-height: 100px;
            width: 14.28%;
            padding: 6px;
            font-size: .82rem;
        }
        .cal-numero { font-weight: bold; font-size: .95rem; margin-bottom: 3px; }
        .cal-vacio  { background: #f8f9fa; }
        .cal-hoy    { background: #cfe2ff !important; }
        .cal-feriado         { background: #fff3cd; }
        .cal-feriado-nombre  {
            font-size: .7rem; color: #856404;
            background: #fff3cd; border-left: 3px solid #ffc107;
            border-radius: 3px; padding: 2px 4px;
            margin-bottom: 2px; display: block;
        }
        .cal-no-laborable    { background: #e2e3e5; }
        .evento-verde    { background:#d4edda; border-left:4px solid #28a745; border-radius:4px; padding:2px 5px; margin-bottom:2px; display:block; }
        .evento-amarillo { background:#fff3cd; border-left:4px solid #ffc107; border-radius:4px; padding:2px 5px; margin-bottom:2px; display:block; }
        .evento-rojo     { background:#f8d7da; border-left:4px solid #dc3545; border-radius:4px; padding:2px 5px; margin-bottom:2px; display:block; }
    </style>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

    <%-- Navegación mes --%>
    <div class="d-flex justify-content-between align-items-center mb-3">
        <h4><i class="bi bi-calendar3 me-2"></i>Calendario Académico</h4>
        <div class="d-flex gap-2 align-items-center">
            <asp:Button ID="btnAnterior" runat="server" Text="◀ Anterior"
                        CssClass="btn btn-outline-secondary btn-sm" OnClick="btnAnterior_Click"/>
            <asp:Label ID="lblMesAnio" runat="server" CssClass="fw-bold px-3 fs-5"/>
            <asp:Button ID="btnSiguiente" runat="server" Text="Siguiente ▶"
                        CssClass="btn btn-outline-secondary btn-sm" OnClick="btnSiguiente_Click"/>
            <asp:Button ID="btnHoy" runat="server" Text="Hoy"
                        CssClass="btn btn-primary btn-sm" OnClick="btnHoy_Click"/>
        </div>
    </div>

    <%-- Leyenda --%>
    <div class="d-flex flex-wrap gap-3 mb-3 small">
        <span><span class="badge bg-success">●</span> Carga baja (≤5)</span>
        <span><span class="badge bg-warning text-dark">●</span> Carga media (≤10)</span>
        <span><span class="badge bg-danger">●</span> Carga alta (&gt;10)</span>
        <span style="display:inline-block;width:14px;height:14px;background:#cfe2ff;border:1px solid #ccc;vertical-align:middle"></span> Hoy
        <span style="display:inline-block;width:14px;height:14px;background:#fff3cd;border:1px solid #ccc;vertical-align:middle"></span> Feriado
        <span style="display:inline-block;width:14px;height:14px;background:#e2e3e5;border:1px solid #ccc;vertical-align:middle"></span> No laborable
    </div>

    <%-- Calendario --%>
    <div class="table-responsive">
        <asp:Table ID="tblCalendario" runat="server" CssClass="cal-tabla"/>
    </div>

</asp:Content>