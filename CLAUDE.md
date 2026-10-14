# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

This is an ASP.NET Web Forms solution targeting .NET 4.7.2. Open `WebApplication12.sln` in Visual Studio and run with IIS Express (F5). There are no CLI build or test commands — all compilation happens inside Visual Studio.

The app starts at `Login.aspx` (configured as default document in `Web.config`).

## Database Setup

Run `DAL/BD_Universidad.sql` against SQL Server to create the database `BDUNIVERSIDAD`, all tables, all stored procedures, the seeded users (admin, webmaster), the base profiles/permissions, and the full plan de estudios (51 materias, 43 correlativas). The script is idempotent and also migrates an existing database (see "Role-Based Access").

The connection string is hardcoded in `DAL/Acceso.cs`:
```
initial catalog=BDUNIVERSIDAD; Data Source=.; Integrated Security=SSPI
```
Change `Data Source=.` if the SQL Server instance is named (e.g. `.\SQLEXPRESS`).

Default credentials: `admin` / `Admin123`, `webmaster` / `Webmaster123`

## Architecture

Four-project layered solution: **BE → DAL → BLL → WebApplication12**.

- **BE** — Plain entity classes (`USUARIO`, `MATERIA`, `ALUMNO_MATERIA`, `PERIODO_ACADEMICO`, `INSCRIPCION`, etc.) plus `AESHelper` (AES encryption for direccion), `HashHelper` (SHA-256), `SessionManager`, and the permissions Composite (`NodoPermiso`, `Rol`, `Permiso`, `Permisos` constants).
- **DAL** — `Acceso.cs` is the central DB gateway (all queries go through `Abrir()` → stored proc → `Cerrar()` in finally). Every DAL class uses `Acceso` exclusively; no inline SQL anywhere. Methods: `Leer()` → DataTable, `Escribir()` → rows affected, `EscribirConRetorno()` → SCOPE_IDENTITY.
- **BLL** — Business logic + audit trail. Every write operation calls `BitacoraBLL.RegistrarAccion()`. Contains `AlumnoMateriaBLL` (includes DVH/DVV digit-verifier logic), `InscripcionBLL`, `PeriodoBLL`, `MateriaBLL`, `UsuarioBLL`, `LoginBLL`, `EventoBLL`.
- **WebApplication12** — ASP.NET Web Forms pages. All pages inherit `Site.Master`. Session authentication: every page checks `Session["Usuario"]` (cast to `BE.USUARIO`) on `Page_Load`.

## Role-Based Access (dynamic profiles, Composite)

Access is controlled by **permissions**, not by a fixed role string (`USUARIO.ROL` no longer exists).

- **Schema:** `ROL` (profile, `PADRE_ID` self-FK, `PROTEGIDO`), `PERMISO`, `ROL_PERMISO`, `USUARIO_PERFIL` (many-to-many user↔profile). Seeded profiles: `Administrador` and `Webmaster` (protected), `Alumno`. The SQL script migrates old `USUARIO.ROL` values into `USUARIO_PERFIL` and drops the column.
- **Composite (real inheritance):** `BE.Rol` contains child `Rol`s and `Permiso` leaves. `TienePermiso` / `ObtenerPermisos` resolve recursively, so a parent profile also grants every permission of its descendants. `BLL.PerfilBLL` loads the whole tree once (in memory) and computes the user's effective permissions; `LoginBLL` stores them in `BE.USUARIO.Permisos` (kept in `Session["Usuario"]`). Never resolve effective permissions with a flat SQL query. `PerfilBLL.CambiarPadre` rejects cycles.
- **Permission names** are centralized in `BE.Permisos` (ASCII, no accents — they are matched as text against `PERMISO.NOMBRE`).
- **Web gating:** pages inherit `PaginaBase` and declare `PermisoRequerido` (or override `TieneAcceso`). Missing session → `Login.aspx`; missing permission → the user's home page (`PaginaBase.ObtenerPaginaInicio`, picked by permission priority).
- **Navigation:** `Site.Master.cs` shows each sidebar link only if the user has its permission; one `asp:PlaceHolder` per link and per section.
- **Not a permission:** "Alumno" as a domain identity (has cursadas) is the `Alumno` profile name, used by `USUARIO_LISTAR_ALUMNOS` (grading list). Login blocks users when integrity fails unless they have `Ingresar con integridad invalida`.

**Important:** The sidebar uses `asp:PlaceHolder` (not Panel) to wrap `<li>` elements — Panel renders a `<div>` which is invalid inside `<ul>`.

## Designer Files

New ASPX controls must be manually declared in the corresponding `.aspx.designer.cs` file — Visual Studio auto-generation is not relied upon. Always keep designer declarations in sync when adding or removing controls from ASPX markup.

## Key Patterns

**DAL pattern:**
```csharp
try { _acceso.Abrir(); /* query */ }
finally { _acceso.Cerrar(); }
```

**Stored procedures only** — all DB access via named stored procs. Proc names follow the pattern `ENTIDAD_ACCION` (e.g. `MATERIA_LISTAR`, `ALUMNO_MATERIA_ACTUALIZAR`).

**Inscripción flow:** `InscripcionBLL.Inscribir()` checks correlativas before inserting. If a cabecera already exists for the period it reuses it (via `INSCRIPCION_OBTENER_ID`) instead of blocking re-enrollment — students can add more subjects to an existing enrollment.

**Digit verifier:** all 13 business tables (the original 9 plus `ROL`, `PERMISO`, `ROL_PERMISO`, `USUARIO_PERFIL`) have a `DVH` column computed per row by `BE.CalculadorDVH`. Column lists (`Columnas*`) live in `BLL/IntegridadBLL.cs`; the vertical digits (DVV) are stored per table+column in `DIGITO_VERIFICADOR_VERTICAL`. `IntegridadBLL` verifies/recalculates every table, exports/restores a JSON backup, and `Integridad.aspx` (permission `Verificar integridad`) drives it. `ROL_PERMISO` and `USUARIO_PERFIL` carry an `ID IDENTITY` column (like `CORRELATIVA`) because the engine works with one integer ID per row. After migrating an existing DB, `Global.asax.cs` computes the integrity of the tables that never had it (`InicializarTablasDePerfiles`).

**Card-based materia selection:** Checkboxes in `Inscripcion.aspx` use `name="materiaId"` inside `<label>` wrappers; server reads them via `Request.Form.GetValues("materiaId")` (not from a CheckBoxList).
