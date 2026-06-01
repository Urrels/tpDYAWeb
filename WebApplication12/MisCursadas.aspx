<%@ Page Title="Mis Cursadas" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="MisCursadas.aspx.cs" Inherits="CAPAS_Web.MisCursadas" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="mb-3">
        <h4><i class="bi bi-journal-check me-2"></i>Mis Cursadas</h4>
    </div>

    <asp:Label ID="lblMsg" runat="server" CssClass="alert d-block mb-3" Visible="false"/>

    <asp:Panel ID="pnlNotas" runat="server" Visible="false" CssClass="card shadow-sm border-0 mb-4">
    <div class="card-body p-4">
        <h6 class="card-section-title card-section-title--warning">Actualizar Notas</h6>
        <asp:HiddenField ID="hfIdCursada" runat="server"/>
        <asp:HiddenField ID="hfIdMateriaNotas" runat="server"/>

        <div class="row g-3">
            <div class="col-md-3">
                <label class="form-label">Estado</label>
                <asp:DropDownList ID="ddlEstado" runat="server" CssClass="form-select"
                                  AutoPostBack="true" OnSelectedIndexChanged="ddlEstado_Changed">
                    <asp:ListItem Value="Cursando">Cursando</asp:ListItem>
                    <asp:ListItem Value="Aprobada">Aprobada</asp:ListItem>
                    <asp:ListItem Value="FinalPendiente">Final Pendiente</asp:ListItem>
                </asp:DropDownList>
            </div>

            <div class="col-md-3">
                <label class="form-label">Nota Parcial 1 <small class="text-muted">(0-10)</small></label>
                <asp:TextBox ID="txtParcial1" runat="server" CssClass="form-control"
                             TextMode="Number" placeholder="0-10"
                             AutoPostBack="true" OnTextChanged="txtParcial_Changed"/>
            </div>
            <div class="col-md-3">
                <label class="form-label">Nota Parcial 2 <small class="text-muted">(0-10)</small></label>
                <asp:TextBox ID="txtParcial2" runat="server" CssClass="form-control"
                             TextMode="Number" placeholder="0-10"
                             AutoPostBack="true" OnTextChanged="txtParcial_Changed"/>
            </div>

            <asp:Panel ID="pnlRecuperatorio" runat="server">
                <div class="col-md-3">
                    <label class="form-label text-warning fw-semibold">
                        ⚠️ Recuperatorio <small class="text-muted">(0-10)</small>
                    </label>
                    <asp:TextBox ID="txtRecuperatorio" runat="server" CssClass="form-control border-warning"
                                 TextMode="Number" placeholder="0-10"/>
                </div>
                <div class="col-md-3">
                    <label class="form-label">Fecha Recuperatorio</label>
                    <asp:TextBox ID="txtFechaRecuperatorio" runat="server"
                                 CssClass="form-control" TextMode="Date"/>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlFinal" runat="server">
                <div class="col-md-3">
                    <label class="form-label text-success fw-semibold">
                        🎓 Nota Final <small class="text-muted">(0-10)</small>
                    </label>
                    <asp:TextBox ID="txtNotaFinal" runat="server" CssClass="form-control border-success"
                                 TextMode="Number" placeholder="0-10"/>
                </div>
                <div class="col-md-3">
                    <label class="form-label">Fecha Mesa Final</label>
                    <asp:TextBox ID="txtFechaFinal" runat="server"
                                 CssClass="form-control" TextMode="Date"/>
                </div>
            </asp:Panel>
        </div>

        <div class="mt-3 d-flex gap-2">
            <asp:Button ID="btnGuardarNotas" runat="server" Text="Guardar"
                        CssClass="btn btn-success" OnClick="btnGuardarNotas_Click"/>
            <asp:Button ID="btnCancelarNotas" runat="server" Text="Cancelar"
                        CssClass="btn btn-secondary" OnClick="btnCancelarNotas_Click"/>
        </div>
    </div>
</asp:Panel>

    <div class="card shadow-sm border-0 table-card">
   <asp:GridView ID="gvCursadas" runat="server"
              CssClass="table table-hover"
              AutoGenerateColumns="false"
              HeaderStyle-CssClass=""
              DataKeyNames="Id"
              OnRowCommand="gvCursadas_RowCommand"
              GridLines="None">
    <Columns>
        <asp:BoundField DataField="CodigoMateria"     HeaderText="Código"      ItemStyle-Width="80"/>
        <asp:BoundField DataField="NombreMateria"     HeaderText="Materia"/>
        <asp:BoundField DataField="Modalidad"         HeaderText="Modalidad"   ItemStyle-Width="110"/>
        <asp:BoundField DataField="Estado"            HeaderText="Estado"      ItemStyle-Width="130"/>
        <asp:BoundField DataField="NotaParcial1"      HeaderText="Parcial 1"   ItemStyle-Width="80"/>
        <asp:BoundField DataField="NotaParcial2"      HeaderText="Parcial 2"   ItemStyle-Width="80"/>
        <asp:BoundField DataField="NotaRecuperatorio" HeaderText="Recup."      ItemStyle-Width="80"/>
        <asp:BoundField DataField="NotaFinal"         HeaderText="Final"       ItemStyle-Width="80"/>
        <asp:BoundField DataField="DVH"               HeaderText="DVH"         ItemStyle-Width="60"/>
        <asp:BoundField DataField="NivelRiesgo"   HeaderText="Riesgo"   ItemStyle-Width="80"/>
<asp:TemplateField HeaderText="Situación" ItemStyle-Width="300">
    <ItemTemplate>
        <span class='<%# ObtenerCssRiesgo(Eval("NivelRiesgo").ToString()) %>'>
            <%# Eval("MensajeRiesgo") %>
        </span>
    </ItemTemplate>
</asp:TemplateField>
        <asp:TemplateField HeaderText="Acciones"      ItemStyle-Width="100">
            <ItemTemplate>
                <asp:LinkButton CommandName="EditarNotas" CommandArgument='<%# Eval("Id") %>'
                                runat="server" CssClass="btn btn-warning btn-sm">
                    <i class="bi bi-pencil"></i>
                </asp:LinkButton>
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
</asp:GridView>
    </div>

</asp:Content>
