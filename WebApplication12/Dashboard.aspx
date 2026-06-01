<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs"
         Inherits="CAPAS_Web.Dashboard" %>

<asp:Content ContentPlaceHolderID="HeadContent" runat="server">
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h4 class="mb-4"><i class="bi bi-bar-chart-fill me-2 text-primary"></i>Dashboard Académico</h4>

    <div class="row g-4 mb-4">
        <div class="col-md-3">
            <div class="card border-0 shadow-sm stat-card stat-card--primary">
                <div class="card-body p-4 d-flex align-items-center gap-3">
                    <div class="nav-card-icon nav-card-icon--primary">
                        <i class="bi bi-book-fill"></i>
                    </div>
                    <div>
                        <div class="stat-number stat-number--primary">
                            <asp:Label ID="lblTotalMaterias" runat="server" Text="0"/>
                        </div>
                        <div class="text-muted small mt-1">Total Materias</div>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card border-0 shadow-sm stat-card stat-card--success">
                <div class="card-body p-4 d-flex align-items-center gap-3">
                    <div class="nav-card-icon nav-card-icon--success">
                        <i class="bi bi-patch-check-fill"></i>
                    </div>
                    <div>
                        <div class="stat-number stat-number--success">
                            <asp:Label ID="lblAprobadas" runat="server" Text="0"/>
                        </div>
                        <div class="text-muted small mt-1">Aprobadas</div>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card border-0 shadow-sm stat-card stat-card--warning">
                <div class="card-body p-4 d-flex align-items-center gap-3">
                    <div class="nav-card-icon nav-card-icon--warning">
                        <i class="bi bi-journal-text"></i>
                    </div>
                    <div>
                        <div class="stat-number stat-number--warning">
                            <asp:Label ID="lblCursando" runat="server" Text="0"/>
                        </div>
                        <div class="text-muted small mt-1">Cursando</div>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card border-0 shadow-sm stat-card stat-card--danger">
                <div class="card-body p-4 d-flex align-items-center gap-3">
                    <div class="nav-card-icon nav-card-icon--danger">
                        <i class="bi bi-hourglass-split"></i>
                    </div>
                    <div>
                        <div class="stat-number stat-number--danger">
                            <asp:Label ID="lblFinalPendiente" runat="server" Text="0"/>
                        </div>
                        <div class="text-muted small mt-1">Final Pendiente</div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="row g-4">
        <div class="col-md-7">
            <div class="card border-0 shadow-sm">
                <div class="card-body">
                    <h6 class="card-section-title">Notas por Materia</h6>
                    <canvas id="chartNotas" height="250"/>
                </div>
            </div>
        </div>

        <div class="col-md-5">
            <div class="card border-0 shadow-sm">
                <div class="card-body">
                    <h6 class="card-section-title">Estado de Cursadas</h6>
                    <canvas id="chartEstados" height="250"/>
                </div>
            </div>
        </div>
    </div>

    <asp:HiddenField ID="hfDatosNotas"   runat="server"/>
    <asp:HiddenField ID="hfDatosEstados" runat="server"/>
    <div class="mt-4 text-end">
        <a href="ExportarPDF.aspx" class="btn btn-danger" target="_blank">
            <i class="bi bi-file-pdf me-2"></i>Exportar PDF
        </a>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="ScriptContent" runat="server">
    <script>
        var datosNotas = JSON.parse(document.getElementById('<%= hfDatosNotas.ClientID %>').value || '{}');
        if (datosNotas.labels && datosNotas.labels.length > 0) {
            new Chart(document.getElementById('chartNotas'), {
                type: 'bar',
                data: {
                    labels: datosNotas.labels,
                    datasets: [
                        {
                            label: 'Parcial 1',
                            data: datosNotas.parcial1,
                            backgroundColor: 'rgba(13,148,136,0.8)'
                        },
                        {
                            label: 'Parcial 2',
                            data: datosNotas.parcial2,
                            backgroundColor: 'rgba(22,163,74,0.8)'
                        },
                        {
                            label: 'Final',
                            data: datosNotas.final,
                            backgroundColor: 'rgba(217,119,6,0.8)'
                        }
                    ]
                },
                options: {
                    responsive: true,
                    scales: { y: { min: 0, max: 10 } },
                    plugins: { legend: { position: 'top' } }
                }
            });
        }

        var datosEstados = JSON.parse(document.getElementById('<%= hfDatosEstados.ClientID %>').value || '{}');
        if (datosEstados.valores) {
            new Chart(document.getElementById('chartEstados'), {
                type: 'doughnut',
                data: {
                    labels: ['Aprobada', 'Cursando', 'Final Pendiente'],
                    datasets: [{
                        data: datosEstados.valores,
                        backgroundColor: ['#16a34a', '#d97706', '#dc2626']
                    }]
                },
                options: {
                    responsive: true,
                    plugins: { legend: { position: 'bottom' } }
                }
            });
        }
    </script>
</asp:Content>