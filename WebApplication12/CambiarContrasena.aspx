<%@ Page Title="Cambiar Contraseña" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="CambiarContrasena.aspx.cs"
         Inherits="CAPAS_Web.CambiarContrasena" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="row justify-content-center">
        <div class="col-md-5">
            <div class="card shadow-sm border-0">
                <div class="card-body p-4">
                    <h6 class="card-section-title"><i class="bi bi-key-fill me-2"></i>Cambiar Contraseña</h6>
                    <asp:Label ID="lblExito" runat="server"
                               CssClass="alert alert-success d-block mb-3" Visible="false"/>
                    <asp:Label ID="lblError" runat="server"
                               CssClass="alert alert-danger d-block mb-3" Visible="false"/>

                    <div class="mb-3">
                        <label class="form-label">Contraseña Actual</label>
                        <asp:TextBox ID="txtPassActual" runat="server"
                                     TextMode="Password" CssClass="form-control"/>
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Nueva Contraseña</label>
                        <asp:TextBox ID="txtNuevaPass" runat="server"
                                     TextMode="Password" CssClass="form-control"/>
                        <div class="form-text">6+ caracteres, 1 mayúscula, 1 número.</div>
                    </div>
                    <div class="mb-4">
                        <label class="form-label">Confirmar Contraseña</label>
                        <asp:TextBox ID="txtConfPass" runat="server"
                                     TextMode="Password" CssClass="form-control"/>
                    </div>
                    <div class="d-flex gap-2">
                        <asp:Button ID="btnGuardar" runat="server" Text="Guardar"
                                    CssClass="btn btn-primary" OnClick="btnGuardar_Click"/>
                        <a href="Menu.aspx" class="btn btn-secondary">Cancelar</a>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
