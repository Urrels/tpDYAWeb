<%@ Page Title="Recomendador" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Recomendador.aspx.cs" Inherits="CAPAS_Web.Recomendador" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h4><i class="bi bi-lightbulb-fill me-2 text-warning"></i>Recomendador de Cursada</h4>
    <p class="text-muted">Materias que podés cursar el próximo cuatrimestre según tus correlativas aprobadas.</p>

    <asp:Panel ID="pnlVacio" runat="server" Visible="false"
               CssClass="alert alert-info">
        No hay materias disponibles para recomendar. Aprobá más materias para desbloquear opciones.
    </asp:Panel>

    <div class="row g-4" id="divRecomendadas" runat="server">
        <asp:Repeater ID="rptRecomendadas" runat="server">
            <ItemTemplate>
                <div class="col-md-4">
                    <div class="card h-100 shadow-sm border-0">
                        <div class="card-body">
                            <h6 class="card-title fw-bold"><%# Eval("Nombre") %></h6>
                            <p class="mb-1 small text-muted">Código: <%# Eval("Codigo") %></p>
                            <p class="mb-1 small">
                                <span class='badge <%# (string)Eval("Modalidad") == "Virtual" ? "bg-info" : "bg-primary" %>'>
                                    <%# Eval("Modalidad") %>
                                </span>
                            </p>
                            <p class="mb-0 small">
                                Complejidad:
                                <span class='badge <%# (int)Eval("Peso") >= 4 ? "bg-danger" : (int)Eval("Peso") == 3 ? "bg-warning text-dark" : "bg-success" %>'>
                                    <%# Eval("Peso") %>/5
                                </span>
                            </p>
                        </div>
                    </div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>

    <hr class="mt-4"/>
    <h5>Tus materias aprobadas</h5>
    <asp:GridView ID="gvAprobadas" runat="server"
                  CssClass="table table-sm table-bordered"
                  AutoGenerateColumns="false"
                  HeaderStyle-CssClass="table-success"
                  GridLines="None">
        <Columns>
            <asp:BoundField DataField="CodigoMateria" HeaderText="Código"/>
            <asp:BoundField DataField="NombreMateria" HeaderText="Materia"/>
            <asp:BoundField DataField="NotaFinal"     HeaderText="Nota Final"/>
        </Columns>
    </asp:GridView>
</asp:Content>
