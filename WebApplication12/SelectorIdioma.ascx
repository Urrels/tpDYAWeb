<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="SelectorIdioma.ascx.cs" Inherits="CAPAS_Web.SelectorIdioma" %>

<span class="navbar-text text-white d-inline-flex align-items-center gap-2">
    <i class="bi bi-translate"></i>
    <asp:Literal ID="litIdioma" runat="server" Text="Idioma:"/>
    <asp:DropDownList ID="ddlIdioma" runat="server" AutoPostBack="true"
                      CssClass="form-select form-select-sm w-auto"
                      OnSelectedIndexChanged="ddlIdioma_SelectedIndexChanged"/>
</span>
