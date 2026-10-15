-- =============================================================================
-- Actualización de la base BDUNIVERSIDAD con todo lo agregado en la fase de multiidioma.
--
-- Qué hace (es idempotente: se puede correr más de una vez y sirve sobre una base ya en uso):
--   1. Crea IDIOMA, CONTROL_IDIOMA y TRADUCCION, agrega USUARIO.IDIOMA_ID y siembra los tres idiomas
--      (español predeterminado, inglés y portugués).
--   2. Ensancha los textos de una versión anterior a NVARCHAR(500) (migración).
--   3. Crea/actualiza los stored procedures de idiomas y traducciones.
--   4. Carga las 607 claves con sus textos en español, inglés y portugués.
-- Los demás cambios de la sesión (XML de materias, web service, páginas) no tocan la base.
--
-- Ejecutar completo, de una vez:
--   sqlcmd -S . -E -f 65001 -i DAL\Multiidioma_Actualizacion.sql
-- En SSMS: abrir con codificación UTF-8 sin firma (65001) y ejecutar con F5.
-- =============================================================================
USE BDUNIVERSIDAD
GO

-- =============================================
-- MULTIIDIOMA (patrón Observer + Facade, igual que ingenieria_software)
-- IDIOMA, CONTROL_IDIOMA (clave + texto por defecto) y TRADUCCION (texto por idioma).
-- USUARIO.IDIOMA_ID guarda la preferencia del usuario. No entra en las columnas de
-- integridad de USUARIO a propósito: cambiar de idioma no es una alteración de datos.
-- Las traducciones al inglés y al portugués se cargan con DAL/traducciones.sql.
-- =============================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='IDIOMA' AND xtype='U')
CREATE TABLE IDIOMA (
    ID             INT IDENTITY(1,1) PRIMARY KEY,
    NOMBRE         VARCHAR(50) NOT NULL,
    HABILITADO     BIT         NOT NULL DEFAULT 1,
    PREDETERMINADO BIT         NOT NULL DEFAULT 0
)
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CONTROL_IDIOMA' AND xtype='U')
CREATE TABLE CONTROL_IDIOMA (
    ID            INT IDENTITY(1,1) PRIMARY KEY,
    CLAVE         VARCHAR(100) NOT NULL UNIQUE,
    TEXTO_DEFAULT NVARCHAR(500) NOT NULL
)
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TRADUCCION' AND xtype='U')
CREATE TABLE TRADUCCION (
    IDIOMA_ID  INT          NOT NULL REFERENCES IDIOMA(ID),
    CONTROL_ID INT          NOT NULL REFERENCES CONTROL_IDIOMA(ID),
    TEXTO      NVARCHAR(500) NOT NULL,
    PRIMARY KEY (IDIOMA_ID, CONTROL_ID)
)
GO

-- Migración: una base creada con una versión anterior de este script tiene los textos más cortos
-- (200 caracteres, a veces VARCHAR) y los textos largos de traducciones.sql no entrarían.
IF EXISTS (SELECT 1 FROM sys.columns WHERE Object_ID = Object_ID('CONTROL_IDIOMA') AND Name = 'TEXTO_DEFAULT'
           AND (system_type_id <> 231 OR max_length <> 1000))
    ALTER TABLE CONTROL_IDIOMA ALTER COLUMN TEXTO_DEFAULT NVARCHAR(500) NOT NULL
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE Object_ID = Object_ID('TRADUCCION') AND Name = 'TEXTO'
           AND (system_type_id <> 231 OR max_length <> 1000))
    ALTER TABLE TRADUCCION ALTER COLUMN TEXTO NVARCHAR(500) NOT NULL
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'IDIOMA_ID' AND Object_ID = Object_ID('USUARIO'))
    ALTER TABLE USUARIO ADD IDIOMA_ID INT NULL REFERENCES IDIOMA(ID)
GO

-- Idiomas base: el español es el predeterminado y no se puede borrar ni deshabilitar.
IF NOT EXISTS (SELECT 1 FROM IDIOMA)
BEGIN
    INSERT INTO IDIOMA (NOMBRE, HABILITADO, PREDETERMINADO) VALUES ('Español',   1, 1);
    INSERT INTO IDIOMA (NOMBRE, HABILITADO, PREDETERMINADO) VALUES ('Inglés',    1, 0);
    INSERT INTO IDIOMA (NOMBRE, HABILITADO, PREDETERMINADO) VALUES ('Portugués', 1, 0);
END
GO

IF OBJECT_ID('IDIOMA_LISTAR_TODOS','P') IS NOT NULL DROP PROCEDURE IDIOMA_LISTAR_TODOS
GO
CREATE PROCEDURE IDIOMA_LISTAR_TODOS
AS
    SELECT ID, NOMBRE, HABILITADO, PREDETERMINADO FROM IDIOMA ORDER BY NOMBRE
GO

IF OBJECT_ID('IDIOMA_LISTAR_HABILITADOS','P') IS NOT NULL DROP PROCEDURE IDIOMA_LISTAR_HABILITADOS
GO
CREATE PROCEDURE IDIOMA_LISTAR_HABILITADOS
AS
    SELECT ID, NOMBRE, HABILITADO, PREDETERMINADO FROM IDIOMA WHERE HABILITADO = 1 ORDER BY NOMBRE
GO

IF OBJECT_ID('IDIOMA_OBTENER_POR_ID','P') IS NOT NULL DROP PROCEDURE IDIOMA_OBTENER_POR_ID
GO
CREATE PROCEDURE IDIOMA_OBTENER_POR_ID
    @id INT
AS
    SELECT ID, NOMBRE, HABILITADO, PREDETERMINADO FROM IDIOMA WHERE ID = @id
GO

IF OBJECT_ID('IDIOMA_OBTENER_PREDETERMINADO','P') IS NOT NULL DROP PROCEDURE IDIOMA_OBTENER_PREDETERMINADO
GO
CREATE PROCEDURE IDIOMA_OBTENER_PREDETERMINADO
AS
    SELECT TOP 1 ID, NOMBRE, HABILITADO, PREDETERMINADO FROM IDIOMA
    WHERE PREDETERMINADO = 1 AND HABILITADO = 1 ORDER BY ID
GO

IF OBJECT_ID('IDIOMA_ESTA_EN_USO','P') IS NOT NULL DROP PROCEDURE IDIOMA_ESTA_EN_USO
GO
CREATE PROCEDURE IDIOMA_ESTA_EN_USO
    @id INT
AS
    SELECT COUNT(*) AS TOTAL FROM USUARIO WHERE IDIOMA_ID = @id
GO

IF OBJECT_ID('IDIOMA_INSERTAR','P') IS NOT NULL DROP PROCEDURE IDIOMA_INSERTAR
GO
CREATE PROCEDURE IDIOMA_INSERTAR
    @nombre     VARCHAR(50),
    @habilitado BIT
AS
BEGIN
    INSERT INTO IDIOMA (NOMBRE, HABILITADO) VALUES (@nombre, @habilitado)
    SELECT SCOPE_IDENTITY() AS ID
END
GO

IF OBJECT_ID('IDIOMA_RENOMBRAR','P') IS NOT NULL DROP PROCEDURE IDIOMA_RENOMBRAR
GO
CREATE PROCEDURE IDIOMA_RENOMBRAR
    @id     INT,
    @nombre NVARCHAR(100)
AS
    UPDATE IDIOMA SET NOMBRE = @nombre WHERE ID = @id
GO

IF OBJECT_ID('IDIOMA_ACTUALIZAR_ESTADO','P') IS NOT NULL DROP PROCEDURE IDIOMA_ACTUALIZAR_ESTADO
GO
CREATE PROCEDURE IDIOMA_ACTUALIZAR_ESTADO
    @id         INT,
    @habilitado BIT
AS
    UPDATE IDIOMA SET HABILITADO = @habilitado WHERE ID = @id
GO

IF OBJECT_ID('IDIOMA_ELIMINAR','P') IS NOT NULL DROP PROCEDURE IDIOMA_ELIMINAR
GO
CREATE PROCEDURE IDIOMA_ELIMINAR
    @id INT
AS
BEGIN
    SET XACT_ABORT ON;

    IF EXISTS (SELECT 1 FROM IDIOMA WHERE ID = @id AND PREDETERMINADO = 1)
        THROW 50001, 'No se puede eliminar el idioma predeterminado del sistema.', 1;

    IF EXISTS (SELECT 1 FROM USUARIO WHERE IDIOMA_ID = @id)
        THROW 50002, 'No se puede eliminar un idioma que está en uso por algún usuario.', 1;

    BEGIN TRANSACTION;
        DELETE FROM TRADUCCION WHERE IDIOMA_ID = @id;
        DELETE FROM IDIOMA     WHERE ID        = @id;
    COMMIT TRANSACTION;
END
GO

IF OBJECT_ID('CONTROL_REGISTRAR','P') IS NOT NULL DROP PROCEDURE CONTROL_REGISTRAR
GO
CREATE PROCEDURE CONTROL_REGISTRAR
    @clave         VARCHAR(100),
    @texto_default NVARCHAR(500)
AS
    IF NOT EXISTS (SELECT 1 FROM CONTROL_IDIOMA WHERE CLAVE = @clave)
        INSERT INTO CONTROL_IDIOMA (CLAVE, TEXTO_DEFAULT) VALUES (@clave, @texto_default)
GO

IF OBJECT_ID('TRADUCCION_GUARDAR','P') IS NOT NULL DROP PROCEDURE TRADUCCION_GUARDAR
GO
CREATE PROCEDURE TRADUCCION_GUARDAR
    @idioma_id  INT,
    @control_id INT,
    @texto      NVARCHAR(500)
AS
BEGIN
    IF @idioma_id IS NULL OR @control_id IS NULL
    BEGIN
        RAISERROR('idioma_id o control_id es NULL — verificar que existan en sus tablas', 16, 1)
        RETURN
    END
    IF EXISTS (SELECT 1 FROM TRADUCCION WHERE IDIOMA_ID = @idioma_id AND CONTROL_ID = @control_id)
        UPDATE TRADUCCION SET TEXTO = @texto
        WHERE IDIOMA_ID = @idioma_id AND CONTROL_ID = @control_id
    ELSE
        INSERT INTO TRADUCCION (IDIOMA_ID, CONTROL_ID, TEXTO) VALUES (@idioma_id, @control_id, @texto)
END
GO

IF OBJECT_ID('TRADUCCION_LISTAR_CONTROLES','P') IS NOT NULL DROP PROCEDURE TRADUCCION_LISTAR_CONTROLES
GO
CREATE PROCEDURE TRADUCCION_LISTAR_CONTROLES
    @idioma_id INT
AS
    SELECT ci.ID, ci.CLAVE, ci.TEXTO_DEFAULT,
           ISNULL(t.TEXTO, '') AS TEXTO_TRADUCCION
    FROM CONTROL_IDIOMA ci
    LEFT JOIN TRADUCCION t ON t.CONTROL_ID = ci.ID AND t.IDIOMA_ID = @idioma_id
    ORDER BY ci.CLAVE
GO

IF OBJECT_ID('USUARIO_ACTUALIZAR_IDIOMA','P') IS NOT NULL DROP PROCEDURE USUARIO_ACTUALIZAR_IDIOMA
GO
CREATE PROCEDURE USUARIO_ACTUALIZAR_IDIOMA
    @id        INT,
    @idioma_id INT
AS
    UPDATE USUARIO SET IDIOMA_ID = @idioma_id WHERE ID = @id
GO

IF OBJECT_ID('USUARIO_OBTENER_IDIOMA','P') IS NOT NULL DROP PROCEDURE USUARIO_OBTENER_IDIOMA
GO
CREATE PROCEDURE USUARIO_OBTENER_IDIOMA
    @id INT
AS
    SELECT IDIOMA_ID FROM USUARIO WHERE ID = @id
GO

PRINT '✔ Multiidioma instalado correctamente.';
GO

-- =============================================
-- TRADUCCIONES (es / en / pt)
-- =============================================
SET NOCOUNT ON;

