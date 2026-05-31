<%@ Page Title="Integridad de Datos" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Integridad.aspx.cs" Inherits="CAPAS_Web.Integridad" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h4><i class="bi bi-shield-lock-fill me-2"></i>Integridad de Datos</h4>
        <asp:Button ID="btnVerificar" runat="server" Text="Verificar todos"
                    CssClass="btn btn-primary btn-sm" OnClick="btnVerificar_Click"/>
    </div>

    <asp:Label ID="lblMsg" runat="server" CssClass="alert d-block mb-3" Visible="false"/>

    <asp:Panel ID="pnlResultados" runat="server" Visible="false">
        <div class="card shadow-sm border-0">
            <div class="card-body p-4">
                <h6 class="card-section-title mb-3">Estado por alumno</h6>
                <asp:Repeater ID="rptResultados" runat="server">
                    <HeaderTemplate>
                        <table class="table table-hover mb-0">
                            <thead>
                                <tr>
                                    <th>Alumno</th>
                                    <th style="width:160px">Estado</th>
                                </tr>
                            </thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr>
                            <td><i class="bi bi-person-circle me-2 text-muted"></i><%# Eval("NombreUsuario") %></td>
                            <td>
                                <%# (bool)Eval("Ok")
                                    ? "<span class='badge bg-success'><i class='bi bi-check-circle me-1'></i>OK</span>"
                                    : "<span class='badge bg-danger'><i class='bi bi-exclamation-triangle me-1'></i>Alteración detectada</span>" %>
                            </td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
                <asp:Label ID="lblSinAlumnos" runat="server" Visible="false"
                           Text="<p class='text-muted small mt-2'>No hay alumnos registrados.</p>"/>
            </div>
        </div>
    </asp:Panel>
</asp:Content>
