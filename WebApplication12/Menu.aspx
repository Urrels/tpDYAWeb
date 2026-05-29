<%@ Page Title="Inicio" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Menu.aspx.cs" Inherits="CAPAS_Web.Menu" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h4 class="mb-4">Bienvenido, <strong><asp:Label ID="lblNombre" runat="server"/></strong> 👋</h4>

    <%-- Alerta de clima RF08 --%>
    <asp:Panel ID="pnlAlerta" runat="server" Visible="false"
               CssClass="alert alert-info d-flex gap-3 align-items-center mb-4">
        <i class="bi bi-cloud-sun-fill fs-3"></i>
        <div><strong>Clima hoy:</strong> <asp:Label ID="lblClima" runat="server"/></div>
    </asp:Panel>
    <%-- Alerta exámenes próximos --%>
<asp:Panel ID="pnlExamenes" runat="server" Visible="false"
           CssClass="alert alert-danger d-flex gap-3 align-items-start mb-4">
    <i class="bi bi-alarm-fill fs-3"></i>
    <div>
        <strong>⚠️ Exámenes próximos:</strong>
        <asp:Repeater ID="rptExamenes" runat="server">
            <ItemTemplate>
                <div class="mt-1">
                    📅 <strong><%# Eval("NombreMateria") %></strong> —
                    <%# Eval("Tipo") %> el
                    <%# ((DateTime)Eval("Fecha")).ToString("dd/MM/yyyy") %>
                    (<strong><%# ((DateTime)Eval("Fecha") - DateTime.Today).Days %> días</strong>)
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>
</asp:Panel>
    <%-- Promedio ponderado --%>
    <div class="card border-0 shadow-sm mb-4">
        <div class="card-body p-4">
            <div class="row align-items-center g-3">
                <div class="col-md-2 text-center">
                    <div style="font-size:3rem; font-weight:bold; color:#0d6efd;">
                        <asp:Label ID="lblPromedio" runat="server" Text="--"/>
                    </div>
                    <div class="text-muted small">Promedio Ponderado</div>
                </div>
                <div class="col-md-7">
                    <div class="progress mb-2" style="height:20px">
                        <asp:Panel ID="pnlBarra" runat="server"
                                   CssClass="progress-bar"
                                   role="progressbar"
                                   style="width:0%"/>
                    </div>
                    <asp:Label ID="lblMensajePromedio" runat="server" CssClass="small"/>
                </div>
                <div class="col-md-3 text-center">
                    <div class="text-muted small mb-1">Objetivo: 7.00</div>
                    <asp:Label ID="lblDiferenciaObjetivo" runat="server" CssClass="fw-bold"/>
                </div>
            </div>
        </div>
    </div>

    <%-- Cards de navegación --%>
    <div class="row g-4">
        <div class="col-md-4">
            <div class="card h-100 shadow-sm border-0 text-center p-4">
                <i class="bi bi-book-fill text-primary" style="font-size:2.5rem"></i>
                <h5 class="mt-3">Materias</h5>
                <p class="text-muted small">ABM de asignaturas y correlativas.</p>
                <a href="Materias.aspx" class="btn btn-primary btn-sm">Gestionar</a>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card h-100 shadow-sm border-0 text-center p-4">
                <i class="bi bi-journal-check text-success" style="font-size:2.5rem"></i>
                <h5 class="mt-3">Mis Cursadas</h5>
                <p class="text-muted small">Estado y notas de cada materia.</p>
                <a href="MisCursadas.aspx" class="btn btn-success btn-sm">Ver</a>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card h-100 shadow-sm border-0 text-center p-4">
                <i class="bi bi-calendar3 text-warning" style="font-size:2.5rem"></i>
                <h5 class="mt-3">Calendario</h5>
                <p class="text-muted small">Eventos con mapa de calor.</p>
                <a href="Calendario.aspx" class="btn btn-warning btn-sm">Ver</a>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card h-100 shadow-sm border-0 text-center p-4">
                <i class="bi bi-lightbulb-fill text-info" style="font-size:2.5rem"></i>
                <h5 class="mt-3">Recomendador</h5>
                <p class="text-muted small">Materias para el próximo cuatrimestre.</p>
                <a href="Recomendador.aspx" class="btn btn-info btn-sm">Ver</a>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card h-100 shadow-sm border-0 text-center p-4">
                <i class="bi bi-calculator text-primary" style="font-size:2.5rem"></i>
                <h5 class="mt-3">Simulador</h5>
                <p class="text-muted small">¿Qué pasa si mejorás una nota?</p>
                <a href="Simulador.aspx" class="btn btn-primary btn-sm">Simular</a>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card h-100 shadow-sm border-0 text-center p-4">
                <i class="bi bi-person-fill text-secondary" style="font-size:2.5rem"></i>
                <h5 class="mt-3">Mi Perfil</h5>
                <p class="text-muted small">Datos y dirección encriptada AES.</p>
                <a href="Perfil.aspx" class="btn btn-secondary btn-sm">Editar</a>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card h-100 shadow-sm border-0 text-center p-4">
                <i class="bi bi-clipboard-data text-danger" style="font-size:2.5rem"></i>
                <h5 class="mt-3">Bitácora</h5>
                <p class="text-muted small">Registro de acciones del sistema.</p>
                <a href="Bitacora.aspx" class="btn btn-danger btn-sm">Ver</a>
            </div>
        </div>
        <div class="col-md-4">
    <div class="card h-100 shadow-sm border-0 text-center p-4">
        <i class="bi bi-bar-chart-fill text-primary" style="font-size:2.5rem"></i>
        <h5 class="mt-3">Dashboard</h5>
        <p class="text-muted small">Gráficos y resumen visual de tu situación.</p>
        <a href="Dashboard.aspx" class="btn btn-primary btn-sm">Ver</a>
    </div>
</div>
    </div>
</asp:Content>