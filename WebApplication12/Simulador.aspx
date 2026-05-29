<%@ Page Title="Simulador" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Simulador.aspx.cs" Inherits="CAPAS_Web.Simulador" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h4><i class="bi bi-calculator me-2 text-primary"></i>Simulador "¿Qué pasa si?"</h4>
    <p class="text-muted">Simulá cómo cambiaría tu promedio si mejoraras una nota.</p>

    <div class="row">
        <div class="col-md-5">
            <div class="card shadow-sm border-0 mb-4">
                <div class="card-header bg-primary text-white">Configurar Simulación</div>
                <div class="card-body p-4">
                    <div class="mb-3">
                        <label class="form-label">Materia</label>
                        <asp:DropDownList ID="ddlMateria" runat="server" CssClass="form-select"/>
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Nota hipotética en el Final (0-10)</label>
                        <asp:TextBox ID="txtNotaHipotetica" runat="server"
                                     CssClass="form-control" TextMode="Number"
                                     placeholder="Ej: 8"/>
                    </div>
                    <asp:Button ID="btnSimular" runat="server" Text="Simular"
                                CssClass="btn btn-primary w-100" OnClick="btnSimular_Click"/>
                </div>
            </div>
        </div>

        <div class="col-md-7">
            <asp:Panel ID="pnlResultado" runat="server" Visible="false">
                <div class="card shadow-sm border-0">
                    <div class="card-header bg-success text-white">Resultado de la Simulación</div>
                    <div class="card-body p-4">
                        <div class="row text-center g-4">
                            <div class="col-6">
                                <div class="text-muted small mb-1">Promedio Actual</div>
                                <div style="font-size:2.5rem; font-weight:bold; color:#dc3545;">
                                    <asp:Label ID="lblPromedioActual" runat="server"/>
                                </div>
                            </div>
                            <div class="col-6">
                                <div class="text-muted small mb-1">Promedio Simulado</div>
                                <div style="font-size:2.5rem; font-weight:bold; color:#198754;">
                                    <asp:Label ID="lblPromedioSimulado" runat="server"/>
                                </div>
                            </div>
                            <div class="col-12">
                                <asp:Label ID="lblMensajeSimulacion" runat="server"
                                           CssClass="alert alert-info d-block"/>
                            </div>
                        </div>
                    </div>
                </div>
            </asp:Panel>
        </div>
    </div>
</asp:Content>