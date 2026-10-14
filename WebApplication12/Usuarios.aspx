<%@ Page Title="Usuarios" Language="C#" MasterPageFile="~/Site.Master"
         AutoEventWireup="true" CodeBehind="Usuarios.aspx.cs" Inherits="CAPAS_Web.Usuarios" %>
<%@ Register Src="~/Paginador.ascx" TagPrefix="uc" TagName="Paginador" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h4 class="mb-4"><i class="bi bi-person-lines-fill me-2"></i>Usuarios</h4>

    <asp:Label ID="lblMsg" runat="server" CssClass="alert d-block mb-3" Visible="false"/>

    <div class="card shadow-sm border-0 table-card mb-4">
        <asp:GridView ID="gvUsuarios" runat="server" AutoGenerateColumns="false"
                      CssClass="table table-hover mb-0" GridLines="None" DataKeyNames="Id"
                      OnRowCommand="gvUsuarios_RowCommand" OnRowDataBound="gvUsuarios_RowDataBound">
            <Columns>
                <asp:BoundField DataField="Usuario" HeaderText="Usuario"/>
                <asp:BoundField DataField="PerfilesTexto" HeaderText="Perfiles"/>
                <asp:TemplateField HeaderText="Estado">
                    <ItemTemplate>
                        <asp:Label runat="server" CssClass='<%# (bool)Eval("Activo") ? "badge bg-success" : "badge bg-secondary" %>'
                                   Text='<%# (bool)Eval("Activo") ? "Activo" : "De baja" %>'/>
                        <asp:Label runat="server" CssClass="badge bg-warning text-dark" Text="Bloqueado"
                                   Visible='<%# (bool)Eval("Bloqueado") %>'/>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Acciones">
                    <ItemTemplate>
                        <asp:LinkButton ID="lnkEditar" runat="server" CommandName="Editar" CommandArgument='<%# Eval("Id") %>'
                                        CssClass="btn btn-outline-primary btn-sm">
                            <i class="bi bi-pencil"></i> Editar
                        </asp:LinkButton>
                        <asp:LinkButton ID="lnkBaja" runat="server" CommandName="Baja" CommandArgument='<%# Eval("Id") %>'
                                        CssClass="btn btn-outline-danger btn-sm"
                                        OnClientClick="return confirm('¿Dar de baja a este usuario?');">
                            <i class="bi bi-person-x"></i> Dar de baja
                        </asp:LinkButton>
                        <asp:LinkButton ID="lnkReactivar" runat="server" CommandName="Reactivar" CommandArgument='<%# Eval("Id") %>'
                                        CssClass="btn btn-outline-success btn-sm">
                            <i class="bi bi-person-check"></i> Reactivar
                        </asp:LinkButton>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>

    <uc:Paginador ID="paginador" runat="server" TamanioPagina="10" OnCambioPagina="paginador_CambioPagina"/>

    <div class="row g-3 mt-1">
        <div class="col-lg-6">
            <div class="card shadow-sm border-0">
                <div class="card-body">
                    <h6 class="mb-3">Nuevo usuario</h6>
                    <asp:TextBox ID="txtUsuario" runat="server" CssClass="form-control mb-2" MaxLength="50" placeholder="Usuario"/>
                    <asp:TextBox ID="txtPass" runat="server" CssClass="form-control mb-2" TextMode="Password" autocomplete="new-password" placeholder="Contraseña (6+, 1 mayúscula, 1 número)"/>
                    <asp:TextBox ID="txtConf" runat="server" CssClass="form-control mb-2" TextMode="Password" autocomplete="new-password" placeholder="Repetir contraseña"/>
                    <asp:TextBox ID="txtDireccion" runat="server" CssClass="form-control mb-2" MaxLength="100" placeholder="Dirección (opcional, hasta 100 caracteres)"/>
                    <label class="form-label small">Perfiles (al menos uno)</label>
                    <asp:CheckBoxList ID="cblPerfilesAlta" runat="server" CssClass="mb-2" RepeatColumns="1" CellPadding="2"/>
                    <asp:Button ID="btnCrear" runat="server" Text="Crear usuario" CssClass="btn btn-primary btn-sm" OnClick="btnCrear_Click"/>
                </div>
            </div>
        </div>

        <div class="col-lg-6">
            <asp:Panel ID="pnlEdicion" runat="server" Visible="false">
                <div class="card shadow-sm border-0">
                    <div class="card-body">
                        <h6 class="mb-3">Editar: <asp:Label ID="lblEditUsuario" runat="server" CssClass="fw-bold"/></h6>
                        <asp:TextBox ID="txtEditDireccion" runat="server" CssClass="form-control mb-2" MaxLength="100" placeholder="Dirección (vacío = sin cambios)"/>
                        <label class="form-label small">Perfiles</label>
                        <asp:CheckBoxList ID="cblPerfilesEdit" runat="server" CssClass="mb-2" RepeatColumns="1" CellPadding="2"/>
                        <asp:Button ID="btnGuardarEdicion" runat="server" Text="Guardar cambios" CssClass="btn btn-primary btn-sm" OnClick="btnGuardarEdicion_Click"/>
                        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-outline-secondary btn-sm" OnClick="btnCancelar_Click"/>

                        <asp:Panel ID="pnlReset" runat="server">
                        <hr/>
                        <h6>Restablecer contraseña</h6>
                        <asp:TextBox ID="txtResetPass" runat="server" CssClass="form-control mb-2" TextMode="Password" autocomplete="new-password" placeholder="Nueva contraseña"/>
                        <asp:TextBox ID="txtResetConf" runat="server" CssClass="form-control mb-2" TextMode="Password" autocomplete="new-password" placeholder="Repetir nueva contraseña"/>
                        <asp:Button ID="btnResetPass" runat="server" Text="Restablecer contraseña" CssClass="btn btn-outline-danger btn-sm"
                                    OnClick="btnResetPass_Click" OnClientClick="return confirm('¿Restablecer la contraseña de este usuario?');"/>
                        </asp:Panel>
                    </div>
                </div>
            </asp:Panel>
        </div>
    </div>
</asp:Content>