DECLARE @textos TABLE (CLAVE VARCHAR(100) PRIMARY KEY, ES NVARCHAR(500), EN NVARCHAR(500), PT NVARCHAR(500));
INSERT INTO @textos (CLAVE, ES, EN, PT) VALUES
    (N'AsignarPerfiles', N'Asignar perfiles', N'Assign profiles', N'Atribuir perfis'),
    (N'AsignarPerfiles.btnGuardar', N'Guardar asignación', N'Save assignment', N'Salvar atribuição'),
    (N'AsignarPerfiles.litAsignarPerfilesAUsuarios', N'Asignar perfiles a usuarios', N'Assign profiles to users', N'Atribuir perfis a usuários'),
    (N'AsignarPerfiles.litPerfilesAsignados', N'Perfiles asignados', N'Assigned profiles', N'Perfis atribuídos'),
    (N'AsignarPerfiles.litPermisosEfectivosDelUsua', N'Permisos efectivos del usuario', N'User''s effective permissions', N'Permissões efetivas do usuário'),
    (N'AsignarPerfiles.litSegunLoGuardado', N'(según lo guardado)', N'(as saved)', N'(conforme salvo)'),
    (N'AsignarPerfiles.litUsuario', N'Usuario', N'User', N'Usuário'),
    (N'AsignarPerfiles.msgGuardada', N'Asignación guardada.', N'Assignment saved.', N'Atribuição salva.'),
    (N'AsignarPerfiles.msgInesperado', N'Ocurrió un error inesperado al guardar. Revisá el estado y volvé a intentar.', N'An unexpected error occurred while saving. Check the status and try again.', N'Ocorreu um erro inesperado ao salvar. Verifique o estado e tente novamente.'),
    (N'Bitacora', N'Bitácora', N'Audit log', N'Log de auditoria'),
    (N'Bitacora.btnFiltrar', N'Filtrar', N'Filter', N'Filtrar'),
    (N'Bitacora.btnLimpiar', N'Limpiar filtros', N'Clear filters', N'Limpar filtros'),
    (N'Bitacora.ddlCriticidad.item.Alta', N'Alta', N'High', N'Alta'),
    (N'Bitacora.ddlCriticidad.item.Baja', N'Baja', N'Low', N'Baixa'),
    (N'Bitacora.ddlCriticidad.item.Media', N'Media', N'Medium', N'Média'),
    (N'Bitacora.ddlCriticidad.item.vacio', N'Todas', N'All', N'Todas'),
    (N'Bitacora.gvBitacora.enc.Accion', N'Acción', N'Action', N'Ação'),
    (N'Bitacora.gvBitacora.enc.Criticidad', N'Criticidad', N'Severity', N'Criticidade'),
    (N'Bitacora.gvBitacora.enc.FechayHora', N'Fecha y Hora', N'Date and time', N'Data e hora'),
    (N'Bitacora.gvBitacora.enc.ID', N'ID', N'ID', N'ID'),
    (N'Bitacora.gvBitacora.enc.Usuario', N'Usuario', N'User', N'Usuário'),
    (N'Bitacora.litBitacoraDelSistema', N'Bitácora del Sistema', N'System audit log', N'Log de auditoria do sistema'),
    (N'Bitacora.litCriticidad', N'Criticidad', N'Severity', N'Criticidade'),
    (N'Bitacora.litFechaDesde', N'Fecha desde', N'Date from', N'Data de'),
    (N'Bitacora.litFechaHasta', N'Fecha hasta', N'Date to', N'Data até'),
    (N'Bitacora.litFiltros', N'Filtros', N'Filters', N'Filtros'),
    (N'Bitacora.litUsuario', N'Usuario', N'User', N'Usuário'),
    (N'Bitacora.todos', N'Todos', N'All', N'Todos'),
    (N'Calendario', N'Calendario', N'Calendar', N'Calendário'),
    (N'Calendario.btnAnterior', N'◀ Anterior', N'◀ Previous', N'◀ Anterior'),
    (N'Calendario.btnCancelarEvento', N'Cancelar', N'Cancel', N'Cancelar'),
    (N'Calendario.btnGuardarEvento', N'Guardar', N'Save', N'Salvar'),
    (N'Calendario.btnHoy', N'Hoy', N'Today', N'Hoje'),
    (N'Calendario.btnNuevoEvento', N'+ Nuevo Evento', N'+ New event', N'+ Novo evento'),
    (N'Calendario.btnSiguiente', N'Siguiente ▶', N'Next ▶', N'Próximo ▶'),
    (N'Calendario.ddlTipoEvento.item.Final', N'Final', N'Final', N'Final'),
    (N'Calendario.ddlTipoEvento.item.Parcial', N'Parcial', N'Midterm', N'Prova parcial'),
    (N'Calendario.ddlTipoEvento.item.TP', N'TP / Entrega', N'Assignment / Deliverable', N'Trabalho / Entrega'),
    (N'Calendario.gvEventos.enc.Acciones', N'Acciones', N'Actions', N'Ações'),
    (N'Calendario.gvEventos.enc.Descripcion', N'Descripción', N'Description', N'Descrição'),
    (N'Calendario.gvEventos.enc.Fecha', N'Fecha', N'Date', N'Data'),
    (N'Calendario.gvEventos.enc.Materia', N'Materia', N'Subject', N'Matéria'),
    (N'Calendario.gvEventos.enc.Peso', N'Peso', N'Weight', N'Peso'),
    (N'Calendario.gvEventos.enc.Tipo', N'Tipo', N'Type', N'Tipo'),
    (N'Calendario.gvEventos.vacio', N'No hay eventos registrados para este mes.', N'There are no events registered for this month.', N'Não há eventos registrados para este mês.'),
    (N'Calendario.litCalendarioAcademico', N'Calendario Académico', N'Academic calendar', N'Calendário acadêmico'),
    (N'Calendario.litCargaAlta10', N'Carga alta (>10)', N'High load (>10)', N'Carga alta (>10)'),
    (N'Calendario.litCargaBaja5', N'Carga baja (≤5)', N'Low load (≤5)', N'Carga baixa (≤5)'),
    (N'Calendario.litCargaMedia10', N'Carga media (≤10)', N'Medium load (≤10)', N'Carga média (≤10)'),
    (N'Calendario.litDescripcion', N'Descripción', N'Description', N'Descrição'),
    (N'Calendario.litFechaYHora', N'Fecha y hora', N'Date and time', N'Data e hora'),
    (N'Calendario.litFeriado', N'Feriado', N'Holiday', N'Feriado'),
    (N'Calendario.litHoy', N'Hoy', N'Today', N'Hoje'),
    (N'Calendario.litMateria', N'Materia', N'Subject', N'Matéria'),
    (N'Calendario.litNoLaborable', N'No laborable', N'Non-working day', N'Não útil'),
    (N'Calendario.litPeso', N'Peso', N'Weight', N'Peso'),
    (N'Calendario.litTipo', N'Tipo', N'Type', N'Tipo'),
    (N'Calendario.lnkTexto2.confirmar', N'¿Eliminar este evento?', N'Delete this event?', N'Excluir este evento?'),
    (N'Calendario.msgActualizado', N'✔ Evento actualizado.', N'✔ Event updated.', N'✔ Evento atualizado.'),
    (N'Calendario.msgCreado', N'✔ Evento creado.', N'✔ Event created.', N'✔ Evento criado.'),
    (N'Calendario.msgEliminado', N'✔ Evento eliminado.', N'✔ Event deleted.', N'✔ Evento excluído.'),
    (N'Calendario.msgErrorEliminar', N'✖ Error al eliminar.', N'✖ Error while deleting.', N'✖ Erro ao excluir.'),
    (N'Calendario.msgErrorGuardar', N'✖ Error al guardar el evento.', N'✖ Error while saving the event.', N'✖ Erro ao salvar o evento.'),
    (N'Calendario.tituloEditar', N'Editar Evento', N'Edit event', N'Editar evento'),
    (N'Calendario.tituloNuevo', N'Nuevo Evento', N'New event', N'Novo evento'),
    (N'Calendario.txtDescripcionEvento.placeholder', N'Opcional', N'Optional', N'Opcional'),
    (N'CambiarContrasena', N'Cambiar Contraseña', N'Change password', N'Alterar senha'),
    (N'CambiarContrasena.btnGuardar', N'Guardar', N'Save', N'Salvar'),
    (N'CambiarContrasena.lit6Caracteres1Mayuscula', N'6+ caracteres, 1 mayúscula, 1 número.', N'6+ characters, 1 uppercase letter, 1 number.', N'6+ caracteres, 1 maiúscula, 1 número.'),
    (N'CambiarContrasena.litCambiarContrasena', N'Cambiar Contraseña', N'Change password', N'Alterar senha'),
    (N'CambiarContrasena.litCancelar', N'Cancelar', N'Cancel', N'Cancelar'),
    (N'CambiarContrasena.litConfirmarContrasena', N'Confirmar Contraseña', N'Confirm password', N'Confirmar senha'),
    (N'CambiarContrasena.litContrasenaActual', N'Contraseña Actual', N'Current password', N'Senha atual'),
    (N'CambiarContrasena.litNuevaContrasena', N'Nueva Contraseña', N'New password', N'Nova senha'),
    (N'CambiarContrasena.msgActualIncorrecta', N'La contraseña actual es incorrecta.', N'The current password is incorrect.', N'A senha atual está incorreta.'),
    (N'CambiarContrasena.msgCambiada', N'✔ Contraseña cambiada.', N'✔ Password changed.', N'✔ Senha alterada.'),
    (N'CambiarContrasena.msgCompletar', N'Completá todos los campos.', N'Fill in all the fields.', N'Preencha todos os campos.'),
    (N'CambiarContrasena.msgError', N'✖ Error al cambiar.', N'✖ Error while changing.', N'✖ Erro ao alterar.'),
    (N'CambiarContrasena.msgNoCoinciden', N'Las contraseñas no coinciden.', N'The passwords do not match.', N'As senhas não coincidem.'),
    (N'CambiarContrasena.msgPolitica', N'Debe tener 6+ caracteres, 1 mayúscula y 1 número.', N'It must have 6+ characters, 1 uppercase letter and 1 number.', N'Deve ter 6+ caracteres, 1 maiúscula e 1 número.'),
    (N'Dashboard', N'Dashboard', N'Dashboard', N'Painel'),
    (N'Dashboard.jsAprobadas', N'Aprobadas:', N'Passed:', N'Aprovadas:'),
    (N'Dashboard.jsConsultando', N'Consultando…', N'Querying…', N'Consultando…'),
    (N'Dashboard.jsCursando', N'Cursando:', N'In progress:', N'Cursando:'),
    (N'Dashboard.jsError', N'No se pudo consultar el servicio:', N'The service could not be queried:', N'Não foi possível consultar o serviço:'),
    (N'Dashboard.jsFinal', N'Final', N'Final', N'Final'),
    (N'Dashboard.jsFinalPendiente', N'Final pendiente:', N'Final exam pending:', N'Final pendente:'),
    (N'Dashboard.jsParcial1', N'Parcial 1', N'Midterm 1', N'Parcial 1'),
    (N'Dashboard.jsParcial2', N'Parcial 2', N'Midterm 2', N'Parcial 2'),
    (N'Dashboard.jsPromedio', N'Promedio ponderado:', N'Weighted average:', N'Média ponderada:'),
    (N'Dashboard.jsRespondio', N'El servicio respondió', N'The service responded', N'O serviço respondeu'),
    (N'Dashboard.jsSinDatos', N'sin datos', N'no data', N'sem dados'),
    (N'Dashboard.jsTotal', N'Total:', N'Total:', N'Total:'),
    (N'Dashboard.litAprobadas', N'Aprobadas', N'Passed', N'Aprovadas'),
    (N'Dashboard.litConsultarMateriasservice', N'Consultar MateriasService', N'Query MateriasService', N'Consultar MateriasService'),
    (N'Dashboard.litCursando', N'Cursando', N'In progress', N'Cursando'),
    (N'Dashboard.litEstados', N'Estado de cursadas', N'Course status', N'Situação das cursadas'),
    (N'Dashboard.litFinalPendiente', N'Final pendiente', N'Final exam pending', N'Final pendente'),
    (N'Dashboard.litNotas', N'Notas por materia', N'Grades by subject', N'Notas por matéria'),
    (N'Dashboard.litPdf', N'Exportar PDF', N'Export PDF', N'Exportar PDF'),
    (N'Dashboard.litTitulo', N'Dashboard académico', N'Academic dashboard', N'Painel acadêmico'),
    (N'Dashboard.litTotal', N'Total de materias', N'Total subjects', N'Total de matérias'),
    (N'Dashboard.litWs', N'Resumen vía Web Service', N'Summary via Web Service', N'Resumo via Web Service'),
    (N'Idiomas', N'Idiomas', N'Languages', N'Idiomas'),
    (N'Idiomas.btnAgregar', N'Agregar idioma', N'Add language', N'Adicionar idioma'),
    (N'Idiomas.btnAlternar', N'Habilitar/Deshabilitar', N'Enable/Disable', N'Ativar/Desativar'),
    (N'Idiomas.btnEliminar', N'Eliminar', N'delete', N'excluir'),
    (N'Idiomas.btnFiltrar', N'Filtrar por clave', N'Filter by key', N'Filtrar por chave'),
    (N'Idiomas.btnGuardar', N'Guardar traducciones', N'Save translations', N'Salvar traduções'),
    (N'Idiomas.btnRenombrar', N'Renombrar el seleccionado', N'Rename the selected one', N'Renomear o selecionado'),
    (N'Idiomas.btnTraducciones', N'Ver traducciones', N'View translations', N'Ver traduções'),
    (N'Idiomas.confirmarEliminar', N'¿Eliminar el idioma? Se borrarán todas sus traducciones.', N'Delete the language? All its translations will be erased.', N'Excluir o idioma? Todas as suas traduções serão apagadas.'),
    (N'Idiomas.gvIdiomas.enc.Acciones', N'Acciones', N'Actions', N'Ações'),
    (N'Idiomas.gvIdiomas.enc.Habilitado', N'Habilitado', N'Enabled', N'Habilitado'),
    (N'Idiomas.gvIdiomas.enc.Idioma', N'Idioma', N'Language', N'Idioma'),
    (N'Idiomas.gvIdiomas.enc.Predeterminado', N'Predeterminado', N'Default', N'Padrão'),
    (N'Idiomas.gvTraducciones.enc.Clave', N'Clave', N'Key', N'Chave'),
    (N'Idiomas.gvTraducciones.enc.Textopordefecto', N'Texto por defecto', N'Default text', N'Texto padrão'),
    (N'Idiomas.gvTraducciones.enc.Traduccion', N'Traducción', N'Translation', N'Tradução'),
    (N'Idiomas.litIdiomas', N'Idiomas', N'Languages', N'Idiomas'),
    (N'Idiomas.litTitulo', N'Gestión de idiomas', N'Language management', N'Gestão de idiomas'),
    (N'Idiomas.litTraducciones', N'Traducciones del idioma seleccionado', N'Translations of the selected language', N'Traduções do idioma selecionado'),
    (N'Idiomas.msgCreado', N'Idioma agregado. Está deshabilitado hasta que lo habilites.', N'Language added. It stays disabled until you enable it.', N'Idioma adicionado. Fica desativado até você ativá-lo.'),
    (N'Idiomas.msgEliminado', N'Idioma eliminado.', N'Language deleted.', N'Idioma excluído.'),
    (N'Idiomas.msgEstado', N'Estado actualizado.', N'Status updated.', N'Estado atualizado.'),
    (N'Idiomas.msgGuardadas', N'Traducciones guardadas: {0}.', N'Translations saved: {0}.', N'Traduções salvas: {0}.'),
    (N'Idiomas.msgRenombrado', N'Idioma renombrado.', N'Language renamed.', N'Idioma renomeado.'),
    (N'Idiomas.msgSeleccionar', N'Seleccioná un idioma.', N'Select a language.', N'Selecione um idioma.'),
    (N'Idiomas.no', N'No', N'No', N'Não'),
    (N'Idiomas.si', N'Sí', N'Yes', N'Sim'),
    (N'Inscripcion', N'Inscripción Cuatrimestral', N'Term enrollment', N'Matrícula semestral'),
    (N'Inscripcion.btnCancelarCrear', N'Cancelar', N'Cancel', N'Cancelar'),
    (N'Inscripcion.btnGuardarCambios', N'Guardar Cambios', N'Save changes', N'Salvar alterações'),
    (N'Inscripcion.btnGuardarPeriodo', N'Guardar Período', N'Save period', N'Salvar período'),
    (N'Inscripcion.btnInscribir', N'Confirmar Inscripción', N'Confirm enrollment', N'Confirmar matrícula'),
    (N'Inscripcion.btnMostrarCrear', N'+ Crear Período', N'+ Create period', N'+ Criar período'),
    (N'Inscripcion.ddlCuatrimestre.item.1', N'1° Cuatrimestre', N'1st Term', N'1º Semestre'),
    (N'Inscripcion.ddlCuatrimestre.item.2', N'2° Cuatrimestre', N'2nd Term', N'2º Semestre'),
    (N'Inscripcion.fechaNoDefinida', N'fecha no definida', N'date not set', N'data não definida'),
    (N'Inscripcion.litAno', N'Año', N'Year', N'Ano'),
    (N'Inscripcion.litBloqueadasCorrelativasPe', N'Bloqueadas (correlativas pendientes)', N'Locked (pending prerequisites)', N'Bloqueadas (correlativas pendentes)'),
    (N'Inscripcion.litCuatrimestre', N'Cuatrimestre', N'Term', N'Semestre'),
    (N'Inscripcion.litDescripcion', N'Descripción', N'Description', N'Descrição'),
    (N'Inscripcion.litFechaFin', N'Fecha fin', N'End date', N'Data final'),
    (N'Inscripcion.litFechaInicio', N'Fecha inicio', N'Start date', N'Data inicial'),
    (N'Inscripcion.litHaceClicEnLas', N'Hacé clic en las materias que querés cursar. Solo se muestran las que ya tenés habilitadas por correlativas.', N'Click the subjects you want to take. Only those you are already allowed to take by prerequisites are shown.', N'Clique nas matérias que você quer cursar. Só aparecem as que você já tem liberadas pelas correlativas.'),
    (N'Inscripcion.litHistorialDeInscripciones', N'Historial de inscripciones', N'Enrollment history', N'Histórico de matrículas'),
    (N'Inscripcion.litInscripcionCuatrimestral', N'Inscripción Cuatrimestral', N'Term enrollment', N'Matrícula semestral'),
    (N'Inscripcion.litInscriptoEl', N'Inscripto el', N'Enrolled on', N'Matriculado em'),
    (N'Inscripcion.litMateriasDisponibles', N'Materias disponibles', N'Available subjects', N'Matérias disponíveis'),
    (N'Inscripcion.litNoExisteUnCuatrimestre', N'No existe un cuatrimestre configurado para la fecha de hoy. Consultá al administrador para que habilite las inscripciones.', N'There is no term configured for today''s date. Ask the administrator to enable enrollments.', N'Não existe um semestre configurado para a data de hoje. Consulte o administrador para habilitar as matrículas.'),
    (N'Inscripcion.litNoHayPeriodos', N'No hay períodos registrados.', N'There are no registered periods.', N'Não há períodos registrados.'),
    (N'Inscripcion.litNoHayUnPeriodo', N'No hay un período académico activo', N'There is no active academic period', N'Não há um período acadêmico ativo'),
    (N'Inscripcion.litPeriodosAcademicos', N'Períodos Académicos', N'Academic periods', N'Períodos acadêmicos'),
    (N'Inscripcion.litPeso', N'Peso', N'Weight', N'Peso'),
    (N'Inscripcion.litRequiere', N'Requiere:', N'Requires:', N'Requer:'),
    (N'Inscripcion.msgErrorActualizar', N'Error al actualizar el período.', N'Error while updating the period.', N'Erro ao atualizar o período.'),
    (N'Inscripcion.msgErrorCrear', N'Error al crear el período.', N'Error while creating the period.', N'Erro ao criar o período.'),
    (N'Inscripcion.msgPeriodoActivo', N'Período activo: {0}. Los alumnos pueden inscribirse hasta {1}.', N'Active period: {0}. Students can enroll until {1}.', N'Período ativo: {0}. Os alunos podem se matricular até {1}.'),
    (N'Inscripcion.msgPeriodoActualizado', N'✔ Período {0} actualizado correctamente.', N'✔ Period {0} updated successfully.', N'✔ Período {0} atualizado com sucesso.'),
    (N'Inscripcion.msgPeriodoCreado', N'✔ Período {0} creado correctamente.', N'✔ Period {0} created successfully.', N'✔ Período {0} criado com sucesso.'),
    (N'Inscripcion.msgSinMaterias', N'No hay materias disponibles para inscribirte. Revisá si ya estás cursando todo o si te faltan correlativas.', N'There are no subjects available for you to enroll in. Check whether you are already taking everything or are missing prerequisites.', N'Não há matérias disponíveis para você se matricular. Verifique se você já está cursando tudo ou se faltam correlativas.'),
    (N'Inscripcion.registradoEl', N'Registrado el {0}.', N'Registered on {0}.', N'Registrado em {0}.'),
    (N'Inscripcion.sinMaterias', N'Sin materias registradas', N'No subjects registered', N'Nenhuma matéria registrada'),
    (N'Inscripcion.tituloEditar', N'Editar Período Académico', N'Edit academic period', N'Editar período acadêmico'),
    (N'Inscripcion.tituloNuevo', N'Nuevo Período Académico', N'New academic period', N'Novo período acadêmico'),
    (N'Inscripcion.txtAnio.placeholder', N'2025', N'2025', N'2025'),
    (N'Inscripcion.txtDescripcion.placeholder', N'Ej: Cursada regular 2025', N'E.g. Regular term 2025', N'Ex.: Cursada regular 2025'),
    (N'Inscripcion.yaInscripto', N'Ya estás inscripto en {0}.', N'You are already enrolled in {0}.', N'Você já está matriculado em {0}.'),
    (N'Integridad', N'Integridad de Datos', N'Data integrity', N'Integridade de dados'),
    (N'Integridad.alteraciones', N'{0} alteración(es)', N'{0} alteration(s)', N'{0} alteração(ões)'),
    (N'Integridad.btnBackup', N'Backup', N'Backup', N'Backup'),
    (N'Integridad.btnCancelar', N'Cancelar', N'Cancel', N'Cancelar'),
    (N'Integridad.btnRecalcular', N'Recalcular dígitos verificadores', N'Recalculate check digits', N'Recalcular dígitos verificadores'),
    (N'Integridad.btnRestaurar', N'Restaurar base de datos', N'Restore database', N'Restaurar banco de dados'),
    (N'Integridad.btnRestaurar.confirmar', N'¿Restaurar la base de datos al último estado válido conocido? Se perderán los cambios posteriores.', N'Restore the database to the last known valid state? Later changes will be lost.', N'Restaurar o banco de dados ao último estado válido conhecido? As alterações posteriores serão perdidas.'),
    (N'Integridad.btnRestaurarDesdeBackup', N'Restaurar desde backup', N'Restore from backup', N'Restaurar a partir do backup'),
    (N'Integridad.btnRestaurarDesdeBackup.confirmar', N'¿Restaurar la base de datos usando el archivo de backup seleccionado? Se perderán los cambios posteriores a ese backup.', N'Restore the database using the selected backup file? Changes made after that backup will be lost.', N'Restaurar o banco de dados usando o arquivo de backup selecionado? As alterações posteriores a esse backup serão perdidas.'),
    (N'Integridad.litAlteracionesDetectadas', N'Alteraciones detectadas', N'Alterations detected', N'Alterações detectadas'),
    (N'Integridad.litDetalle', N'Detalle', N'Detail', N'Detalhe'),
    (N'Integridad.litElegiComoContinuarCancel', N'Elegí cómo continuar: cancelar sin aplicar ningún cambio, aceptar el estado actual de los datos (recalcular dígitos verificadores) o volver al último estado válido conocido (restaurar base de datos).', N'Choose how to continue: cancel without applying any change, accept the current state of the data (recalculate check digits) or go back to the last known valid state (restore the database).', N'Escolha como continuar: cancelar sem aplicar nenhuma alteração, aceitar o estado atual dos dados (recalcular dígitos verificadores) ou voltar ao último estado válido conhecido (restaurar o banco de dados).'),
    (N'Integridad.litIntegridadDeDatos', N'Integridad de Datos', N'Data integrity', N'Integridade de dados'),
    (N'Integridad.litORestaurarUsandoUn', N'O restaurar usando un archivo de backup (.json) descargado previamente, en vez del último estado guardado internamente:', N'Or restore using a backup file (.json) downloaded earlier, instead of the last internally saved state:', N'Ou restaurar usando um arquivo de backup (.json) baixado anteriormente, em vez do último estado salvo internamente:'),
    (N'Integridad.litSinAlteraciones', N'No se detectaron alteraciones en ninguna de las 13 tablas de negocio.', N'No alterations were detected in any of the 13 business tables.', N'Nenhuma alteração foi detectada em nenhuma das 13 tabelas de negócio.'),
    (N'Integridad.msgAlteraciones', N'{0} detectada(s).', N'{0} detected.', N'{0} detectada(s).'),
    (N'Integridad.msgElegirBackup', N'Elegí un archivo de backup (.json) antes de restaurar.', N'Choose a backup file (.json) before restoring.', N'Escolha um arquivo de backup (.json) antes de restaurar.'),
    (N'Integridad.msgErrorBackup', N'No se pudo restaurar desde el backup: {0}', N'Could not restore from the backup: {0}', N'Não foi possível restaurar a partir do backup: {0}'),
    (N'Integridad.msgOk', N'Integridad verificada correctamente en las 13 tablas de negocio.', N'Integrity successfully verified in all 13 business tables.', N'Integridade verificada com sucesso nas 13 tabelas de negócio.'),
    (N'Integridad.msgUsuariosAfectados', N'Usuario(s) afectado(s): {0}.', N'Affected user(s): {0}.', N'Usuário(s) afetado(s): {0}.'),
    (N'Login.btnIngresar', N'Ingresar', N'Log in', N'Entrar'),
    (N'Login.litAviso', N'Aviso', N'Notice', N'Aviso'),
    (N'Login.litCerrar', N'Cerrar', N'Close', N'Fechar'),
    (N'Login.litContrasena', N'Contraseña', N'Password', N'Senha'),
    (N'Login.litNoDisponible', N'El sistema no se encuentra disponible en este momento, intente más tarde.', N'The system is not available right now, please try again later.', N'O sistema não está disponível no momento, tente mais tarde.'),
    (N'Login.litSubtitulo', N'Iniciá sesión para continuar', N'Log in to continue', N'Entre para continuar'),
    (N'Login.litTitulo', N'Sistema de Gestión Académica', N'Academic Management System', N'Sistema de Gestão Acadêmica'),
    (N'Login.litUsuario', N'Usuario', N'User', N'Usuário'),
    (N'Login.msgBloqueado', N'Tu cuenta fue bloqueada por demasiados intentos fallidos. Contactá a un administrador para desbloquearla.', N'Your account was blocked after too many failed attempts. Contact an administrator to unblock it.', N'Sua conta foi bloqueada por excesso de tentativas falhas. Contate um administrador para desbloqueá-la.'),
    (N'Login.msgCompletar', N'Completá usuario y contraseña.', N'Enter your user and password.', N'Preencha usuário e senha.'),
    (N'Login.msgCredenciales', N'Usuario o contraseña incorrectos.', N'Incorrect user or password.', N'Usuário ou senha incorretos.'),
    (N'Login.msgInactivo', N'Tu cuenta fue dada de baja. Contactá a un administrador.', N'Your account was deactivated. Contact an administrator.', N'Sua conta foi desativada. Contate um administrador.'),
    (N'Login.msgSesionActiva', N'Ya hay una sesión activa con este usuario. Cerrá la sesión anterior antes de volver a ingresar.', N'This user already has an active session. Close the previous session before logging in again.', N'Já existe uma sessão ativa com este usuário. Encerre a sessão anterior antes de entrar novamente.'),
    (N'Login.msgSinPermisos', N'Tu cuenta no tiene permisos asignados. Contactá a un administrador.', N'Your account has no permissions assigned. Contact an administrator.', N'Sua conta não tem permissões atribuídas. Contate um administrador.'),
    (N'Materias', N'Materias', N'Subjects', N'Matérias'),
    (N'Materias.btnCancelar', N'Cancelar', N'Cancel', N'Cancelar'),
    (N'Materias.btnExportarXml', N'Exportar XML', N'Export XML', N'Exportar XML'),
    (N'Materias.btnGuardar', N'Guardar', N'Save', N'Salvar'),
    (N'Materias.btnImportarXml', N'Importar XML', N'Import XML', N'Importar XML'),
    (N'Materias.btnImportarXml.confirmar', N'Se crearán o actualizarán materias según el archivo. ¿Continuar?', N'Subjects will be created or updated according to the file. Continue?', N'Matérias serão criadas ou atualizadas conforme o arquivo. Continuar?'),
    (N'Materias.btnNueva', N'+ Nueva Materia', N'+ New subject', N'+ Nova matéria'),
    (N'Materias.ddlModalidad.item.Presencial', N'Presencial', N'In person', N'Presencial'),
    (N'Materias.ddlModalidad.item.Virtual', N'Virtual', N'Virtual', N'Virtual'),
    (N'Materias.ddlPeso.item.1', N'1 - Muy baja', N'1 - Very low', N'1 - Muito baixa'),
    (N'Materias.ddlPeso.item.2', N'2 - Baja', N'2 - Low', N'2 - Baixa'),
    (N'Materias.ddlPeso.item.3', N'3 - Media', N'3 - Medium', N'3 - Média'),
    (N'Materias.ddlPeso.item.4', N'4 - Alta', N'4 - High', N'4 - Alta'),
    (N'Materias.ddlPeso.item.5', N'5 - Muy alta', N'5 - Very high', N'5 - Muito alta'),
    (N'Materias.gvMaterias.enc.Acciones', N'Acciones', N'Actions', N'Ações'),
    (N'Materias.gvMaterias.enc.Codigo', N'Código', N'Code', N'Código'),
    (N'Materias.gvMaterias.enc.Modalidad', N'Modalidad', N'Mode', N'Modalidade'),
    (N'Materias.gvMaterias.enc.Nombre', N'Nombre', N'Name', N'Nome'),
    (N'Materias.gvMaterias.enc.Peso', N'Peso', N'Weight', N'Peso'),
    (N'Materias.jsColCodigo', N'Código', N'Code', N'Código'),
    (N'Materias.jsColCorrelativas', N'Correlativas', N'Prerequisites', N'Correlativas'),
    (N'Materias.jsColModalidad', N'Modalidad', N'Mode', N'Modalidade'),
    (N'Materias.jsColNombre', N'Nombre', N'Name', N'Nome'),
    (N'Materias.jsColPeso', N'Peso', N'Weight', N'Peso'),
    (N'Materias.jsConsultando', N'Consultando…', N'Querying…', N'Consultando…'),
    (N'Materias.jsError', N'No se pudo consultar el servicio:', N'The service could not be queried:', N'Não foi possível consultar o serviço:'),
    (N'Materias.jsRecibidas', N'materias recibidas del servicio.', N'subjects received from the service.', N'matérias recebidas do serviço.'),
    (N'Materias.jsRespondio', N'El servicio respondió', N'The service responded', N'O serviço respondeu'),
    (N'Materias.litCodigo', N'Código', N'Code', N'Código'),
    (N'Materias.litConsultarMateriasservice', N'Consultar MateriasService', N'Query MateriasService', N'Consultar MateriasService'),
    (N'Materias.litModalidad', N'Modalidad', N'Mode', N'Modalidade'),
    (N'Materias.litNombre', N'Nombre', N'Name', N'Nome'),
    (N'Materias.litPeso', N'Peso (complejidad 1-5)', N'Weight (difficulty 1-5)', N'Peso (complexidade 1-5)'),
    (N'Materias.litTitulo', N'Gestión de materias', N'Subject management', N'Gestão de matérias'),
    (N'Materias.litWs', N'Plan de estudios vía Web Service', N'Study plan via Web Service', N'Plano de estudos via Web Service'),
    (N'Materias.litXml', N'Plan de estudios en XML', N'Study plan in XML', N'Plano de estudos em XML'),
    (N'Materias.lnkBaja', N'Baja', N'Low', N'Baixa'),
    (N'Materias.lnkBaja.confirmar', N'¿Eliminar esta materia?', N'Delete this subject?', N'Excluir esta matéria?'),
    (N'Materias.lnkEditar', N'Editar', N'Edit', N'Editar'),
    (N'Materias.msgBaja', N'✔ Materia dada de baja.', N'✔ Subject deactivated.', N'✔ Matéria desativada.'),
    (N'Materias.msgElegirXml', N'Elegí un archivo .xml para importar.', N'Choose an .xml file to import.', N'Escolha um arquivo .xml para importar.'),
    (N'Materias.msgError', N'✖ Error.', N'✖ Error.', N'✖ Erro.'),
    (N'Materias.msgErrorGuardar', N'✖ Error al guardar.', N'✖ Error while saving.', N'✖ Erro ao salvar.'),
    (N'Materias.msgGuardado', N'✔ Guardado.', N'✔ Saved.', N'✔ Salvo.'),
    (N'Materias.msgImportacionError', N'✖ No se importó nada:', N'✖ Nothing was imported:', N'✖ Nada foi importado:'),
    (N'Materias.msgImportacionOk', N'✔ Importación completa: {0} materias nuevas, {1} actualizadas, {2} correlativas agregadas.', N'✔ Import complete: {0} new subjects, {1} updated, {2} prerequisites added.', N'✔ Importação concluída: {0} matérias novas, {1} atualizadas, {2} correlativas adicionadas.'),
    (N'Materias.msgMasErrores', N'… y {0} errores más.', N'… and {0} more errors.', N'… e mais {0} erros.'),
    (N'Materias.tituloEditar', N'Editar Materia', N'Edit subject', N'Editar matéria'),
    (N'Materias.tituloNueva', N'Nueva Materia', N'New subject', N'Nova matéria'),
    (N'Menu', N'Inicio', N'Home', N'Início'),
    (N'Menu.litAbmDeAsignaturasY', N'ABM de asignaturas y correlativas.', N'Create, edit and delete subjects and prerequisites.', N'Cadastro de matérias e correlativas.'),
    (N'Menu.litBienvenido', N'Bienvenido,', N'Welcome,', N'Bem-vindo,'),
    (N'Menu.litBitacora', N'Bitácora', N'Audit log', N'Log de auditoria'),
    (N'Menu.litCalendario', N'Calendario', N'Calendar', N'Calendário'),
    (N'Menu.litDashboard', N'Dashboard', N'Dashboard', N'Painel'),
    (N'Menu.litDatosYDireccionEncriptad', N'Datos y dirección encriptada AES.', N'Your data and AES-encrypted address.', N'Dados e endereço criptografado com AES.'),
    (N'Menu.litDias', N'días', N'days', N'dias'),
    (N'Menu.litEditar', N'Editar', N'Edit', N'Editar'),
    (N'Menu.litEl', N'el', N'on', N'em'),
    (N'Menu.litEstadoYNotasDe', N'Estado y notas de cada materia.', N'Status and grades of each subject.', N'Situação e notas de cada matéria.'),
    (N'Menu.litEventosConMapaDe', N'Eventos con mapa de calor.', N'Events with a heat map.', N'Eventos com mapa de calor.'),
    (N'Menu.litExamenesProximos', N'⚠️ Exámenes próximos:', N'⚠️ Upcoming exams:', N'⚠️ Próximas provas:'),
    (N'Menu.litGestionar', N'Gestionar', N'Manage', N'Gerenciar'),
    (N'Menu.litGraficosYResumenVisual', N'Gráficos y resumen visual de tu situación.', N'Charts and a visual summary of your situation.', N'Gráficos e resumo visual da sua situação.'),
    (N'Menu.litInscribirme', N'Inscribirme', N'Enroll', N'Matricular-me'),
    (N'Menu.litInscribiteAlCuatrimestre', N'Inscribite al cuatrimestre en curso.', N'Enroll in the current term.', N'Matricule-se no semestre em curso.'),
    (N'Menu.litInscripcion', N'Inscripción', N'Enrollment', N'Matrícula'),
    (N'Menu.litMaterias', N'Materias', N'Subjects', N'Matérias'),
    (N'Menu.litMiPerfil', N'Mi Perfil', N'My profile', N'Meu perfil'),
    (N'Menu.litMisCursadas', N'Mis Cursadas', N'My courses', N'Minhas cursadas'),
    (N'Menu.litObjetivo700', N'Objetivo: 7.00', N'Goal: 7.00', N'Objetivo: 7.00'),
    (N'Menu.litPromedioPonderado', N'Promedio Ponderado', N'Weighted average', N'Média ponderada'),
    (N'Menu.litRegistroDeAccionesDel', N'Registro de acciones del sistema.', N'Record of system actions.', N'Registro de ações do sistema.'),
    (N'Menu.litVer', N'Ver', N'View', N'Ver'),
    (N'Menu.litVer2', N'Ver', N'View', N'Ver'),
    (N'Menu.litVer3', N'Ver', N'View', N'Ver'),
    (N'Menu.litVer4', N'Ver', N'View', N'Ver'),
    (N'Menu.objetivoAlcanzado', N'✔ Objetivo alcanzado', N'✔ Goal reached', N'✔ Objetivo alcançado'),
    (N'Menu.teFaltan', N'Te faltan {0} puntos para llegar a 7', N'You need {0} more points to reach 7', N'Faltam {0} pontos para chegar a 7'),
    (N'MisCursadas', N'Mis Cursadas', N'My courses', N'Minhas cursadas'),
    (N'MisCursadas.gvCursadas.enc.Codigo', N'Código', N'Code', N'Código'),
    (N'MisCursadas.gvCursadas.enc.DVH', N'DVH', N'DVH', N'DVH'),
    (N'MisCursadas.gvCursadas.enc.Estado', N'Estado', N'Status', N'Situação'),
    (N'MisCursadas.gvCursadas.enc.Final', N'Final', N'Final', N'Final'),
    (N'MisCursadas.gvCursadas.enc.Materia', N'Materia', N'Subject', N'Matéria'),
    (N'MisCursadas.gvCursadas.enc.Modalidad', N'Modalidad', N'Mode', N'Modalidade'),
    (N'MisCursadas.gvCursadas.enc.Parcial1', N'Parcial 1', N'Midterm 1', N'Parcial 1'),
    (N'MisCursadas.gvCursadas.enc.Parcial2', N'Parcial 2', N'Midterm 2', N'Parcial 2'),
    (N'MisCursadas.gvCursadas.enc.Recup', N'Recup.', N'Make-up', N'Recup.'),
    (N'MisCursadas.gvCursadas.enc.Riesgo', N'Riesgo', N'Risk', N'Risco'),
    (N'MisCursadas.gvCursadas.enc.Situacion', N'Situación', N'Situation', N'Situação'),
    (N'MisCursadas.litMisCursadas', N'Mis Cursadas', N'My courses', N'Minhas cursadas'),
    (N'Msg.0digitosverificadoresverticalesausentes', N'{0}: dígitos verificadores verticales ausentes.', N'{0}: vertical check digits missing.', N'{0}: dígitos verificadores verticais ausentes.'),
    (N'Msg.0digitoverificadorverticalausenteparalacolumna1', N'{0}: dígito verificador vertical ausente para la columna {1}.', N'{0}: vertical check digit missing for column {1}.', N'{0}: dígito verificador vertical ausente para a coluna {1}.'),
    (N'Msg.0DVVinvalidoenlacolumna1esperado2guardado3', N'{0}: DVV inválido en la columna {1} (esperado {2}, guardado {3}).', N'{0}: invalid DVV in column {1} (expected {2}, stored {3}).', N'{0}: DVV inválido na coluna {1} (esperado {2}, salvo {3}).'),
    (N'Msg.0elsnapshotestavacionoseborraronlas1filasactuales', N'{0}: el snapshot está vacío, no se borraron las {1} fila(s) actuales.', N'{0}: the snapshot is empty, the {1} current row(s) were not deleted.', N'{0}: o snapshot está vazio, as {1} linha(s) atuais não foram apagadas.'),
    (N'Msg.0hayalmenosunregistroconDVHinvalidoperonosepudoidentificarelcampoalteradodigitosverificado', N'{0}: hay al menos un registro con DVH inválido, pero no se pudo identificar el campo alterado (dígitos verificadores ausentes o no inicializados para la tabla, o el propio DVH fue modificado directamente). Recalculá la integridad de la tabla para refinar el diagnóstico.', N'{0}: at least one record has an invalid DVH, but the altered field could not be identified (check digits missing or not initialized for the table, or the DVH itself was modified directly). Recalculate the table integrity to refine the diagnosis.', N'{0}: há pelo menos um registro com DVH inválido, mas não foi possível identificar o campo alterado (dígitos verificadores ausentes ou não inicializados para a tabela, ou o próprio DVH foi modificado diretamente). Recalcule a integridade da tabela para refinar o diagnóstico.'),
    (N'Msg.0seagregoelregistro1noestabaenelultimoestadovalidoconocido', N'{0}: se agregó el registro {1} (no estaba en el último estado válido conocido).', N'{0}: record {1} was added (it was not in the last known valid state).', N'{0}: o registro {1} foi adicionado (não estava no último estado válido conhecido).'),
    (N'Msg.0seeliminoelregistro1', N'{0}: se eliminó el registro {1}.', N'{0}: record {1} was deleted.', N'{0}: o registro {1} foi excluído.'),
    (N'Msg.0semodificoelcampo1delregistro2antes3ahora4', N'{0}: se modificó el campo {1} del registro {2} (antes: ''{3}'', ahora: ''{4}'').', N'{0}: field {1} of record {2} was modified (before: ''{3}'', now: ''{4}'').', N'{0}: o campo {1} do registro {2} foi modificado (antes: ''{3}'', agora: ''{4}'').'),
    (N'Msg.Cambiosguardados', N'Cambios guardados.', N'Changes saved.', N'Alterações salvas.'),
    (N'Msg.Contrasenarestablecida', N'Contraseña restablecida.', N'Password reset.', N'Senha redefinida.'),
    (N'Msg.Elarchivoestavacio', N'El archivo está vacío.', N'The file is empty.', N'O arquivo está vazio.'),
    (N'Msg.Elarchivonocontienematerias', N'El archivo no contiene materias.', N'The file contains no subjects.', N'O arquivo não contém matérias.'),
    (N'Msg.ElarchivonoesunXMLvalidolinea0', N'El archivo no es un XML válido (línea {0}).', N'The file is not valid XML (line {0}).', N'O arquivo não é um XML válido (linha {0}).'),
    (N'Msg.ElarchivonotieneelformatodebackupesperadofaltaTablas', N'El archivo no tiene el formato de backup esperado (falta ''Tablas'').', N'The file does not have the expected backup format (''Tablas'' is missing).', N'O arquivo não tem o formato de backup esperado (falta ''Tablas'').'),
    (N'Msg.Elarchivosuperaelmaximode1MB', N'El archivo supera el máximo de 1 MB.', N'The file exceeds the 1 MB maximum.', N'O arquivo excede o máximo de 1 MB.'),
    (N'Msg.ElbackupesanterioralosperfilesdinamicosnoincluyeROLPERMISOROLPERMISOniUSUARIOPERFILEsastab', N'El backup es anterior a los perfiles dinámicos: no incluye ROL, PERMISO, ROL_PERMISO ni USUARIO_PERFIL. Esas tablas se dejaron como estaban.', N'The backup predates dynamic profiles: it does not include ROL, PERMISO, ROL_PERMISO or USUARIO_PERFIL. Those tables were left as they were.', N'O backup é anterior aos perfis dinâmicos: não inclui ROL, PERMISO, ROL_PERMISO nem USUARIO_PERFIL. Essas tabelas foram mantidas como estavam.'),
    (N'Msg.Elbackupnocontienedatosparalatabla0', N'El backup no contiene datos para la tabla {0}.', N'The backup contains no data for table {0}.', N'O backup não contém dados para a tabela {0}.'),
    (N'Msg.ElbackuptieneunformatoanteriordeUSUARIOseadaptolosusuariosquedanactivos', N'El backup tiene un formato anterior de USUARIO: se adaptó (los usuarios quedan activos).', N'The backup has an older USUARIO format: it was adapted (users stay active).', N'O backup tem um formato anterior de USUARIO: foi adaptado (os usuários ficam ativos).'),
    (N'Msg.ElbackuptieneunformatoanteriordeUSUARIOseadaptolosusuariosquedanactivosylosquenotienenperf', N'El backup tiene un formato anterior de USUARIO: se adaptó (los usuarios quedan activos y los que no tienen perfil se reasignan según su rol de entonces).', N'The backup has an older USUARIO format: it was adapted (users stay active and those without a profile are reassigned according to their role at the time).', N'O backup tem um formato anterior de USUARIO: foi adaptado (os usuários ficam ativos e os que não têm perfil são reatribuídos conforme o papel que tinham).'),
    (N'Msg.ElbackuptieneunformatodeUSUARIOquenosereconocenoserestauronada', N'El backup tiene un formato de USUARIO que no se reconoce; no se restauró nada.', N'The backup has a USUARIO format that is not recognized; nothing was restored.', N'O backup tem um formato de USUARIO que não é reconhecido; nada foi restaurado.'),
    (N'Msg.Elcambiodejariaalsistemasinningunusuarioactivoynobloqueadoconpermisoparagestionarperfiles', N'El cambio dejaría al sistema sin ningún usuario activo y no bloqueado con permiso para gestionar perfiles.', N'The change would leave the system without any active, unblocked user allowed to manage profiles.', N'A alteração deixaria o sistema sem nenhum usuário ativo e não bloqueado com permissão para gerenciar perfis.'),
    (N'Msg.Elcambioseguardoperofallounpasoposteriorintegridadobitacora0', N'El cambio se guardó, pero falló un paso posterior (integridad o bitácora): {0}', N'The change was saved, but a later step failed (integrity or audit log): {0}', N'A alteração foi salva, mas uma etapa posterior falhou (integridade ou log de auditoria): {0}'),
    (N'Msg.Elcodigodemateriayaexiste', N'El código de materia ya existe.', N'The subject code already exists.', N'O código da disciplina já existe.'),
    (N'Msg.Elegialmenosunperfil', N'Elegí al menos un perfil.', N'Choose at least one profile.', N'Escolha pelo menos um perfil.'),
    (N'Msg.ElelementoraizdebeserMaterias', N'El elemento raíz debe ser <Materias>.', N'The root element must be <Materias>.', N'O elemento raiz deve ser <Materias>.'),
    (N'Msg.Elidiomanoexiste', N'El idioma no existe.', N'The language does not exist.', N'O idioma não existe.'),
    (N'Msg.Elnombredelidiomaesobligatorioyadmitehasta50caracteres', N'El nombre del idioma es obligatorio y admite hasta 50 caracteres.', N'The language name is required and allows up to 50 characters.', N'O nome do idioma é obrigatório e admite até 50 caracteres.'),
    (N'Msg.Elnombredelperfilesobligatorio', N'El nombre del perfil es obligatorio.', N'The profile name is required.', N'O nome do perfil é obrigatório.'),
    (N'Msg.Elnombredelperfilnopuedesuperarlos100caracteres', N'El nombre del perfil no puede superar los 100 caracteres.', N'The profile name cannot exceed 100 characters.', N'O nome do perfil não pode ultrapassar 100 caracteres.'),
    (N'Msg.Elnombredeusuarioesobligatorio', N'El nombre de usuario es obligatorio.', N'The user name is required.', N'O nome de usuário é obrigatório.'),
    (N'Msg.Elnombredeusuarionopuedesuperarlos50caracteres', N'El nombre de usuario no puede superar los 50 caracteres.', N'The user name cannot exceed 50 characters.', N'O nome de usuário não pode ultrapassar 50 caracteres.'),
    (N'Msg.Elperfil0esdelsistemanosepuede1', N'El perfil {0} es del sistema: no se puede {1}.', N'Profile {0} is a system profile: you cannot {1}.', N'O perfil {0} é do sistema: não é possível {1}.'),
    (N'Msg.Elperfilnoexiste', N'El perfil no existe.', N'The profile does not exist.', N'O perfil não existe.'),
    (N'Msg.Elperfiltieneperfileshijosmoverlosoeliminarlosprimero', N'El perfil tiene perfiles hijos: moverlos o eliminarlos primero.', N'The profile has child profiles: move or delete them first.', N'O perfil tem perfis filhos: mova-os ou exclua-os primeiro.'),
    (N'Msg.Elperfiltieneusuariosasignadosreasignarlosprimero', N'El perfil tiene usuarios asignados: reasignarlos primero.', N'The profile has assigned users: reassign them first.', N'O perfil tem usuários atribuídos: reatribua-os primeiro.'),
    (N'Msg.Elusuariodebeteneralmenosunperfil', N'El usuario debe tener al menos un perfil.', N'The user must have at least one profile.', N'O usuário deve ter pelo menos um perfil.'),
    (N'Msg.Elusuarionoexiste', N'El usuario no existe.', N'The user does not exist.', N'O usuário não existe.'),
    (N'Msg.Erroralregistrarlainscripcion', N'Error al registrar la inscripción.', N'Error while registering the enrollment.', N'Erro ao registrar a matrícula.'),
    (N'Msg.Lasnotasdebensernumerosentre0y10', N'Las notas deben ser números entre 0 y 10.', N'Grades must be numbers between 0 and 10.', N'As notas devem ser números entre 0 e 10.'),
    (N'Msg.Nopodescambiartuspropiosperfiles', N'No podés cambiar tus propios perfiles.', N'You cannot change your own profiles.', N'Você não pode alterar seus próprios perfis.'),
    (N'Msg.Secompletolaoperacionperonosepudoregistrarenlabitacora', N'Se completó la operación, pero no se pudo registrar en la bitácora.', N'The operation completed, but it could not be logged in the audit log.', N'A operação foi concluída, mas não foi possível registrá-la na bitácora.'),
    (N'Msg.Yaexisteunperiodoparaeseanioycuatrimestre', N'Ya existe un período para ese año y cuatrimestre.', N'A period already exists for that year and term.', N'Já existe um período para esse ano e quadrimestre.'),
    (N'Msg.errorInesperado', N'Ocurrió un error inesperado al guardar. Revisá el estado y volvé a intentar.', N'An unexpected error occurred while saving. Check the status and try again.', N'Ocorreu um erro inesperado ao salvar. Verifique o estado e tente novamente.'),
    (N'Msg.Inscripcioncompletada0materiasregistradas', N'✔ Inscripción completada: {0} materia(s) registrada(s).', N'✔ Enrollment completed: {0} subject(s) registered.', N'✔ Matrícula concluída: {0} matéria(s) registrada(s).'),
    (N'Msg.Lacontrasenadebetener6caracteres1mayusculay1numero', N'La contraseña debe tener 6+ caracteres, 1 mayúscula y 1 número.', N'The password must have 6+ characters, 1 uppercase letter and 1 number.', N'A senha deve ter 6+ caracteres, 1 maiúscula e 1 número.'),
    (N'Msg.Lacontrasenaesobligatoria', N'La contraseña es obligatoria.', N'The password is required.', N'A senha é obrigatória.'),
    (N'Msg.Ladireccionesdemasiadolargamaximo0caractereslosacentosysimbolosocupanmas', N'La dirección es demasiado larga (máximo {0} caracteres; los acentos y símbolos ocupan más).', N'The address is too long (maximum {0} characters; accents and symbols take up more).', N'O endereço é longo demais (máximo de {0} caracteres; acentos e símbolos ocupam mais).'),
    (N'Msg.Lascontrasenasnocoinciden', N'Las contraseñas no coinciden.', N'The passwords do not match.', N'As senhas não coincidem.'),
    (N'Msg.Lascorrelativasformanunciclo0', N'Las correlativas forman un ciclo: {0}.', N'The prerequisites form a cycle: {0}.', N'As correlativas formam um ciclo: {0}.'),
    (N'Msg.Losperfilesseguardaronperofallolaactualizaciondeladireccion0', N'Los perfiles se guardaron, pero falló la actualización de la dirección ({0}).', N'The profiles were saved, but updating the address failed ({0}).', N'Os perfis foram salvos, mas a atualização do endereço falhou ({0}).'),
    (N'Msg.Materia0elcodigoesobligatorioyadmitehasta20caracteres', N'Materia {0}: el código es obligatorio y admite hasta 20 caracteres.', N'Subject {0}: the code is required and allows up to 20 characters.', N'Matéria {0}: o código é obrigatório e admite até 20 caracteres.'),
    (N'Msg.Materia0elcodigoestarepetidoenelarchivo', N'Materia {0}: el código está repetido en el archivo.', N'Subject {0}: the code is repeated in the file.', N'Matéria {0}: o código está repetido no arquivo.'),
    (N'Msg.Materia0elnombreesobligatorioyadmitehasta100caracteres', N'Materia {0}: el nombre es obligatorio y admite hasta 100 caracteres.', N'Subject {0}: the name is required and allows up to 100 characters.', N'Matéria {0}: o nome é obrigatório e admite até 100 caracteres.'),
    (N'Msg.Materia0elpesodebeserunnumeroentre1y5', N'Materia {0}: el peso debe ser un número entre 1 y 5.', N'Subject {0}: the weight must be a number between 1 and 5.', N'Matéria {0}: o peso deve ser um número entre 1 e 5.'),
    (N'Msg.Materia0hayunacorrelativasincodigo', N'Materia {0}: hay una correlativa sin código.', N'Subject {0}: there is a prerequisite without a code.', N'Matéria {0}: há uma correlativa sem código.'),
    (N'Msg.Materia0lacorrelativa1noexistenienelarchivonienelsistema', N'Materia {0}: la correlativa {1} no existe ni en el archivo ni en el sistema.', N'Subject {0}: prerequisite {1} exists neither in the file nor in the system.', N'Matéria {0}: a correlativa {1} não existe nem no arquivo nem no sistema.'),
    (N'Msg.Materia0lamodalidaddebeserPresencialoVirtual', N'Materia {0}: la modalidad debe ser Presencial o Virtual.', N'Subject {0}: the mode must be In person or Virtual.', N'Matéria {0}: a modalidade deve ser Presencial ou Virtual.'),
    (N'Msg.Materia0nopuedesercorrelativadesimisma', N'Materia {0}: no puede ser correlativa de sí misma.', N'Subject {0}: it cannot be a prerequisite of itself.', N'Matéria {0}: não pode ser correlativa de si mesma.'),
    (N'Msg.Materiaaprobada', N'✔ Materia aprobada.', N'✔ Subject passed.', N'✔ Matéria aprovada.'),
    (N'Msg.Nohayunusuarioquerealicelaoperacion', N'No hay un usuario que realice la operación.', N'There is no user performing the operation.', N'Não há um usuário realizando a operação.'),
    (N'Msg.NopodescambiartuspropiosperfilesdesdeUsuarios', N'No podés cambiar tus propios perfiles desde Usuarios.', N'You cannot change your own profiles from Users.', N'Você não pode alterar seus próprios perfis em Usuários.'),
    (N'Msg.Nopodesdartedebajaavosmismo', N'No podés darte de baja a vos mismo.', N'You cannot deactivate yourself.', N'Você não pode desativar a si mesmo.'),
    (N'Msg.Nopodesinscribirtea0tenescorrelativaspendientes', N'No podés inscribirte a ''{0}'': tenés correlativas pendientes.', N'You cannot enroll in ''{0}'': you have pending prerequisites.', N'Você não pode se matricular em ''{0}'': tem correlativas pendentes.'),
    (N'Msg.Noseencontroelperfil0paraelusuario1', N'No se encontró el perfil {0} para el usuario {1}.', N'Profile {0} was not found for user {1}.', N'O perfil {0} não foi encontrado para o usuário {1}.'),
    (N'Msg.Noseguardofallolaactualizaciondeladireccion0', N'No se guardó: falló la actualización de la dirección ({0}).', N'Nothing was saved: updating the address failed ({0}).', N'Nada foi salvo: a atualização do endereço falhou ({0}).'),
    (N'Msg.Nosepudieronsembrarlosdatosbaseocalcularsuintegridadinicial0', N'No se pudieron sembrar los datos base o calcular su integridad inicial: {0}', N'The base data could not be seeded or its initial integrity could not be calculated: {0}', N'Não foi possível semear os dados base nem calcular sua integridade inicial: {0}'),
    (N'Msg.NosepudoimportarynoseguardoninguncambioRevisaqueninguncodigocoincidaconunamateriadadadebaj', N'No se pudo importar y no se guardó ningún cambio. Revisá que ningún código coincida con una materia dada de baja.', N'The import failed and nothing was saved. Check that no code matches a deactivated subject.', N'Não foi possível importar e nada foi salvo. Verifique se nenhum código coincide com uma matéria desativada.'),
    (N'Msg.Nosepudorestablecerlacontrasena', N'No se pudo restablecer la contraseña.', N'The password could not be reset.', N'Não foi possível redefinir a senha.'),
    (N'Msg.Nosepuedeasignaresepadregenerariaunareferenciacircular', N'No se puede asignar ese padre: generaría una referencia circular.', N'That parent cannot be assigned: it would create a circular reference.', N'Não é possível atribuir esse pai: geraria uma referência circular.'),
    (N'Msg.Nosepuededeshabilitarelidiomapredeterminadodelsistema', N'No se puede deshabilitar el idioma predeterminado del sistema.', N'The system''s default language cannot be disabled.', N'O idioma padrão do sistema não pode ser desativado.'),
    (N'Msg.Nosepuededeshabilitarunidiomaqueestaenusoporalgunusuario', N'No se puede deshabilitar un idioma que está en uso por algún usuario.', N'A language that is in use by a user cannot be disabled.', N'Um idioma que está em uso por algum usuário não pode ser desativado.'),
    (N'Msg.Nosepuedeeliminarelidiomapredeterminadodelsistema', N'No se puede eliminar el idioma predeterminado del sistema.', N'The system''s default language cannot be deleted.', N'O idioma padrão do sistema não pode ser excluído.'),
    (N'Msg.Nosepuedeeliminarunidiomaqueestaenusoporalgunusuario', N'No se puede eliminar un idioma que está en uso por algún usuario.', N'A language that is in use by a user cannot be deleted.', N'Um idioma que está em uso por algum usuário não pode ser excluído.'),
    (N'Msg.Notenespermisoparaasignarperfiles', N'No tenés permiso para asignar perfiles.', N'You do not have permission to assign profiles.', N'Você não tem permissão para atribuir perfis.'),
    (N'Msg.Notenespermisoparadesbloquearusuarios', N'No tenés permiso para desbloquear usuarios.', N'You do not have permission to unblock users.', N'Você não tem permissão para desbloquear usuários.'),
    (N'Msg.Notenespermisoparagestionarperfiles', N'No tenés permiso para gestionar perfiles.', N'You do not have permission to manage profiles.', N'Você não tem permissão para gerenciar perfis.'),
    (N'Msg.Notenespermisoparagestionarusuarios', N'No tenés permiso para gestionar usuarios.', N'You do not have permission to manage users.', N'Você não tem permissão para gerenciar usuários.'),
    (N'Msg.Notenespermisoparaverusuarios', N'No tenés permiso para ver usuarios.', N'You do not have permission to view users.', N'Você não tem permissão para ver usuários.'),
    (N'Msg.Padreactualizado', N'Padre actualizado.', N'Parent updated.', N'Pai atualizado.'),
    (N'Msg.Parcial1conmargenjusto0Necesitasalmenos6enelParcial2', N'🟡 Parcial 1 con margen justo ({0}). Necesitás al menos 6 en el Parcial 2.', N'🟡 Midterm 1 with a narrow margin ({0}). You need at least 6 in Midterm 2.', N'🟡 Parcial 1 com margem apertada ({0}). Você precisa de pelo menos 6 no Parcial 2.'),
    (N'Msg.Parcial1desaprobado0Necesitasiralrecuperatorio', N'🔴 Parcial 1 desaprobado ({0}). Necesitás ir al recuperatorio.', N'🔴 Midterm 1 failed ({0}). You need to take the make-up exam.', N'🔴 Parcial 1 reprovado ({0}). Você precisa fazer a recuperação.'),
    (N'Msg.Parcial2desaprobado0Necesitasiralrecuperatorio', N'🔴 Parcial 2 desaprobado ({0}). Necesitás ir al recuperatorio.', N'🔴 Midterm 2 failed ({0}). You need to take the make-up exam.', N'🔴 Parcial 2 reprovado ({0}). Você precisa fazer a recuperação.'),
    (N'Msg.Perfilcreado', N'Perfil creado.', N'Profile created.', N'Perfil criado.'),
    (N'Msg.Perfilcreadodentrodelseleccionado', N'Perfil creado dentro del seleccionado.', N'Profile created inside the selected one.', N'Perfil criado dentro do selecionado.'),
    (N'Msg.Perfileliminado', N'Perfil eliminado.', N'Profile deleted.', N'Perfil excluído.'),
    (N'Msg.Perfilrenombrado', N'Perfil renombrado.', N'Profile renamed.', N'Perfil renomeado.'),
    (N'Msg.Permisosguardados', N'Permisos guardados.', N'Permissions saved.', N'Permissões salvas.'),
    (N'Msg.Promedioaceptable', N'🟡 Promedio aceptable', N'🟡 Acceptable average', N'🟡 Média aceitável'),
    (N'Msg.Promediobajorevisatusituacion', N'🔴 Promedio bajo, revisá tu situación', N'🔴 Low average, review your situation', N'🔴 Média baixa, revise sua situação'),
    (N'Msg.Promedioexcelente', N'🟢 Promedio excelente', N'🟢 Excellent average', N'🟢 Média excelente'),
    (N'Msg.Seindicounperfilquenoexiste', N'Se indicó un perfil que no existe.', N'A profile that does not exist was specified.', N'Foi indicado um perfil que não existe.'),
    (N'Msg.Seindicounpermisoquenoexiste', N'Se indicó un permiso que no existe.', N'A permission that does not exist was specified.', N'Foi indicada uma permissão que não existe.'),
    (N'Msg.Seleccionaalmenosunamateria', N'Seleccioná al menos una materia.', N'Select at least one subject.', N'Selecione pelo menos uma matéria.'),
    (N'Msg.sincodigo', N'(sin código)', N'(no code)', N'(sem código)'),
    (N'Msg.Sinmateriasfinalizadastodavia', N'Sin materias finalizadas todavía.', N'No finished subjects yet.', N'Nenhuma matéria concluída ainda.'),
    (N'Msg.Sinnotascargadastodavia', N'🟡 Sin notas cargadas todavía.', N'🟡 No grades recorded yet.', N'🟡 Nenhuma nota lançada ainda.'),
    (N'Msg.Tenesunexamenenmenosde7diasynohaynotascargadas', N'🔴 Tenés un examen en menos de 7 días y no hay notas cargadas.', N'🔴 You have an exam in less than 7 days and no grades are recorded.', N'🔴 Você tem uma prova em menos de 7 dias e não há notas lançadas.'),
    (N'Msg.Tucuentanoestahabilitadapararealizarestaoperacion', N'Tu cuenta no está habilitada para realizar esta operación.', N'Your account is not enabled to perform this operation.', N'Sua conta não está habilitada para realizar esta operação.'),
    (N'Msg.TupropiacontrasenasecambiadesdeContrasena', N'Tu propia contraseña se cambia desde Contraseña.', N'Your own password is changed from Password.', N'Sua própria senha é alterada em Senha.'),
    (N'Msg.Unatraduccionsuperalos500caracteres', N'Una traducción supera los 500 caracteres.', N'A translation exceeds 500 characters.', N'Uma tradução excede 500 caracteres.'),
    (N'Msg.Usuariocreado', N'Usuario creado.', N'User created.', N'Usuário criado.'),
    (N'Msg.Usuariodadodebaja', N'Usuario dado de baja.', N'User deactivated.', N'Usuário desativado.'),
    (N'Msg.Usuarioreactivado', N'Usuario reactivado.', N'User reactivated.', N'Usuário reativado.'),
    (N'Msg.VasbienSeguiasi', N'🟢 Vas bien. Seguí así.', N'🟢 You''re doing well. Keep it up.', N'🟢 Você está indo bem. Continue assim.'),
    (N'Msg.Yaexisteunperfilconesenombre', N'Ya existe un perfil con ese nombre.', N'A profile with that name already exists.', N'Já existe um perfil com esse nome.'),
    (N'Msg.Yaexisteunusuarioconesenombre', N'Ya existe un usuario con ese nombre.', N'A user with that name already exists.', N'Já existe um usuário com esse nome.'),
    (N'Notas', N'Notas', N'Grades', N'Notas'),
    (N'Notas.btnCancelarNotas', N'Cancelar', N'Cancel', N'Cancelar'),
    (N'Notas.btnGuardarNotas', N'Guardar', N'Save', N'Salvar'),
    (N'Notas.ddlEstado.item.Aprobada', N'Aprobada', N'Passed', N'Aprovada'),
    (N'Notas.ddlEstado.item.Cursando', N'Cursando', N'In progress', N'Cursando'),
    (N'Notas.ddlEstado.item.FinalPendiente', N'Final Pendiente', N'Final exam pending', N'Final pendente'),
    (N'Notas.gvCursadas.enc.Acciones', N'Acciones', N'Actions', N'Ações'),
    (N'Notas.gvCursadas.enc.Codigo', N'Código', N'Code', N'Código'),
    (N'Notas.gvCursadas.enc.DVH', N'DVH', N'DVH', N'DVH'),
    (N'Notas.gvCursadas.enc.Estado', N'Estado', N'Status', N'Situação'),
    (N'Notas.gvCursadas.enc.Final', N'Final', N'Final', N'Final'),
    (N'Notas.gvCursadas.enc.Materia', N'Materia', N'Subject', N'Matéria'),
    (N'Notas.gvCursadas.enc.Modalidad', N'Modalidad', N'Mode', N'Modalidade'),
    (N'Notas.gvCursadas.enc.Parcial1', N'Parcial 1', N'Midterm 1', N'Parcial 1'),
    (N'Notas.gvCursadas.enc.Parcial2', N'Parcial 2', N'Midterm 2', N'Parcial 2'),
    (N'Notas.gvCursadas.enc.Recup', N'Recup.', N'Make-up', N'Recup.'),
    (N'Notas.litActualizarNotas', N'Actualizar Notas', N'Update grades', N'Atualizar notas'),
    (N'Notas.litAlumno', N'Alumno', N'Student', N'Aluno'),
    (N'Notas.litEstado', N'Estado', N'Status', N'Situação'),
    (N'Notas.litFechaMesaFinal', N'Fecha Mesa Final', N'Final exam date', N'Data da prova final'),
    (N'Notas.litFechaRecuperatorio', N'Fecha Recuperatorio', N'Make-up exam date', N'Data da recuperação'),
    (N'Notas.litNotaFinal', N'🎓 Nota Final', N'🎓 Final grade', N'🎓 Nota final'),
    (N'Notas.litNotaParcial1', N'Nota Parcial 1', N'Midterm 1 grade', N'Nota da prova 1'),
    (N'Notas.litNotaParcial2', N'Nota Parcial 2', N'Midterm 2 grade', N'Nota da prova 2'),
    (N'Notas.litNotas', N'Notas', N'Grades', N'Notas'),
    (N'Notas.litRecuperatorio', N'⚠️ Recuperatorio', N'⚠️ Make-up exam', N'⚠️ Recuperação'),
    (N'Notas.msgActualizadas', N'✔ Notas actualizadas.', N'✔ Grades updated.', N'✔ Notas atualizadas.'),
    (N'Notas.msgError', N'✖ Error.', N'✖ Error.', N'✖ Erro.'),
    (N'Notas.txtNotaFinal.placeholder', N'0-10', N'0-10', N'0-10'),
    (N'Notas.txtParcial1.placeholder', N'0-10', N'0-10', N'0-10'),
    (N'Notas.txtParcial2.placeholder', N'0-10', N'0-10', N'0-10'),
    (N'Notas.txtRecuperatorio.placeholder', N'0-10', N'0-10', N'0-10'),
    (N'Paginador.btnAnterior', N'Anterior', N'Previous', N'Anterior'),
    (N'Paginador.btnPrimera', N'Primera', N'First', N'Primeira'),
    (N'Paginador.btnSiguiente', N'Siguiente', N'Next', N'Próxima'),
    (N'Paginador.btnUltima', N'Última', N'Last', N'Última'),
    (N'Paginador.pagina', N'Página {0} de {1} ({2} registros)', N'Page {0} of {1} ({2} records)', N'Página {0} de {1} ({2} registros)'),
    (N'Paginador.sinRegistros', N'Sin registros', N'No records', N'Sem registros'),
    (N'Perfil', N'Mi Perfil', N'My profile', N'Meu perfil'),
    (N'Perfil.btnGuardar', N'Guardar Cambios', N'Save changes', N'Salvar alterações'),
    (N'Perfil.litAes256', N'🔒 AES-256', N'🔒 AES-256', N'🔒 AES-256'),
    (N'Perfil.litDireccionUbicacion', N'Dirección / Ubicación', N'Address / Location', N'Endereço / Localização'),
    (N'Perfil.litMiPerfil', N'Mi Perfil', N'My profile', N'Meu perfil'),
    (N'Perfil.litTuDireccionSeAlmacena', N'Tu dirección se almacena encriptada con AES-256. Se usa para calcular el tiempo de viaje a clases presenciales (RF08).', N'Your address is stored encrypted with AES-256. It is used to calculate travel time to in-person classes (RF08).', N'Seu endereço é armazenado criptografado com AES-256. É usado para calcular o tempo de viagem até as aulas presenciais (RF08).'),
    (N'Perfil.litUsuario', N'Usuario', N'User', N'Usuário'),
    (N'Perfil.msgError', N'✖ Error al guardar.', N'✖ Error while saving.', N'✖ Erro ao salvar.'),
    (N'Perfil.msgGuardada', N'✔ Dirección guardada con AES-256.', N'✔ Address saved with AES-256.', N'✔ Endereço salvo com AES-256.'),
    (N'Perfil.txtDireccion.placeholder', N'Ej: Av. Corrientes 1234, CABA', N'E.g. 1234 Corrientes Ave., CABA', N'Ex.: Av. Corrientes 1234, CABA'),
    (N'Perfiles', N'Perfiles', N'Profiles', N'Perfis'),
    (N'Perfiles.btnCambiarPadre', N'Cambiar padre', N'Change parent', N'Alterar pai'),
    (N'Perfiles.btnCrearHijo', N'Crear dentro del seleccionado', N'Create inside the selected one', N'Criar dentro do selecionado'),
    (N'Perfiles.btnCrearRaiz', N'Crear en la raíz', N'Create at the root', N'Criar na raiz'),
    (N'Perfiles.btnEliminar', N'Eliminar perfil', N'Delete profile', N'Excluir perfil'),
    (N'Perfiles.btnEliminar.confirmar', N'¿Eliminar este perfil?', N'Delete this profile?', N'Excluir este perfil?'),
    (N'Perfiles.btnGuardarPermisos', N'Guardar permisos', N'Save permissions', N'Salvar permissões'),
    (N'Perfiles.btnRenombrar', N'Renombrar', N'rename', N'renomear'),
    (N'Perfiles.litArbolDePerfiles', N'Árbol de perfiles', N'Profile tree', N'Árvore de perfis'),
    (N'Perfiles.litNuevoPerfil', N'Nuevo perfil', N'New profile', N'Novo perfil'),
    (N'Perfiles.litPerfilesYPermisos', N'Perfiles y permisos', N'Profiles and permissions', N'Perfis e permissões'),
    (N'Perfiles.litPermisoPropioDelPerfil', N'Permiso propio del perfil. Un perfil incluye también los permisos de los perfiles que contiene.', N'Permission owned by the profile. A profile also includes the permissions of the profiles it contains.', N'Permissão própria do perfil. Um perfil inclui também as permissões dos perfis que contém.'),
    (N'Perfiles.litPermisosEfectivos', N'Permisos efectivos', N'Effective permissions', N'Permissões efetivas'),
    (N'Perfiles.litPermisosPropios', N'Permisos propios', N'Own permissions', N'Permissões próprias'),
    (N'Perfiles.litPropiosHeredadosDeLos', N'(propios + heredados de los perfiles que contiene)', N'(own + inherited from the profiles it contains)', N'(próprias + herdadas dos perfis que contém)'),
    (N'Perfiles.litSeleccionaUnPerfil', N'Seleccioná un perfil del árbol para editarlo.', N'Select a profile from the tree to edit it.', N'Selecione um perfil da árvore para editá-lo.'),
    (N'Perfiles.sinPadre', N'(raíz — sin padre)', N'(root — no parent)', N'(raiz — sem pai)'),
    (N'Perfiles.txtNuevoNombre.placeholder', N'Nombre del perfil', N'Profile name', N'Nome do perfil'),
    (N'SelectorIdioma.litIdioma', N'Idioma:', N'Language:', N'Idioma:'),
    (N'Site.btnCerrarSesion', N'Cerrar sesión', N'Log out', N'Sair'),
    (N'Site.litMarca', N'Sistema de Gestión Académico', N'Academic Management System', N'Sistema de Gestão Acadêmica'),
    (N'Site.litNavAsignarPerfiles', N'Asignar perfiles', N'Assign profiles', N'Atribuir perfis'),
    (N'Site.litNavBitacora', N'Bitácora', N'Audit log', N'Log de auditoria'),
    (N'Site.litNavBloqueados', N'Usuarios bloqueados', N'Blocked users', N'Usuários bloqueados'),
    (N'Site.litNavCalendario', N'Calendario', N'Calendar', N'Calendário'),
    (N'Site.litNavContrasena', N'Contraseña', N'Password', N'Senha'),
    (N'Site.litNavDashboard', N'Dashboard', N'Dashboard', N'Painel'),
    (N'Site.litNavIdiomas', N'Idiomas', N'Languages', N'Idiomas'),
    (N'Site.litNavInicio', N'Inicio', N'Home', N'Início'),
    (N'Site.litNavInscripcion', N'Inscripción', N'Enrollment', N'Matrícula'),
    (N'Site.litNavIntegridad', N'Integridad', N'Integrity', N'Integridade'),
    (N'Site.litNavMaterias', N'Materias', N'Subjects', N'Matérias'),
    (N'Site.litNavMisCursadas', N'Mis cursadas', N'My courses', N'Minhas cursadas'),
    (N'Site.litNavNotas', N'Notas', N'Grades', N'Notas'),
    (N'Site.litNavPerfil', N'Mi perfil', N'My profile', N'Meu perfil'),
    (N'Site.litNavPerfiles', N'Perfiles', N'Profiles', N'Perfis'),
    (N'Site.litNavPeriodos', N'Períodos', N'Periods', N'Períodos'),
    (N'Site.litNavUsuarios', N'Usuarios', N'Users', N'Usuários'),
    (N'Site.litSecAcademico', N'Académico', N'Academic', N'Acadêmico'),
    (N'Site.litSecGestion', N'Gestión', N'Management', N'Gestão'),
    (N'Site.litSecHerramientas', N'Herramientas', N'Tools', N'Ferramentas'),
    (N'Site.litSecMantenimiento', N'Mantenimiento', N'Maintenance', N'Manutenção'),
    (N'Site.litSecPersonal', N'Personal', N'Personal', N'Pessoal'),
    (N'Usuarios', N'Usuarios', N'Users', N'Usuários'),
    (N'Usuarios.activo', N'Activo', N'Active', N'Ativo'),
    (N'Usuarios.bloqueado', N'Bloqueado', N'Blocked', N'Bloqueado'),
    (N'Usuarios.btnCancelar', N'Cancelar', N'Cancel', N'Cancelar'),
    (N'Usuarios.btnCrear', N'Crear usuario', N'Create user', N'Criar usuário'),
    (N'Usuarios.btnGuardarEdicion', N'Guardar cambios', N'Save changes', N'Salvar alterações'),
    (N'Usuarios.btnResetPass', N'Restablecer contraseña', N'Reset password', N'Redefinir senha'),
    (N'Usuarios.btnResetPass.confirmar', N'¿Restablecer la contraseña de este usuario?', N'Reset this user''s password?', N'Redefinir a senha deste usuário?'),
    (N'Usuarios.deBaja', N'De baja', N'Deactivated', N'Desativado'),
    (N'Usuarios.gvUsuarios.enc.Acciones', N'Acciones', N'Actions', N'Ações'),
    (N'Usuarios.gvUsuarios.enc.Estado', N'Estado', N'Status', N'Situação'),
    (N'Usuarios.gvUsuarios.enc.Perfiles', N'Perfiles', N'Profiles', N'Perfis'),
    (N'Usuarios.gvUsuarios.enc.Usuario', N'Usuario', N'User', N'Usuário'),
    (N'Usuarios.litEditar', N'Editar:', N'Edit:', N'Editar:'),
    (N'Usuarios.litNuevoUsuario', N'Nuevo usuario', N'New user', N'Novo usuário'),
    (N'Usuarios.litPerfiles', N'Perfiles', N'Profiles', N'Perfis'),
    (N'Usuarios.litPerfilesAlMenosUno', N'Perfiles (al menos uno)', N'Profiles (at least one)', N'Perfis (pelo menos um)'),
    (N'Usuarios.litRestablecerContrasena', N'Restablecer contraseña', N'Reset password', N'Redefinir senha'),
    (N'Usuarios.litUsuarios', N'Usuarios', N'Users', N'Usuários'),
    (N'Usuarios.lnkBaja', N'Dar de baja', N'Deactivate', N'Desativar'),
    (N'Usuarios.lnkBaja.confirmar', N'¿Dar de baja a este usuario?', N'Deactivate this user?', N'Desativar este usuário?'),
    (N'Usuarios.lnkEditar', N'Editar', N'Edit', N'Editar'),
    (N'Usuarios.lnkReactivar', N'Reactivar', N'Reactivate', N'Reativar'),
    (N'Usuarios.txtConf.placeholder', N'Repetir contraseña', N'Repeat password', N'Repetir senha'),
    (N'Usuarios.txtDireccion.placeholder', N'Dirección (opcional, hasta 100 caracteres)', N'Address (optional, up to 100 characters)', N'Endereço (opcional, até 100 caracteres)'),
    (N'Usuarios.txtEditDireccion.placeholder', N'Dirección (vacío = sin cambios)', N'Address (empty = no changes)', N'Endereço (vazio = sem alterações)'),
    (N'Usuarios.txtPass.placeholder', N'Contraseña (6+, 1 mayúscula, 1 número)', N'Password (6+, 1 uppercase, 1 number)', N'Senha (6+, 1 maiúscula, 1 número)'),
    (N'Usuarios.txtResetConf.placeholder', N'Repetir nueva contraseña', N'Repeat new password', N'Repetir nova senha'),
    (N'Usuarios.txtResetPass.placeholder', N'Nueva contraseña', N'New password', N'Nova senha'),
    (N'Usuarios.txtUsuario.placeholder', N'Usuario', N'User', N'Usuário'),
    (N'UsuariosBloqueados', N'Usuarios Bloqueados', N'Blocked users', N'Usuários bloqueados'),
    (N'UsuariosBloqueados.litAccion', N'Acción', N'Action', N'Ação'),
    (N'UsuariosBloqueados.litIntentosFallidos', N'Intentos Fallidos', N'Failed attempts', N'Tentativas falhas'),
    (N'UsuariosBloqueados.litNoHayBloqueados', N'No hay usuarios bloqueados actualmente.', N'There are no blocked users right now.', N'Não há usuários bloqueados no momento.'),
    (N'UsuariosBloqueados.litUsuario', N'Usuario', N'User', N'Usuário'),
    (N'UsuariosBloqueados.litUsuariosBloqueados', N'Usuarios Bloqueados', N'Blocked users', N'Usuários bloqueados'),
    (N'UsuariosBloqueados.lnkDesbloquear', N'Desbloquear', N'Unblock', N'Desbloquear'),
    (N'UsuariosBloqueados.lnkDesbloquear.confirmar', N'¿Desbloquear este usuario?', N'Unblock this user?', N'Desbloquear este usuário?'),
    (N'UsuariosBloqueados.msgDesbloqueado', N'Usuario ''{0}'' desbloqueado correctamente.', N'User ''{0}'' was unblocked successfully.', N'Usuário ''{0}'' desbloqueado com sucesso.'),
    (N'Valor.Abril', N'Abril', N'April', N'Abril'),
    (N'Valor.Agosto', N'Agosto', N'August', N'Agosto'),
    (N'Valor.Alta', N'Alta', N'High', N'Alta'),
    (N'Valor.Alto', N'Alto', N'High', N'Alto'),
    (N'Valor.AnoNuevo', N'Año Nuevo', N'New Year''s Day', N'Ano Novo'),
    (N'Valor.Aprobada', N'Aprobada', N'Passed', N'Aprovada'),
    (N'Valor.Baja', N'Baja', N'Low', N'Baixa'),
    (N'Valor.Bajo', N'Bajo', N'Low', N'Baixo'),
    (N'Valor.Cambiarcontrasena', N'Cambiar contrasena', N'Change password', N'Alterar senha'),
    (N'Valor.Cargarnotas', N'Cargar notas', N'Enter grades', N'Lançar notas'),
    (N'Valor.Carnaval', N'Carnaval', N'Carnival', N'Carnaval'),
    (N'Valor.Cursando', N'Cursando', N'In progress', N'Cursando'),
    (N'Valor.Desbloquearusuarios', N'Desbloquear usuarios', N'Unblock users', N'Desbloquear usuários'),
    (N'Valor.DiadelaIndependencia', N'Día de la Independencia', N'Independence Day', N'Dia da Independência'),
    (N'Valor.DiadelaMemoria', N'Día de la Memoria', N'Day of Remembrance', N'Dia da Memória'),
    (N'Valor.DiadelaMemoriaporlaVerdadylaJusticia', N'Día de la Memoria por la Verdad y la Justicia', N'Day of Remembrance for Truth and Justice', N'Dia da Memória pela Verdade e pela Justiça'),
    (N'Valor.DiadelaRevoluciondeMayo', N'Día de la Revolución de Mayo', N'May Revolution Day', N'Dia da Revolução de Maio'),
    (N'Valor.DiadelaSoberaniaNacional', N'Día de la Soberanía Nacional', N'National Sovereignty Day', N'Dia da Soberania Nacional'),
    (N'Valor.DiadelRespetoalaDiversidad', N'Día del Respeto a la Diversidad', N'Day of Respect for Diversity', N'Dia do Respeito à Diversidade'),
    (N'Valor.DiadelRespetoalaDiversidadCultural', N'Día del Respeto a la Diversidad Cultural', N'Day of Respect for Cultural Diversity', N'Dia do Respeito à Diversidade Cultural'),
    (N'Valor.DiadelTrabajador', N'Día del Trabajador', N'Labor Day', N'Dia do Trabalhador'),
    (N'Valor.DiadelVeteranodeMalvinas', N'Día del Veterano de Malvinas', N'Malvinas Veterans Day', N'Dia do Veterano das Malvinas'),
    (N'Valor.DiadelVeteranoydelosCaidosenlaGuerradeMalvinas', N'Día del Veterano y de los Caídos en la Guerra de Malvinas', N'Veterans and Fallen of the Malvinas War Day', N'Dia do Veterano e dos Caídos na Guerra das Malvinas'),
    (N'Valor.Dianolaborableconfinesturisticos', N'Día no laborable con fines turísticos', N'Non-working day for tourism purposes', N'Dia não útil para fins turísticos'),
    (N'Valor.Diciembre', N'Diciembre', N'December', N'Dezembro'),
    (N'Valor.Domingo', N'Domingo', N'Sunday', N'Domingo'),
    (N'Valor.Editarperfil', N'Editar perfil', N'Edit profile', N'Editar perfil'),
    (N'Valor.eliminar', N'eliminar', N'delete', N'excluir'),
    (N'Valor.Enero', N'Enero', N'January', N'Janeiro'),
    (N'Valor.ExportarPDF', N'Exportar PDF', N'Export PDF', N'Exportar PDF'),
    (N'Valor.Febrero', N'Febrero', N'February', N'Fevereiro'),
    (N'Valor.Feriadopuenteturistico', N'Feriado puente turístico', N'Tourist bridge holiday', N'Feriado ponte turística'),
    (N'Valor.Final', N'Final', N'Final', N'Final'),
    (N'Valor.FinalPendiente', N'FinalPendiente', N'Final exam pending', N'Final pendente'),
    (N'Valor.Gestionaridiomas', N'Gestionar idiomas', N'Manage languages', N'Gerenciar idiomas'),
    (N'Valor.Gestionarmaterias', N'Gestionar materias', N'Manage subjects', N'Gerenciar matérias'),
    (N'Valor.Gestionarperfiles', N'Gestionar perfiles', N'Manage profiles', N'Gerenciar perfis'),
    (N'Valor.Gestionarperiodoseinscripciones', N'Gestionar periodos e inscripciones', N'Manage periods and enrollments', N'Gerenciar períodos e matrículas'),
    (N'Valor.Gestionarusuarios', N'Gestionar usuarios', N'Manage users', N'Gerenciar usuários'),
    (N'Valor.Ingresarconintegridadinvalida', N'Ingresar con integridad invalida', N'Log in with invalid integrity', N'Entrar com integridade inválida'),
    (N'Valor.InmaculadaConcepcion', N'Inmaculada Concepción', N'Immaculate Conception', N'Imaculada Conceição'),
    (N'Valor.InmaculadaConcepciondeMaria', N'Inmaculada Concepción de María', N'Immaculate Conception of Mary', N'Imaculada Conceição de Maria'),
    (N'Valor.Inscribirseamaterias', N'Inscribirse a materias', N'Enroll in subjects', N'Matricular-se em matérias'),
    (N'Valor.Jueves', N'Jueves', N'Thursday', N'Quinta-feira'),
    (N'Valor.JuevesSanto', N'Jueves Santo', N'Holy Thursday', N'Quinta-feira Santa'),
    (N'Valor.Julio', N'Julio', N'July', N'Julho'),
    (N'Valor.Junio', N'Junio', N'June', N'Junho'),
    (N'Valor.Lunes', N'Lunes', N'Monday', N'Segunda-feira'),
    (N'Valor.Martes', N'Martes', N'Tuesday', N'Terça-feira'),
    (N'Valor.Marzo', N'Marzo', N'March', N'Março'),
    (N'Valor.Mayo', N'Mayo', N'May', N'Maio'),
    (N'Valor.Media', N'Media', N'Medium', N'Média'),
    (N'Valor.Medio', N'Medio', N'Medium', N'Médio'),
    (N'Valor.Miercoles', N'Miércoles', N'Wednesday', N'Quarta-feira'),
    (N'Valor.modificarsuspermisos', N'modificar sus permisos', N'modify its permissions', N'modificar suas permissões'),
    (N'Valor.mover', N'mover', N'move', N'mover'),
    (N'Valor.Navidad', N'Navidad', N'Christmas', N'Natal'),
    (N'Valor.Noviembre', N'Noviembre', N'November', N'Novembro'),
    (N'Valor.Octubre', N'Octubre', N'October', N'Outubro'),
    (N'Valor.Parcial', N'Parcial', N'Midterm', N'Prova parcial'),
    (N'Valor.Pascuas', N'Pascuas', N'Easter', N'Páscoa'),
    (N'Valor.PasoalaInmortalidaddeBelgrano', N'Paso a la Inmortalidad de Belgrano', N'Anniversary of Belgrano''s death', N'Passagem para a Imortalidade de Belgrano'),
    (N'Valor.PasoalaInmortalidaddelGeneralJosedeSanMartin', N'Paso a la Inmortalidad del General José de San Martín', N'Anniversary of General José de San Martín''s death', N'Passagem para a Imortalidade do General José de San Martín'),
    (N'Valor.PasoalaInmortalidaddelGeneralManuelBelgrano', N'Paso a la Inmortalidad del General Manuel Belgrano', N'Anniversary of General Manuel Belgrano''s death', N'Passagem para a Imortalidade do General Manuel Belgrano'),
    (N'Valor.PasoalaInmortalidaddelGeneralMartinMigueldeGuemes', N'Paso a la Inmortalidad del General Martín Miguel de Güemes', N'Anniversary of General Martín Miguel de Güemes''s death', N'Passagem para a Imortalidade do General Martín Miguel de Güemes'),
    (N'Valor.PasoalaInmortalidaddeSanMartin', N'Paso a la Inmortalidad de San Martín', N'Anniversary of San Martín''s death', N'Passagem para a Imortalidade de San Martín'),
    (N'Valor.Presencial', N'Presencial', N'In person', N'Presencial'),
    (N'Valor.renombrar', N'renombrar', N'rename', N'renomear'),
    (N'Valor.RevoluciondeMayo', N'Revolución de Mayo', N'May Revolution', N'Revolução de Maio'),
    (N'Valor.Sabado', N'Sábado', N'Saturday', N'Sábado'),
    (N'Valor.Septiembre', N'Septiembre', N'September', N'Setembro'),
    (N'Valor.TP', N'TP', N'Assignment', N'Trabalho'),
    (N'Valor.Verbitacora', N'Ver bitacora', N'View audit log', N'Ver log de auditoria'),
    (N'Valor.Vercalendario', N'Ver calendario', N'View calendar', N'Ver calendário'),
    (N'Valor.Verdashboard', N'Ver dashboard', N'View dashboard', N'Ver painel'),
    (N'Valor.Verificarintegridad', N'Verificar integridad', N'Verify integrity', N'Verificar integridade'),
    (N'Valor.Vermenudealumno', N'Ver menu de alumno', N'View student menu', N'Ver menu do aluno'),
    (N'Valor.Vermiscursadas', N'Ver mis cursadas', N'View my courses', N'Ver minhas cursadas'),
    (N'Valor.Viernes', N'Viernes', N'Friday', N'Sexta-feira'),
    (N'Valor.ViernesSanto', N'Viernes Santo', N'Good Friday', N'Sexta-feira Santa'),
    (N'Valor.Virtual', N'Virtual', N'Virtual', N'Virtual');

