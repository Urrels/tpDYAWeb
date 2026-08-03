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
                <h6 class="card-section-title mb-3">Alteraciones detectadas</h6>
                <asp:Repeater ID="rptResultados" runat="server">
                    <HeaderTemplate>
                        <table class="table table-hover mb-0">
                            <thead>
                                <tr>
                                    <th>Detalle</th>
                                </tr>
                            </thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr>
                            <td><i class="bi bi-exclamation-triangle me-2 text-danger"></i><%# Container.DataItem %></td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
                <asp:Label ID="lblSinAlumnos" runat="server" Visible="false"
                           Text="<p class='text-muted small mt-2'>No se detectaron alteraciones en ninguna de las 9 tablas de negocio.</p>"/>

                <asp:Panel ID="pnlAcciones" runat="server" Visible="false" CssClass="mt-3 pt-3 border-top">
                    <p class="text-muted small mb-2">
                        Elegí cómo continuar: cancelar sin aplicar ningún cambio, aceptar el estado
                        actual de los datos (recalcular dígitos verificadores) o volver al último
                        estado válido conocido (restaurar base de datos).
                    </p>
                    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CausesValidation="false"
                                CssClass="btn btn-outline-secondary btn-sm me-2" OnClick="btnCancelar_Click"/>
                    <asp:Button ID="btnRecalcular" runat="server" Text="Recalcular dígitos verificadores"
                                CssClass="btn btn-warning btn-sm me-2" OnClick="btnRecalcular_Click"/>
                    <asp:Button ID="btnBackup" runat="server" Text="Backup" CausesValidation="false"
                                CssClass="btn btn-outline-secondary btn-sm me-2" OnClick="btnBackup_Click"/>
                    <asp:Button ID="btnRestaurar" runat="server" Text="Restaurar base de datos"
                                CssClass="btn btn-danger btn-sm"
                                OnClientClick="return confirm('¿Restaurar la base de datos al último estado válido conocido? Se perderán los cambios posteriores.');"
                                OnClick="btnRestaurar_Click"/>

                    <div class="mt-3 pt-3 border-top">
                        <p class="text-muted small mb-2">
                            O restaurar usando un archivo de backup (.json) descargado previamente,
                            en vez del último estado guardado internamente:
                        </p>
                        <asp:FileUpload ID="fileBackup" runat="server"
                                         CssClass="form-control form-control-sm d-inline-block me-2" style="width:auto;"/>
                        <asp:Button ID="btnRestaurarDesdeBackup" runat="server" Text="Restaurar desde backup"
                                    CausesValidation="false" CssClass="btn btn-outline-danger btn-sm"
                                    OnClientClick="return confirm('¿Restaurar la base de datos usando el archivo de backup seleccionado? Se perderán los cambios posteriores a ese backup.');"
                                    OnClick="btnRestaurarDesdeBackup_Click"/>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </asp:Panel>
</asp:Content>
