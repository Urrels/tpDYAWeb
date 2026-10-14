<%@ Page Title="Perfiles" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Perfiles.aspx.cs" Inherits="CAPAS_Web.Perfiles" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h4 class="mb-4"><i class="bi bi-diagram-3-fill me-2"></i><asp:Literal ID="litPerfilesYPermisos" runat="server" Text="Perfiles y permisos"/></h4>

    <asp:Label ID="lblMsg" runat="server" CssClass="alert d-block mb-3" Visible="false"/>

    <div class="row g-3">
        <div class="col-lg-5">
            <div class="card shadow-sm border-0">
                <div class="card-body">
                    <h6 class="mb-3"><asp:Literal ID="litArbolDePerfiles" runat="server" Text="Árbol de perfiles"/></h6>
                    <p class="text-muted small">
                        <i class="bi bi-diagram-3"></i> Perfil &nbsp;·&nbsp;
                        <i class="bi bi-key"></i> <asp:Literal ID="litPermisoPropioDelPerfil" runat="server" Text="Permiso propio del perfil. Un perfil incluye también los permisos de los perfiles que contiene."/>
                    </p>
                    <asp:TreeView ID="tvPerfiles" runat="server" SkipLinkText="" ShowLines="true" ExpandDepth="FullyExpand"
                                  OnSelectedNodeChanged="tvPerfiles_SelectedNodeChanged">
                        <SelectedNodeStyle Font-Bold="true" BackColor="#e7f1ff"/>
                    </asp:TreeView>
                </div>
            </div>

            <div class="card shadow-sm border-0 mt-3">
                <div class="card-body">
                    <h6 class="mb-3"><asp:Literal ID="litNuevoPerfil" runat="server" Text="Nuevo perfil"/></h6>
                    <asp:TextBox ID="txtNuevoNombre" runat="server" CssClass="form-control mb-2"
                                 MaxLength="100" placeholder="Nombre del perfil"/>
                    <asp:Button ID="btnCrearRaiz" runat="server" Text="Crear en la raíz"
                                CssClass="btn btn-primary btn-sm" OnClick="btnCrearRaiz_Click"/>
                    <asp:Button ID="btnCrearHijo" runat="server" Text="Crear dentro del seleccionado"
                                CssClass="btn btn-outline-primary btn-sm" OnClick="btnCrearHijo_Click"/>
                </div>
            </div>
        </div>

        <div class="col-lg-7">
            <asp:Panel ID="lblSinSeleccion" runat="server">
                <p class="text-muted"><asp:Literal ID="litSeleccionaUnPerfil" runat="server" Text="Seleccioná un perfil del árbol para editarlo."/></p>
            </asp:Panel>

            <asp:Panel ID="pnlRol" runat="server" Visible="false">
                <div class="card shadow-sm border-0">
                    <div class="card-body">
                        <h5 class="mb-1"><asp:Label ID="lblRolNombre" runat="server"/></h5>
                        <asp:Label ID="lblProtegido" runat="server" Visible="false" CssClass="badge bg-secondary mb-3"
                                   Text="Perfil del sistema: no se puede renombrar, mover, eliminar ni modificar sus permisos."/>

                        <div class="row g-2 mb-3">
                            <div class="col-md-8">
                                <asp:TextBox ID="txtRenombrar" runat="server" CssClass="form-control" MaxLength="100"/>
                            </div>
                            <div class="col-md-4">
                                <asp:Button ID="btnRenombrar" runat="server" Text="Renombrar"
                                            CssClass="btn btn-outline-secondary btn-sm w-100" OnClick="btnRenombrar_Click"/>
                            </div>
                        </div>

                        <div class="row g-2 mb-3">
                            <div class="col-md-8">
                                <asp:DropDownList ID="ddlPadre" runat="server" CssClass="form-select"/>
                            </div>
                            <div class="col-md-4">
                                <asp:Button ID="btnCambiarPadre" runat="server" Text="Cambiar padre"
                                            CssClass="btn btn-outline-secondary btn-sm w-100" OnClick="btnCambiarPadre_Click"/>
                            </div>
                        </div>

                        <h6><asp:Literal ID="litPermisosPropios" runat="server" Text="Permisos propios"/></h6>
                        <asp:CheckBoxList ID="cblPermisos" runat="server" CssClass="mb-2"
                                          RepeatColumns="1" CellPadding="2"/>
                        <asp:Button ID="btnGuardarPermisos" runat="server" Text="Guardar permisos"
                                    CssClass="btn btn-primary btn-sm" OnClick="btnGuardarPermisos_Click"/>

                        <hr/>
                        <h6><asp:Literal ID="litPermisosEfectivos" runat="server" Text="Permisos efectivos"/> <span class="text-muted small"><asp:Literal ID="litPropiosHeredadosDeLos" runat="server" Text="(propios + heredados de los perfiles que contiene)"/></span></h6>
                        <asp:BulletedList ID="blEfectivos" runat="server" CssClass="small"/>

                        <hr/>
                        <asp:Button ID="btnEliminar" runat="server" Text="Eliminar perfil"
                                    CssClass="btn btn-danger btn-sm" OnClick="btnEliminar_Click"
                                    OnClientClick="return confirm('¿Eliminar este perfil?');"/>
                    </div>
                </div>
            </asp:Panel>
        </div>
    </div>
</asp:Content>
