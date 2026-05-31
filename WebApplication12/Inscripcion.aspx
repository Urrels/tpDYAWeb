<%@ Page Title="Inscripción Cuatrimestral" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Inscripcion.aspx.cs" Inherits="CAPAS_Web.Inscripcion" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h4><i class="bi bi-pencil-square me-2"></i>Inscripción Cuatrimestral</h4>
    </div>

    <asp:Label ID="lblMsg" runat="server" CssClass="alert d-block mb-3" Visible="false"/>

    <asp:Panel ID="pnlAdminAcciones" runat="server" Visible="false" CssClass="mb-3">
        <asp:Button ID="btnMostrarCrear" runat="server" Text="+ Crear Período"
                    CssClass="btn btn-success btn-sm"
                    OnClick="btnMostrarCrear_Click"/>
    </asp:Panel>

    <asp:Panel ID="pnlListaPeriodos" runat="server" Visible="false"
               CssClass="card shadow-sm border-0 mb-4">
        <div class="card-body p-4">
            <h6 class="card-section-title">Períodos Académicos</h6>
            <asp:Repeater ID="rptPeriodos" runat="server"
                          OnItemCommand="rptPeriodos_ItemCommand">
                <ItemTemplate>
                    <div class="d-flex justify-content-between align-items-center py-2 border-bottom">
                        <div>
                            <span class="fw-semibold"><%# Eval("Etiqueta") %></span>
                            <span class="text-muted small ms-2"><%# Eval("Descripcion") %></span>
                            <span class="text-secondary small ms-2">
                                <%# ((DateTime?)Eval("FechaInicio")).HasValue ? ((DateTime)Eval("FechaInicio")).ToString("dd/MM/yyyy") : "—" %>
                                →
                                <%# ((DateTime?)Eval("FechaFin")).HasValue ? ((DateTime)Eval("FechaFin")).ToString("dd/MM/yyyy") : "—" %>
                            </span>
                        </div>
                        <asp:LinkButton CommandName="EditarPeriodo"
                                        CommandArgument='<%# Eval("Id") %>'
                                        runat="server" CssClass="btn btn-warning btn-sm">
                            <i class="bi bi-pencil"></i>
                        </asp:LinkButton>
                    </div>
                </ItemTemplate>
                <FooterTemplate>
                    <asp:Label ID="lblSinPeriodos" runat="server"
                               Text="<p class='text-muted small mt-2'>No hay períodos registrados.</p>"
                               Visible='<%# rptPeriodos.Items.Count == 0 %>'/>
                </FooterTemplate>
            </asp:Repeater>
        </div>
    </asp:Panel>

    <asp:Panel ID="pnlSinPeriodo" runat="server" Visible="false"
               CssClass="card shadow-sm border-0 mb-4">
        <div class="card-body p-4 text-center">
            <i class="bi bi-calendar-x text-warning" style="font-size:2.5rem;"></i>
            <h6 class="mt-3 fw-semibold">No hay un período académico activo</h6>
            <p class="text-muted small">No existe un cuatrimestre configurado para la fecha de hoy.
               Consultá al administrador para que habilite las inscripciones.</p>
        </div>
    </asp:Panel>

    <asp:Panel ID="pnlCrearPeriodo" runat="server" Visible="false"
               CssClass="card shadow-sm border-0 mb-4">
        <div class="card-body p-4">
            <asp:HiddenField ID="hfIdPeriodo" runat="server" Value="0"/>
            <h6 class="card-section-title">
                <asp:Label ID="lblTituloFormPeriodo" runat="server" Text="Nuevo Período Académico"/>
            </h6>
            <div class="row g-3">
                <div class="col-md-2">
                    <label class="form-label">Año</label>
                    <asp:TextBox ID="txtAnio" runat="server" CssClass="form-control"
                                 TextMode="Number" placeholder="2025"/>
                </div>
                <div class="col-md-3">
                    <label class="form-label">Cuatrimestre</label>
                    <asp:DropDownList ID="ddlCuatrimestre" runat="server" CssClass="form-select">
                        <asp:ListItem Value="1">1° Cuatrimestre</asp:ListItem>
                        <asp:ListItem Value="2">2° Cuatrimestre</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="col-md-4">
                    <label class="form-label">Descripción</label>
                    <asp:TextBox ID="txtDescripcion" runat="server" CssClass="form-control"
                                 placeholder="Ej: Cursada regular 2025"/>
                </div>
                <div class="col-md-2">
                    <label class="form-label">Fecha inicio</label>
                    <asp:TextBox ID="txtFechaInicio" runat="server" CssClass="form-control" TextMode="Date"/>
                </div>
                <div class="col-md-2">
                    <label class="form-label">Fecha fin</label>
                    <asp:TextBox ID="txtFechaFin" runat="server" CssClass="form-control" TextMode="Date"/>
                </div>
            </div>
            <div class="mt-3 d-flex gap-2">
                <asp:Button ID="btnCrearPeriodo" runat="server" Text="Guardar Período"
                            CssClass="btn btn-success" OnClick="btnCrearPeriodo_Click"/>
                <asp:Button ID="btnCancelarCrear" runat="server" Text="Cancelar"
                            CssClass="btn btn-secondary" OnClick="btnCancelarCrear_Click"/>
            </div>
        </div>
    </asp:Panel>

    <asp:Panel ID="pnlYaInscripto" runat="server" Visible="false" CssClass="mb-4">
        <asp:Label ID="lblResumen" runat="server"/>
    </asp:Panel>

    <asp:Panel ID="pnlInscribir" runat="server" Visible="false">
        <div class="d-flex justify-content-between align-items-center mb-3">
            <h6 class="card-section-title mb-0">Materias disponibles</h6>
            <asp:Label ID="lblPeriodoBadge" runat="server" CssClass="badge bg-primary fs-6"/>
        </div>
        <p class="text-muted small mb-3">
            Hacé clic en las materias que querés cursar. Solo se muestran las que ya tenés habilitadas por correlativas.
        </p>

        <div class="row g-3 mb-4">
            <asp:Repeater ID="rptMateriasDisponibles" runat="server">
                <ItemTemplate>
                    <div class="col-md-4 col-sm-6">
                        <label class="d-block h-100" style="cursor:pointer;">
                            <input type="checkbox" name="materiaId"
                                   value='<%# Eval("Id") %>'
                                   class="materia-check visually-hidden"/>
                            <div class="card h-100 border-2 materia-card-sel">
                                <div class="card-body p-3">
                                    <div class="d-flex justify-content-between align-items-start mb-2">
                                        <span class="badge bg-secondary"><%# Eval("Codigo") %></span>
                                        <span class='badge <%# (int)Eval("Peso") >= 4 ? "bg-danger" : (int)Eval("Peso") == 3 ? "bg-warning text-dark" : "bg-success" %>'>
                                            Peso <%# Eval("Peso") %>
                                        </span>
                                    </div>
                                    <h6 class="fw-semibold mb-2 lh-sm"><%# Eval("Nombre") %></h6>
                                    <span class='badge <%# (string)Eval("Modalidad") == "Virtual" ? "bg-info" : "bg-primary" %>'>
                                        <%# Eval("Modalidad") %>
                                    </span>
                                    <div class="materia-check-icon">
                                        <i class="bi bi-check-circle-fill"></i>
                                    </div>
                                </div>
                            </div>
                        </label>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </div>

        <div class="mb-4">
            <asp:Button ID="btnInscribir" runat="server" Text="Confirmar Inscripción"
                        CssClass="btn btn-primary" OnClick="btnInscribir_Click"/>
        </div>

        <asp:Panel ID="pnlNoDisponibles" runat="server" Visible="false" CssClass="mb-4">
            <h6 class="card-section-title card-section-title--warning mb-3">
                Bloqueadas (correlativas pendientes)
            </h6>
            <div class="row g-3">
                <asp:Repeater ID="rptNoDisponibles" runat="server">
                    <ItemTemplate>
                        <div class="col-md-4 col-sm-6">
                            <div class="card h-100 border-2 materia-card-bloqueada">
                                <div class="card-body p-3">
                                    <div class="d-flex justify-content-between align-items-start mb-2">
                                        <span class="badge bg-secondary"><%# Eval("Codigo") %></span>
                                        <i class="bi bi-lock-fill text-danger"></i>
                                    </div>
                                    <h6 class="fw-semibold mb-2 lh-sm text-muted"><%# Eval("Nombre") %></h6>
                                    <p class="small text-danger mb-0">
                                        Requiere: <%# ObtenerNombresCorrelativas((List<BE.MATERIA>)Eval("Correlativas")) %>
                                    </p>
                                </div>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </asp:Panel>
    </asp:Panel>

    <div class="card shadow-sm border-0">
        <div class="card-body p-4">
            <h6 class="card-section-title">Historial de inscripciones</h6>
            <asp:Repeater ID="rptHistorial" runat="server">
                <ItemTemplate>
                    <div class="mb-3 pb-3 border-bottom">
                        <div class="d-flex justify-content-between align-items-center mb-2">
                            <span class="fw-semibold">
                                <i class="bi bi-calendar-check me-1 text-primary"></i>
                                <%# Eval("EtiquetaPeriodo") %>
                            </span>
                            <span class="text-muted small">
                                Inscripto el <%# ((DateTime)Eval("FechaInscripcion")).ToString("dd/MM/yyyy") %>
                            </span>
                        </div>
                        <div class="d-flex flex-wrap gap-2">
                            <%# RenderBadgesMaterias((List<BE.MATERIA>)Eval("Materias")) %>
                        </div>
                    </div>
                </ItemTemplate>
                <FooterTemplate>
                    <asp:Label ID="lblSinHistorial" runat="server"
                               Text="<p class='text-muted small'>Todavía no tenés inscripciones registradas.</p>"
                               Visible='<%# rptHistorial.Items.Count == 0 %>'/>
                </FooterTemplate>
            </asp:Repeater>
        </div>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="ScriptContent" runat="server">
<script>
    document.querySelectorAll('.materia-check').forEach(function (cb) {
        var card = cb.closest('label').querySelector('.materia-card-sel');
        cb.addEventListener('change', function () {
            card.classList.toggle('materia-card-sel--activa', this.checked);
        });
    });
</script>
</asp:Content>
