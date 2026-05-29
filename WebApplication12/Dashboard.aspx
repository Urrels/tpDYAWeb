<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs"
         Inherits="CAPAS_Web.Dashboard" %>

<asp:Content ContentPlaceHolderID="HeadContent" runat="server">
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h4 class="mb-4"><i class="bi bi-bar-chart-fill me-2 text-primary"></i>Dashboard Académico</h4>

    <div class="row g-4 mb-4">
        <!-- Estadísticas rápidas -->
        <div class="col-md-3">
            <div class="card border-0 shadow-sm text-center p-3">
                <div style="font-size:2rem; font-weight:bold; color:#0d6efd;">
                    <asp:Label ID="lblTotalMaterias" runat="server" Text="0"/>
                </div>
                <div class="text-muted small">Total Materias</div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card border-0 shadow-sm text-center p-3">
                <div style="font-size:2rem; font-weight:bold; color:#198754;">
                    <asp:Label ID="lblAprobadas" runat="server" Text="0"/>
                </div>
                <div class="text-muted small">Aprobadas</div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card border-0 shadow-sm text-center p-3">
                <div style="font-size:2rem; font-weight:bold; color:#ffc107;">
                    <asp:Label ID="lblCursando" runat="server" Text="0"/>
                </div>
                <div class="text-muted small">Cursando</div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card border-0 shadow-sm text-center p-3">
                <div style="font-size:2rem; font-weight:bold; color:#dc3545;">
                    <asp:Label ID="lblFinalPendiente" runat="server" Text="0"/>
                </div>
                <div class="text-muted small">Final Pendiente</div>
            </div>
        </div>
    </div>

    <div class="row g-4">
        <!-- Gráfico de barras - notas por materia -->
        <div class="col-md-7">
            <div class="card border-0 shadow-sm">
                <div class="card-header bg-dark text-white">
                    Notas por Materia
                </div>
                <div class="card-body">
                    <canvas id="chartNotas" height="250"/>
                </div>
            </div>
        </div>

        <!-- Gráfico de torta - estados -->
        <div class="col-md-5">
            <div class="card border-0 shadow-sm">
                <div class="card-header bg-dark text-white">
                    Estado de Cursadas
                </div>
                <div class="card-body">
                    <canvas id="chartEstados" height="250"/>
                </div>
            </div>
        </div>
    </div>

    <!-- Datos para los gráficos (ocultos, generados desde el servidor) -->
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
        // Gráfico de barras - Notas
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
                            backgroundColor: 'rgba(13,110,253,0.7)'
                        },
                        {
                            label: 'Parcial 2',
                            data: datosNotas.parcial2,
                            backgroundColor: 'rgba(25,135,84,0.7)'
                        },
                        {
                            label: 'Final',
                            data: datosNotas.final,
                            backgroundColor: 'rgba(255,193,7,0.7)'
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

        // Gráfico de torta - Estados
        var datosEstados = JSON.parse(document.getElementById('<%= hfDatosEstados.ClientID %>').value || '{}');
        if (datosEstados.valores) {
            new Chart(document.getElementById('chartEstados'), {
                type: 'doughnut',
                data: {
                    labels: ['Aprobada', 'Cursando', 'Final Pendiente'],
                    datasets: [{
                        data: datosEstados.valores,
                        backgroundColor: ['#198754', '#ffc107', '#dc3545']
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