DECLARE @esId  INT = (SELECT TOP 1 ID FROM IDIOMA WHERE PREDETERMINADO = 1 ORDER BY ID);
DECLARE @enId  INT = (SELECT TOP 1 ID FROM IDIOMA WHERE NOMBRE LIKE 'Ingl%' OR NOMBRE = 'English' ORDER BY ID);
DECLARE @ptId  INT = (SELECT TOP 1 ID FROM IDIOMA WHERE NOMBRE LIKE 'Portugu%' ORDER BY ID);

DECLARE @porIdioma TABLE (IDIOMA_ID INT, CLAVE VARCHAR(100), TEXTO NVARCHAR(500));
INSERT INTO @porIdioma SELECT @esId, CLAVE, ES FROM @textos WHERE @esId IS NOT NULL;
INSERT INTO @porIdioma SELECT @enId, CLAVE, EN FROM @textos WHERE @enId IS NOT NULL;
INSERT INTO @porIdioma SELECT @ptId, CLAVE, PT FROM @textos WHERE @ptId IS NOT NULL;

BEGIN TRANSACTION;

UPDATE c SET TEXTO_DEFAULT = t.ES
FROM CONTROL_IDIOMA c JOIN @textos t ON t.CLAVE = c.CLAVE;

INSERT INTO CONTROL_IDIOMA (CLAVE, TEXTO_DEFAULT)
SELECT t.CLAVE, t.ES FROM @textos t
WHERE NOT EXISTS (SELECT 1 FROM CONTROL_IDIOMA c WHERE c.CLAVE = t.CLAVE);

UPDATE tr SET TEXTO = pi.TEXTO
FROM TRADUCCION tr
JOIN CONTROL_IDIOMA c ON c.ID = tr.CONTROL_ID
JOIN @porIdioma pi ON pi.CLAVE = c.CLAVE AND pi.IDIOMA_ID = tr.IDIOMA_ID;

INSERT INTO TRADUCCION (IDIOMA_ID, CONTROL_ID, TEXTO)
SELECT pi.IDIOMA_ID, c.ID, pi.TEXTO
FROM @porIdioma pi
JOIN CONTROL_IDIOMA c ON c.CLAVE = pi.CLAVE
WHERE NOT EXISTS (SELECT 1 FROM TRADUCCION tr WHERE tr.IDIOMA_ID = pi.IDIOMA_ID AND tr.CONTROL_ID = c.ID);

COMMIT TRANSACTION;

SELECT i.NOMBRE AS IDIOMA, COUNT(tr.CONTROL_ID) AS TRADUCCIONES
FROM IDIOMA i LEFT JOIN TRADUCCION tr ON tr.IDIOMA_ID = i.ID
GROUP BY i.NOMBRE;
GO