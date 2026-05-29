<%@ Page Title="Mi Perfil" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Perfil.aspx.cs" Inherits="CAPAS_Web.Perfil" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="row justify-content-center">
        <div class="col-md-6">
            <div class="card shadow-sm border-0">
                <div class="card-header bg-dark text-white">
                    <i class="bi bi-person-fill me-2"></i>Mi Perfil
                </div>
                <div class="card-body p-4">
                    <asp:Label ID="lblMsg" runat="server"
                               CssClass="alert d-block mb-3" Visible="false"/>

                    <div class="mb-3">
                        <label class="form-label fw-semibold">Usuario</label>
                        <asp:TextBox ID="txtUsuario" runat="server"
                                     CssClass="form-control" ReadOnly="true"/>
                    </div>

                    <!-- RF11 - Dirección encriptada con AES -->
                    <div class="mb-3">
                        <label class="form-label fw-semibold">
                            Dirección / Ubicación
                            <span class="badge bg-secondary ms-1" title="Encriptada con AES-256">
                                🔒 AES-256
                            </span>
                        </label>
                        <asp:TextBox ID="txtDireccion" runat="server"
                                     CssClass="form-control"
                                     placeholder="Ej: Av. Corrientes 1234, CABA"/>
                        <div class="form-text">
                            Tu dirección se almacena encriptada con AES-256.
                            Se usa para calcular el tiempo de viaje a clases presenciales (RF08).
                        </div>
                    </div>

                    <asp:Button ID="btnGuardar" runat="server" Text="Guardar Cambios"
                                CssClass="btn btn-primary" OnClick="btnGuardar_Click"/>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
