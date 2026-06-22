<%@ Page Language="C#" AutoEventWireup="true"
         CodeBehind="Login.aspx.cs" Inherits="CAPAS_Web.Login" %>
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8"/>
    <meta name="viewport" content="width=device-width, initial-scale=1"/>
    <title>Iniciar Sesión</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet"/>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet"/>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.5/font/bootstrap-icons.css" rel="stylesheet"/>
    <link href="site.css" rel="stylesheet"/>
</head>
<body class="login-bg d-flex align-items-center justify-content-center">
    <form runat="server">
        <div class="card login-card">
            <div class="card-body p-5">
                <div class="text-center mb-4">
                    <i class="bi bi-mortarboard-fill text-primary login-icon"></i>
                    <h3 class="mt-2 fw-bold">Sistema de Gestión Académica</h3>
                    <p class="text-muted">Iniciá sesión para continuar</p>
                </div>
                <asp:Label ID="lblError" runat="server"
                           CssClass="alert alert-danger d-block mb-3" Visible="false"/>
                <div class="mb-3">
                    <label class="form-label fw-semibold">Usuario</label>
                    <div class="input-group">
                        <span class="input-group-text"><i class="bi bi-person"></i></span>
                        <asp:TextBox ID="txtUsuario" runat="server" CssClass="form-control"/>
                    </div>
                </div>
                <div class="mb-4">
                    <label class="form-label fw-semibold">Contraseña</label>
                    <div class="input-group">
                        <span class="input-group-text"><i class="bi bi-lock"></i></span>
                        <asp:TextBox ID="txtContrasena" runat="server"
                                     TextMode="Password" CssClass="form-control"/>
                    </div>
                </div>
                <asp:Button ID="btnIngresar" runat="server" Text="Ingresar"
                            CssClass="btn btn-primary w-100 py-2 fw-semibold"
                            OnClick="btnIngresar_Click"/>
            </div>
        </div>
    </form>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
</body>

</html>