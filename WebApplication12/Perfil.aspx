<%@ Page Title="Mi Perfil" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Perfil.aspx.cs" Inherits="CAPAS_Web.Perfil" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="row justify-content-center">
        <div class="col-md-6">
            <div class="card shadow-sm border-0">
                <div class="card-body p-4">
                    <h6 class="card-section-title"><i class="bi bi-person-fill me-2"></i><asp:Literal ID="litMiPerfil" runat="server" Text="Mi Perfil"/></h6>
                    <asp:Label ID="lblMsg" runat="server"
                               CssClass="alert d-block mb-3" Visible="false"/>

                    <div class="mb-3">
                        <label class="form-label fw-semibold"><asp:Literal ID="litUsuario" runat="server" Text="Usuario"/></label>
                        <asp:TextBox ID="txtUsuario" runat="server"
                                     CssClass="form-control" ReadOnly="true"/>
                    </div>

                    <div class="mb-3">
                        <label class="form-label fw-semibold">
                            <asp:Literal ID="litDireccionUbicacion" runat="server" Text="Dirección / Ubicación"/>
                            <span class="badge bg-secondary ms-1" title="Encriptada con AES-256">
                                <asp:Literal ID="litAes256" runat="server" Text="🔒 AES-256"/>
                            </span>
                        </label>
                        <asp:TextBox ID="txtDireccion" runat="server"
                                     CssClass="form-control"
                                     placeholder="Ej: Av. Corrientes 1234, CABA"/>
                        <div class="form-text">
                            <asp:Literal ID="litTuDireccionSeAlmacena" runat="server" Text="Tu dirección se almacena encriptada con AES-256. Se usa para calcular el tiempo de viaje a clases presenciales (RF08)."/>
                        </div>
                    </div>

                    <asp:Button ID="btnGuardar" runat="server" Text="Guardar Cambios"
                                CssClass="btn btn-primary" OnClick="btnGuardar_Click"/>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
