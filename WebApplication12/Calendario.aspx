<%@ Page Title="Calendario" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Calendario.aspx.cs" Inherits="CAPAS_Web.Calendario" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

    <div class="d-flex justify-content-between align-items-center mb-3">
        <h4><i class="bi bi-calendar3 me-2"></i><asp:Literal ID="litCalendarioAcademico" runat="server" Text="Calendario Académico"/></h4>
        <div class="d-flex gap-2 align-items-center flex-wrap">
            <asp:Button ID="btnAnterior"    runat="server" Text="◀ Anterior"
                        CssClass="btn btn-outline-primary btn-sm" OnClick="btnAnterior_Click"/>
            <asp:Label  ID="lblMesAnio"     runat="server" CssClass="fw-bold px-2 fs-5"/>
            <asp:Button ID="btnSiguiente"   runat="server" Text="Siguiente ▶"
                        CssClass="btn btn-outline-primary btn-sm" OnClick="btnSiguiente_Click"/>
            <asp:Button ID="btnHoy"         runat="server" Text="Hoy"
                        CssClass="btn btn-primary btn-sm"         OnClick="btnHoy_Click"/>
            <asp:Button ID="btnNuevoEvento" runat="server" Text="+ Nuevo Evento"
                        CssClass="btn btn-success btn-sm"         OnClick="btnNuevoEvento_Click"/>
        </div>
    </div>

    <asp:Label ID="lblMsg" runat="server" CssClass="alert d-block mb-3" Visible="false"/>

    <asp:Panel ID="pnlFormEvento" runat="server" Visible="false"
               CssClass="card shadow-sm border-0 mb-4">
        <div class="card-body p-4">
            <h6 class="card-section-title">
                <asp:Label ID="lblTituloForm" runat="server" Text="Nuevo Evento"/>
            </h6>
            <asp:HiddenField ID="hfIdEvento" runat="server" Value="0"/>
            <div class="row g-3">
                <div class="col-md-4">
                    <label class="form-label"><asp:Literal ID="litMateria" runat="server" Text="Materia"/></label>
                    <asp:DropDownList ID="ddlMateriaEvento" runat="server" CssClass="form-select"/>
                </div>
                <div class="col-md-2">
                    <label class="form-label"><asp:Literal ID="litTipo" runat="server" Text="Tipo"/></label>
                    <asp:DropDownList ID="ddlTipoEvento" runat="server" CssClass="form-select">
                        <asp:ListItem Value="Parcial">Parcial</asp:ListItem>
                        <asp:ListItem Value="Final">Final</asp:ListItem>
                        <asp:ListItem Value="TP">TP / Entrega</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="col-md-3">
                    <label class="form-label"><asp:Literal ID="litFechaYHora" runat="server" Text="Fecha y hora"/></label>
                    <asp:TextBox ID="txtFechaEvento" runat="server" CssClass="form-control"
                                 TextMode="DateTimeLocal"/>
                </div>
                <div class="col-md-1">
                    <label class="form-label"><asp:Literal ID="litPeso" runat="server" Text="Peso"/></label>
                    <asp:DropDownList ID="ddlPesoEvento" runat="server" CssClass="form-select">
                        <asp:ListItem Value="1">1</asp:ListItem>
                        <asp:ListItem Value="2">2</asp:ListItem>
                        <asp:ListItem Value="3" Selected="True">3</asp:ListItem>
                        <asp:ListItem Value="4">4</asp:ListItem>
                        <asp:ListItem Value="5">5</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="col-md-2">
                    <label class="form-label"><asp:Literal ID="litDescripcion" runat="server" Text="Descripción"/></label>
                    <asp:TextBox ID="txtDescripcionEvento" runat="server" CssClass="form-control"
                                 placeholder="Opcional"/>
                </div>
            </div>
            <div class="mt-3 d-flex gap-2">
                <asp:Button ID="btnGuardarEvento"  runat="server" Text="Guardar"
                            CssClass="btn btn-success"   OnClick="btnGuardarEvento_Click"/>
                <asp:Button ID="btnCancelarEvento" runat="server" Text="Cancelar"
                            CssClass="btn btn-secondary" OnClick="btnCancelarEvento_Click"/>
            </div>
        </div>
    </asp:Panel>

    <div class="d-flex flex-wrap gap-3 mb-3 small">
        <span><span class="badge bg-success">●</span> <asp:Literal ID="litCargaBaja5" runat="server" Text="Carga baja (≤5)"/></span>
        <span><span class="badge bg-warning text-dark">●</span> <asp:Literal ID="litCargaMedia10" runat="server" Text="Carga media (≤10)"/></span>
        <span><span class="badge bg-danger">●</span> <asp:Literal ID="litCargaAlta10" runat="server" Text="Carga alta (&gt;10)"/></span>
        <span class="cal-leyenda cal-leyenda--hoy"></span> <asp:Literal ID="litHoy" runat="server" Text="Hoy"/>
        <span class="cal-leyenda cal-leyenda--feriado"></span> <asp:Literal ID="litFeriado" runat="server" Text="Feriado"/>
        <span class="cal-leyenda cal-leyenda--no-lab"></span> <asp:Literal ID="litNoLaborable" runat="server" Text="No laborable"/>
    </div>

    <div class="table-responsive mb-4">
        <asp:Table ID="tblCalendario" runat="server" CssClass="cal-tabla"/>
    </div>

    <div class="card shadow-sm border-0 table-card">
        <asp:GridView ID="gvEventos" runat="server"
                      CssClass="table table-hover"
                      AutoGenerateColumns="false"
                      HeaderStyle-CssClass=""
                      DataKeyNames="Id"
                      OnRowCommand="gvEventos_RowCommand"
                      GridLines="None"
                      EmptyDataText="No hay eventos registrados para este mes.">
            <Columns>
                <asp:BoundField DataField="Fecha"         HeaderText="Fecha"
                                DataFormatString="{0:dd/MM/yyyy HH:mm}" ItemStyle-Width="140"/>
                <asp:BoundField DataField="NombreMateria" HeaderText="Materia"/>
                <asp:TemplateField HeaderText="Tipo" ItemStyle-Width="90"><ItemTemplate><%# CAPAS_Web.Textos.ValorH(Eval("Tipo")) %></ItemTemplate></asp:TemplateField>
                <asp:BoundField DataField="Descripcion"   HeaderText="Descripción"/>
                <asp:BoundField DataField="Peso"          HeaderText="Peso"   ItemStyle-Width="55"/>
                <asp:TemplateField HeaderText="Acciones"  ItemStyle-Width="90">
                    <ItemTemplate>
                        <asp:LinkButton ID="lnkTexto" CommandName="EditarEvento"
                                        CommandArgument='<%# Eval("Id") %>'
                                        runat="server" CssClass="btn btn-warning btn-sm me-1">
                            <i class="bi bi-pencil"></i>
                        </asp:LinkButton>
                        <asp:LinkButton ID="lnkTexto2" CommandName="EliminarEvento"
                                        CommandArgument='<%# Eval("Id") %>'
                                        runat="server" CssClass="btn btn-danger btn-sm"
                                        OnClientClick="return confirm('¿Eliminar este evento?')">
                            <i class="bi bi-trash"></i>
                        </asp:LinkButton>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>

</asp:Content>
