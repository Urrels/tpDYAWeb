-- Traducciones iniciales (español / inglés / portugués) de los textos del sitio.
-- Ejecutar DESPUÉS de BD_Universidad.sql:
--   sqlcmd -S . -E -f 65001 -i DAL\traducciones.sql
-- Es idempotente y no destructivo: agrega las claves que falten, actualiza el texto por defecto
-- (español) y las traducciones de estas claves, y no toca las claves que no figuran acá ni las
-- traducciones que se editaron en Idiomas.aspx para otras claves. Los textos nuevos del código
-- se registran solos en CONTROL_IDIOMA la primera vez que se usan.
USE BDUNIVERSIDAD
GO
SET NOCOUNT ON;

DECLARE @textos TABLE (CLAVE VARCHAR(100) PRIMARY KEY, ES NVARCHAR(200), EN NVARCHAR(200), PT NVARCHAR(200));
INSERT INTO @textos (CLAVE, ES, EN, PT) VALUES
    -- Página maestra
    (N'Site.litMarca', N'Sistema de Gestión Académico', N'Academic Management System', N'Sistema de Gestão Acadêmica'),
    (N'Site.btnCerrarSesion', N'Cerrar sesión', N'Log out', N'Sair'),
    (N'Site.litNavInicio', N'Inicio', N'Home', N'Início'),
    (N'Site.litSecGestion', N'Gestión', N'Management', N'Gestão'),
    (N'Site.litNavMaterias', N'Materias', N'Subjects', N'Matérias'),
    (N'Site.litNavNotas', N'Notas', N'Grades', N'Notas'),
    (N'Site.litNavPeriodos', N'Períodos', N'Periods', N'Períodos'),
    (N'Site.litNavBitacora', N'Bitácora', N'Audit log', N'Log de auditoria'),
    (N'Site.litNavBloqueados', N'Usuarios bloqueados', N'Blocked users', N'Usuários bloqueados'),
    (N'Site.litNavUsuarios', N'Usuarios', N'Users', N'Usuários'),
    (N'Site.litNavPerfiles', N'Perfiles', N'Profiles', N'Perfis'),
    (N'Site.litNavIdiomas', N'Idiomas', N'Languages', N'Idiomas'),
    (N'Site.litNavAsignarPerfiles', N'Asignar perfiles', N'Assign profiles', N'Atribuir perfis'),
    (N'Site.litSecAcademico', N'Académico', N'Academic', N'Acadêmico'),
    (N'Site.litNavInscripcion', N'Inscripción', N'Enrollment', N'Matrícula'),
    (N'Site.litNavMisCursadas', N'Mis cursadas', N'My courses', N'Minhas cursadas'),
    (N'Site.litNavCalendario', N'Calendario', N'Calendar', N'Calendário'),
    (N'Site.litSecHerramientas', N'Herramientas', N'Tools', N'Ferramentas'),
    (N'Site.litNavDashboard', N'Dashboard', N'Dashboard', N'Painel'),
    (N'Site.litSecMantenimiento', N'Mantenimiento', N'Maintenance', N'Manutenção'),
    (N'Site.litNavIntegridad', N'Integridad', N'Integrity', N'Integridade'),
    (N'Site.litSecPersonal', N'Personal', N'Personal', N'Pessoal'),
    (N'Site.litNavPerfil', N'Mi perfil', N'My profile', N'Meu perfil'),
    (N'Site.litNavContrasena', N'Contraseña', N'Password', N'Senha'),
    (N'SelectorIdioma.litIdioma', N'Idioma:', N'Language:', N'Idioma:'),
    -- Paginador
    (N'Paginador.btnPrimera', N'Primera', N'First', N'Primeira'),
    (N'Paginador.btnAnterior', N'Anterior', N'Previous', N'Anterior'),
    (N'Paginador.btnSiguiente', N'Siguiente', N'Next', N'Próxima'),
    (N'Paginador.btnUltima', N'Última', N'Last', N'Última'),
    -- Login
    (N'Login.litTitulo', N'Sistema de Gestión Académica', N'Academic Management System', N'Sistema de Gestão Acadêmica'),
    (N'Login.litSubtitulo', N'Iniciá sesión para continuar', N'Log in to continue', N'Entre para continuar'),
    (N'Login.litUsuario', N'Usuario', N'User', N'Usuário'),
    (N'Login.litContrasena', N'Contraseña', N'Password', N'Senha'),
    (N'Login.btnIngresar', N'Ingresar', N'Log in', N'Entrar'),
    (N'Login.litAviso', N'Aviso', N'Notice', N'Aviso'),
    (N'Login.litNoDisponible', N'El sistema no se encuentra disponible en este momento, intente más tarde.', N'The system is not available right now, please try again later.', N'O sistema não está disponível no momento, tente mais tarde.'),
    (N'Login.litCerrar', N'Cerrar', N'Close', N'Fechar'),
    (N'Login.msgCompletar', N'Completá usuario y contraseña.', N'Enter your user and password.', N'Preencha usuário e senha.'),
    (N'Login.msgCredenciales', N'Usuario o contraseña incorrectos.', N'Incorrect user or password.', N'Usuário ou senha incorretos.'),
    (N'Login.msgBloqueado', N'Tu cuenta fue bloqueada por demasiados intentos fallidos. Contactá a un administrador para desbloquearla.', N'Your account was blocked after too many failed attempts. Contact an administrator to unblock it.', N'Sua conta foi bloqueada por muitas tentativas falhas. Contate um administrador para desbloqueá-la.'),
    (N'Login.msgInactivo', N'Tu cuenta fue dada de baja. Contactá a un administrador.', N'Your account was deactivated. Contact an administrator.', N'Sua conta foi desativada. Contate um administrador.'),
    (N'Login.msgSesionActiva', N'Ya hay una sesión activa con este usuario. Cerrá la sesión anterior antes de volver a ingresar.', N'This user already has an active session. Close the previous session before logging in again.', N'Já existe uma sessão ativa com este usuário. Encerre a sessão anterior antes de entrar novamente.'),
    (N'Login.msgSinPermisos', N'Tu cuenta no tiene permisos asignados. Contactá a un administrador.', N'Your account has no permissions assigned. Contact an administrator.', N'Sua conta não tem permissões atribuídas. Contate um administrador.'),
    -- Materias
    (N'Materias', N'Materias', N'Subjects', N'Matérias'),
    (N'Materias.litTitulo', N'Gestión de materias', N'Subject management', N'Gestão de matérias'),
    (N'Materias.btnNueva', N'+ Nueva Materia', N'+ New subject', N'+ Nova matéria'),
    (N'Materias.litXml', N'Plan de estudios en XML', N'Study plan in XML', N'Plano de estudos em XML'),
    (N'Materias.btnExportarXml', N'Exportar XML', N'Export XML', N'Exportar XML'),
    (N'Materias.btnImportarXml', N'Importar XML', N'Import XML', N'Importar XML'),
    (N'Materias.litWs', N'Plan de estudios vía Web Service', N'Study plan via Web Service', N'Plano de estudos via Web Service'),
    (N'Materias.litNombre', N'Nombre', N'Name', N'Nome'),
    (N'Materias.litCodigo', N'Código', N'Code', N'Código'),
    (N'Materias.litModalidad', N'Modalidad', N'Mode', N'Modalidade'),
    (N'Materias.litPeso', N'Peso (complejidad 1-5)', N'Weight (difficulty 1-5)', N'Peso (complexidade 1-5)'),
    (N'Materias.btnGuardar', N'Guardar', N'Save', N'Salvar'),
    (N'Materias.btnCancelar', N'Cancelar', N'Cancel', N'Cancelar'),
    (N'Materias.colCodigo', N'Código', N'Code', N'Código'),
    (N'Materias.colNombre', N'Nombre', N'Name', N'Nome'),
    (N'Materias.colModalidad', N'Modalidad', N'Mode', N'Modalidade'),
    (N'Materias.colPeso', N'Peso', N'Weight', N'Peso'),
    (N'Materias.colAcciones', N'Acciones', N'Actions', N'Ações'),
    (N'Materias.msgGuardado', N'✔ Guardado.', N'✔ Saved.', N'✔ Salvo.'),
    (N'Materias.msgErrorGuardar', N'✖ Error al guardar.', N'✖ Error while saving.', N'✖ Erro ao salvar.'),
    (N'Materias.msgBaja', N'✔ Materia dada de baja.', N'✔ Subject deactivated.', N'✔ Matéria desativada.'),
    (N'Materias.msgError', N'✖ Error.', N'✖ Error.', N'✖ Erro.'),
    (N'Materias.msgElegirXml', N'Elegí un archivo .xml para importar.', N'Choose an .xml file to import.', N'Escolha um arquivo .xml para importar.'),
    (N'Materias.msgImportacionOk', N'✔ Importación completa: {0} materias nuevas, {1} actualizadas, {2} correlativas agregadas.', N'✔ Import complete: {0} new subjects, {1} updated, {2} prerequisites added.', N'✔ Importação concluída: {0} matérias novas, {1} atualizadas, {2} correlativas adicionadas.'),
    (N'Materias.msgImportacionError', N'✖ No se importó nada:', N'✖ Nothing was imported:', N'✖ Nada foi importado:'),
    (N'Materias.msgMasErrores', N'… y {0} errores más.', N'… and {0} more errors.', N'… e mais {0} erros.'),
    -- Dashboard
    (N'Dashboard', N'Dashboard', N'Dashboard', N'Painel'),
    (N'Dashboard.litTitulo', N'Dashboard académico', N'Academic dashboard', N'Painel acadêmico'),
    (N'Dashboard.litTotal', N'Total de materias', N'Total subjects', N'Total de matérias'),
    (N'Dashboard.litAprobadas', N'Aprobadas', N'Passed', N'Aprovadas'),
    (N'Dashboard.litCursando', N'Cursando', N'In progress', N'Cursando'),
    (N'Dashboard.litFinalPendiente', N'Final pendiente', N'Final exam pending', N'Final pendente'),
    (N'Dashboard.litNotas', N'Notas por materia', N'Grades by subject', N'Notas por matéria'),
    (N'Dashboard.litEstados', N'Estado de cursadas', N'Course status', N'Situação das cursadas'),
    (N'Dashboard.litWs', N'Resumen vía Web Service', N'Summary via Web Service', N'Resumo via Web Service'),
    (N'Dashboard.litPdf', N'Exportar PDF', N'Export PDF', N'Exportar PDF'),
    -- Gestión de idiomas
    (N'Idiomas', N'Idiomas', N'Languages', N'Idiomas'),
    (N'Idiomas.litTitulo', N'Gestión de idiomas', N'Language management', N'Gestão de idiomas'),
    (N'Idiomas.litIdiomas', N'Idiomas', N'Languages', N'Idiomas'),
    (N'Idiomas.btnAgregar', N'Agregar idioma', N'Add language', N'Adicionar idioma'),
    (N'Idiomas.btnRenombrar', N'Renombrar el seleccionado', N'Rename selected', N'Renomear o selecionado'),
    (N'Idiomas.litTraducciones', N'Traducciones del idioma seleccionado', N'Translations of the selected language', N'Traduções do idioma selecionado'),
    (N'Idiomas.btnFiltrar', N'Filtrar por clave', N'Filter by key', N'Filtrar por chave'),
    (N'Idiomas.btnGuardar', N'Guardar traducciones', N'Save translations', N'Salvar traduções'),
    (N'Idiomas.colIdioma', N'Idioma', N'Language', N'Idioma'),
    (N'Idiomas.colHabilitado', N'Habilitado', N'Enabled', N'Habilitado'),
    (N'Idiomas.colPredeterminado', N'Predeterminado', N'Default', N'Padrão'),
    (N'Idiomas.colAcciones', N'Acciones', N'Actions', N'Ações'),
    (N'Idiomas.colClave', N'Clave', N'Key', N'Chave'),
    (N'Idiomas.colTextoDefault', N'Texto por defecto', N'Default text', N'Texto padrão'),
    (N'Idiomas.colTraduccion', N'Traducción', N'Translation', N'Tradução'),
    (N'Idiomas.si', N'Sí', N'Yes', N'Sim'),
    (N'Idiomas.no', N'No', N'No', N'Não'),
    (N'Idiomas.btnTraducciones', N'Ver traducciones', N'View translations', N'Ver traduções'),
    (N'Idiomas.btnAlternar', N'Habilitar/Deshabilitar', N'Enable/Disable', N'Ativar/Desativar'),
    (N'Idiomas.btnEliminar', N'Eliminar', N'Delete', N'Excluir'),
    (N'Idiomas.confirmarEliminar', N'¿Eliminar el idioma? Se borrarán todas sus traducciones.', N'Delete the language? All its translations will be erased.', N'Excluir o idioma? Todas as suas traduções serão apagadas.'),
    (N'Idiomas.msgCreado', N'Idioma agregado. Está deshabilitado hasta que lo habilites.', N'Language added. It stays disabled until you enable it.', N'Idioma adicionado. Fica desativado até você ativá-lo.'),
    (N'Idiomas.msgEliminado', N'Idioma eliminado.', N'Language deleted.', N'Idioma excluído.'),
    (N'Idiomas.msgEstado', N'Estado actualizado.', N'Status updated.', N'Estado atualizado.'),
    (N'Idiomas.msgGuardadas', N'Traducciones guardadas: {0}.', N'Translations saved: {0}.', N'Traduções salvas: {0}.'),
    (N'Idiomas.msgRenombrado', N'Idioma renombrado.', N'Language renamed.', N'Idioma renomeado.'),
    (N'Idiomas.msgSeleccionar', N'Seleccioná un idioma.', N'Select a language.', N'Selecione um idioma.');

DECLARE @esId  INT = (SELECT TOP 1 ID FROM IDIOMA WHERE PREDETERMINADO = 1 ORDER BY ID);
DECLARE @enId  INT = (SELECT TOP 1 ID FROM IDIOMA WHERE NOMBRE LIKE 'Ingl%' OR NOMBRE = 'English' ORDER BY ID);
DECLARE @ptId  INT = (SELECT TOP 1 ID FROM IDIOMA WHERE NOMBRE LIKE 'Portugu%' ORDER BY ID);

DECLARE @porIdioma TABLE (IDIOMA_ID INT, CLAVE VARCHAR(100), TEXTO NVARCHAR(200));
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
