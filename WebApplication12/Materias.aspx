<%@ Page Title="Materias" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Materias.aspx.cs" Inherits="CAPAS_Web.Materias" %>
<%@ Register Src="~/Paginador.ascx" TagPrefix="uc" TagName="Paginador" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="d-flex justify-content-between align-items-center mb-3">
        <h4><i class="bi bi-book-fill me-2"></i><asp:Literal ID="litTitulo" runat="server" Text="Gestión de materias"/></h4>
        <asp:Button ID="btnNueva" runat="server" Text="+ Nueva Materia"
                    CssClass="btn btn-primary btn-sm" OnClick="btnNueva_Click"/>
    </div>

    <asp:Label ID="lblMsg" runat="server" CssClass="alert d-block mb-3" Visible="false"/>

    <div class="card shadow-sm border-0 mb-4">
        <div class="card-body p-3 d-flex flex-wrap align-items-center gap-2">
            <h6 class="card-section-title mb-0 me-3"><asp:Literal ID="litXml" runat="server" Text="Plan de estudios en XML"/></h6>
            <asp:Button ID="btnExportarXml" runat="server" Text="Exportar XML"
                        CssClass="btn btn-outline-primary btn-sm" OnClick="btnExportarXml_Click"/>
            <asp:FileUpload ID="fileXml" runat="server" CssClass="form-control form-control-sm w-auto" accept=".xml"/>
            <asp:Button ID="btnImportarXml" runat="server" Text="Importar XML"
                        CssClass="btn btn-outline-success btn-sm" OnClick="btnImportarXml_Click"
                        OnClientClick="return confirm('Se crearán o actualizarán materias según el archivo. ¿Continuar?')"/>
        </div>
    </div>

    <asp:Panel ID="pnlForm" runat="server" Visible="false" CssClass="card shadow-sm border-0 mb-4">
        <div class="card-body p-4">
            <h6 class="card-section-title"><asp:Label ID="lblTituloForm" runat="server" Text="Nueva Materia"/></h6>
            <asp:HiddenField ID="hfIdMateria" runat="server" Value="0"/>
            <div class="row g-3">
                <div class="col-md-6">
                    <label class="form-label"><asp:Literal ID="litNombre" runat="server" Text="Nombre"/></label>
                    <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control"/>
                </div>
                <div class="col-md-3">
                    <label class="form-label"><asp:Literal ID="litCodigo" runat="server" Text="Código"/></label>
                    <asp:TextBox ID="txtCodigo" runat="server" CssClass="form-control"/>
                </div>
                <div class="col-md-3">
                    <label class="form-label"><asp:Literal ID="litModalidad" runat="server" Text="Modalidad"/></label>
                    <asp:DropDownList ID="ddlModalidad" runat="server" CssClass="form-select">
                        <asp:ListItem Value="Presencial">Presencial</asp:ListItem>
                        <asp:ListItem Value="Virtual">Virtual</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="col-md-3">
                    <label class="form-label"><asp:Literal ID="litPeso" runat="server" Text="Peso (complejidad 1-5)"/></label>
                    <asp:DropDownList ID="ddlPeso" runat="server" CssClass="form-select">
                        <asp:ListItem Value="1">1 - Muy baja</asp:ListItem>
                        <asp:ListItem Value="2">2 - Baja</asp:ListItem>
                        <asp:ListItem Value="3" Selected="True">3 - Media</asp:ListItem>
                        <asp:ListItem Value="4">4 - Alta</asp:ListItem>
                        <asp:ListItem Value="5">5 - Muy alta</asp:ListItem>
                    </asp:DropDownList>
                </div>
            </div>
            <div class="mt-3 d-flex gap-2">
                <asp:Button ID="btnGuardar" runat="server" Text="Guardar"
                            CssClass="btn btn-success" OnClick="btnGuardar_Click"/>
                <asp:Button ID="btnCancelar" runat="server" Text="Cancelar"
                            CssClass="btn btn-secondary" OnClick="btnCancelar_Click"/>
            </div>
        </div>
    </asp:Panel>

    <div class="card shadow-sm border-0 mb-4">
        <div class="card-body p-3">
            <div class="d-flex align-items-center gap-2 mb-2">
                <h6 class="card-section-title mb-0 me-3"><asp:Literal ID="litWs" runat="server" Text="Plan de estudios vía Web Service"/></h6>
                <button type="button" id="btnConsultarWs" class="btn btn-outline-secondary btn-sm"><asp:Literal ID="litConsultarMateriasservice" runat="server" Text="Consultar MateriasService"/></button>
            </div>
            <div id="wsEstado" class="small text-muted"></div>
            <div id="wsResultado" class="table-responsive"></div>
        </div>
    </div>
    <script>
        document.getElementById('btnConsultarWs').addEventListener('click', function () {
            var estado = document.getElementById('wsEstado');
            var cont = document.getElementById('wsResultado');
            estado.textContent = '<%= CAPAS_Web.Textos.Js("Materias.jsConsultando", "Consultando…") %>';
            cont.textContent = '';
            fetch('MateriasService.asmx/ListarMaterias', {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'Content-Type': 'application/json; charset=utf-8' },
                body: '{}'
            }).then(function (r) {
                if (!r.ok) throw new Error('<%= CAPAS_Web.Textos.Js("Materias.jsRespondio", "El servicio respondió") %>' + ' ' + r.status + '.');
                return r.json();
            }).then(function (datos) {
                var materias = datos.d;
                var tabla = document.createElement('table');
                tabla.className = 'table table-sm';
                var cab = tabla.createTHead().insertRow();
                ['<%= CAPAS_Web.Textos.Js("Materias.jsColCodigo", "Código") %>', '<%= CAPAS_Web.Textos.Js("Materias.jsColNombre", "Nombre") %>', '<%= CAPAS_Web.Textos.Js("Materias.jsColModalidad", "Modalidad") %>', '<%= CAPAS_Web.Textos.Js("Materias.jsColPeso", "Peso") %>', '<%= CAPAS_Web.Textos.Js("Materias.jsColCorrelativas", "Correlativas") %>'].forEach(function (t) {
                    cab.insertCell().textContent = t;
                });
                var cuerpo = tabla.createTBody();
                materias.forEach(function (m) {
                    var f = cuerpo.insertRow();
                    f.insertCell().textContent = m.Codigo;
                    f.insertCell().textContent = m.Nombre;
                    f.insertCell().textContent = m.Modalidad;
                    f.insertCell().textContent = m.Peso;
                    f.insertCell().textContent = m.Correlativas.join(', ');
                });
                cont.appendChild(tabla);
                estado.textContent = materias.length + ' ' + '<%= CAPAS_Web.Textos.Js("Materias.jsRecibidas", "materias recibidas del servicio.") %>';
            }).catch(function (e) {
                estado.textContent = '<%= CAPAS_Web.Textos.Js("Materias.jsError", "No se pudo consultar el servicio:") %>' + ' ' + e.message;
            });
        });
    </script>

    <div class="card shadow-sm border-0 table-card">
        <asp:GridView ID="gvMaterias" runat="server"
                      CssClass="table table-hover"
                      AutoGenerateColumns="false"
                      HeaderStyle-CssClass=""
                      DataKeyNames="Id"
                      OnRowCommand="gvMaterias_RowCommand"
                      GridLines="None">
            <Columns>
                <asp:BoundField DataField="Codigo"    HeaderText="Código"    ItemStyle-Width="80"/>
                <asp:BoundField DataField="Nombre"    HeaderText="Nombre"/>
                <asp:TemplateField HeaderText="Modalidad" ItemStyle-Width="120"><ItemTemplate><%# CAPAS_Web.Textos.ValorH(Eval("Modalidad")) %></ItemTemplate></asp:TemplateField>
                <asp:BoundField DataField="Peso"      HeaderText="Peso"      ItemStyle-Width="60"/>
                <asp:TemplateField HeaderText="Acciones" ItemStyle-Width="200">
                    <ItemTemplate>
                        <asp:LinkButton ID="lnkEditar" CommandName="Editar" CommandArgument='<%# Eval("Id") %>'
                                        runat="server" CssClass="btn btn-warning btn-sm me-1">
                            <i class="bi bi-pencil"></i> Editar
                        </asp:LinkButton>
                        <asp:LinkButton ID="lnkBaja" CommandName="Eliminar" CommandArgument='<%# Eval("Id") %>'
                                        runat="server" CssClass="btn btn-danger btn-sm"
                                        OnClientClick="return confirm('¿Eliminar esta materia?')">
                            <i class="bi bi-trash"></i> Baja
                        </asp:LinkButton>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>

    <uc:Paginador ID="paginador" runat="server" TamanioPagina="10" OnCambioPagina="paginador_CambioPagina"/>
</asp:Content>
