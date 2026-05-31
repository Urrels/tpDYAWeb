<%@ Page Title="Inicio" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Menu.aspx.cs" Inherits="CAPAS_Web.Menu" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h4 class="mb-4">Bienvenido, <strong><asp:Label ID="lblNombre" runat="server"/></strong> 👋</h4>

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
    <div class="card border-0 shadow-sm mb-4 promedio-card">
        <div class="card-body p-4">
            <div class="row align-items-center g-3">
                <div class="col-md-2 text-center">
                    <div class="hero-number">
                        <asp:Label ID="lblPromedio" runat="server" Text="--"/>
                    </div>
                    <div class="text-muted small">Promedio Ponderado</div>
                </div>
                <div class="col-md-7">
                    <div class="progress mb-2" style="height:12px; border-radius:6px;">
                        <asp:Panel ID="pnlBarra" runat="server"
                                   CssClass="progress-bar bg-primary"
                                   role="progressbar"
                                   style="width:0%"/>
                    </div>
                    <asp:Label ID="lblMensajePromedio" runat="server" CssClass="small text-muted"/>
                </div>
                <div class="col-md-3 text-center">
                    <div class="text-muted small mb-1">Objetivo: 7.00</div>
                    <asp:Label ID="lblDiferenciaObjetivo" runat="server" CssClass="fw-bold"/>
                </div>
            </div>
        </div>
    </div>

    <div class="row g-4">
        <div class="col-md-4">
            <div class="card h-100 shadow-sm border-0 nav-card">
                <div class="card-body d-flex flex-column align-items-center text-center p-4">
                    <div class="nav-card-icon nav-card-icon--primary mb-3">
                        <i class="bi bi-pencil-square"></i>
                    </div>
                    <h5 class="fw-semibold mb-2">Inscripción</h5>
                    <p class="text-muted small mb-3">Inscribite al cuatrimestre en curso.</p>
                    <a href="Inscripcion.aspx" class="btn btn-primary btn-sm mt-auto w-100">Inscribirme</a>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card h-100 shadow-sm border-0 nav-card">
                <div class="card-body d-flex flex-column align-items-center text-center p-4">
                    <div class="nav-card-icon nav-card-icon--primary mb-3">
                        <i class="bi bi-book-fill"></i>
                    </div>
                    <h5 class="fw-semibold mb-2">Materias</h5>
                    <p class="text-muted small mb-3">ABM de asignaturas y correlativas.</p>
                    <a href="Materias.aspx" class="btn btn-primary btn-sm mt-auto w-100">Gestionar</a>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card h-100 shadow-sm border-0 nav-card">
                <div class="card-body d-flex flex-column align-items-center text-center p-4">
                    <div class="nav-card-icon nav-card-icon--success mb-3">
                        <i class="bi bi-journal-check"></i>
                    </div>
                    <h5 class="fw-semibold mb-2">Mis Cursadas</h5>
                    <p class="text-muted small mb-3">Estado y notas de cada materia.</p>
                    <a href="MisCursadas.aspx" class="btn btn-success btn-sm mt-auto w-100">Ver</a>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card h-100 shadow-sm border-0 nav-card">
                <div class="card-body d-flex flex-column align-items-center text-center p-4">
                    <div class="nav-card-icon nav-card-icon--warning mb-3">
                        <i class="bi bi-calendar3"></i>
                    </div>
                    <h5 class="fw-semibold mb-2">Calendario</h5>
                    <p class="text-muted small mb-3">Eventos con mapa de calor.</p>
                    <a href="Calendario.aspx" class="btn btn-warning btn-sm mt-auto w-100">Ver</a>
                </div>
            </div>
        </div>


        <div class="col-md-4">
            <div class="card h-100 shadow-sm border-0 nav-card">
                <div class="card-body d-flex flex-column align-items-center text-center p-4">
                    <div class="nav-card-icon nav-card-icon--secondary mb-3">
                        <i class="bi bi-person-fill"></i>
                    </div>
                    <h5 class="fw-semibold mb-2">Mi Perfil</h5>
                    <p class="text-muted small mb-3">Datos y dirección encriptada AES.</p>
                    <a href="Perfil.aspx" class="btn btn-secondary btn-sm mt-auto w-100">Editar</a>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card h-100 shadow-sm border-0 nav-card">
                <div class="card-body d-flex flex-column align-items-center text-center p-4">
                    <div class="nav-card-icon nav-card-icon--danger mb-3">
                        <i class="bi bi-clipboard-data"></i>
                    </div>
                    <h5 class="fw-semibold mb-2">Bitácora</h5>
                    <p class="text-muted small mb-3">Registro de acciones del sistema.</p>
                    <a href="Bitacora.aspx" class="btn btn-danger btn-sm mt-auto w-100">Ver</a>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card h-100 shadow-sm border-0 nav-card">
                <div class="card-body d-flex flex-column align-items-center text-center p-4">
                    <div class="nav-card-icon nav-card-icon--primary mb-3">
                        <i class="bi bi-bar-chart-fill"></i>
                    </div>
                    <h5 class="fw-semibold mb-2">Dashboard</h5>
                    <p class="text-muted small mb-3">Gráficos y resumen visual de tu situación.</p>
                    <a href="Dashboard.aspx" class="btn btn-primary btn-sm mt-auto w-100">Ver</a>
                </div>
            </div>
        </div>
    </div>
</asp:Content>