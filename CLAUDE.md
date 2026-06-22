# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

This is an ASP.NET Web Forms solution targeting .NET 4.7.2. Open `WebApplication12.sln` in Visual Studio and run with IIS Express (F5). There are no CLI build or test commands — all compilation happens inside Visual Studio.

The app starts at `Login.aspx` (configured as default document in `Web.config`).

## Database Setup

Run `WebApplication12/BD_Universidad.sql` once against SQL Server to create the database `BDUNIVERSIDAD`, all tables, all stored procedures, the admin user, and the full plan de estudios (51 materias, 43 correlativas).

The connection string is hardcoded in `DAL/Acceso.cs`:
```
initial catalog=BDUNIVERSIDAD; Data Source=.; Integrated Security=SSPI
```
Change `Data Source=.` if the SQL Server instance is named (e.g. `.\SQLEXPRESS`). **The comments in `DAL/Acceso.cs` must never be removed.**

Default admin credentials: `admin` / `Admin123`

## Architecture

Four-project layered solution: **BE → DAL → BLL → WebApplication12**.

- **BE** — Plain entity classes (`USUARIO`, `MATERIA`, `ALUMNO_MATERIA`, `PERIODO_ACADEMICO`, `INSCRIPCION`, etc.) plus `AESHelper` (AES encryption for direccion), `HashHelper` (SHA-256), and `SessionManager`.
- **DAL** — `Acceso.cs` is the central DB gateway (all queries go through `Abrir()` → stored proc → `Cerrar()` in finally). Every DAL class uses `Acceso` exclusively; no inline SQL anywhere. Methods: `Leer()` → DataTable, `Escribir()` → rows affected, `EscribirConRetorno()` → SCOPE_IDENTITY.
- **BLL** — Business logic + audit trail. Every write operation calls `BitacoraBLL.RegistrarAccion()`. Contains `AlumnoMateriaBLL` (includes DVH/DVV digit-verifier logic), `InscripcionBLL`, `PeriodoBLL`, `MateriaBLL`, `UsuarioBLL`, `LoginBLL`, `EventoBLL`.
- **WebApplication12** — ASP.NET Web Forms pages. All pages inherit `Site.Master`. Session authentication: every page checks `Session["Usuario"]` (cast to `BE.USUARIO`) on `Page_Load`.

## Role-Based Access

`BE.USUARIO.EsAdmin` (`Rol == "Admin"`) controls access everywhere:
- Admin pages (`Materias`, `Bitacora`, `Inscripcion` as period manager, `Integridad`) redirect alumno → `Menu.aspx`
- Alumno pages (`Menu`, `MisCursadas`, `Dashboard`, `Calendario`, `Perfil`) redirect admin → `Materias.aspx`
- `Site.Master.cs` toggles `phNavAdmin` / `phNavAlumno` PlaceHolders and sets `lnkInicio` based on role

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

**Digit verifier:** `DVH` is stored per row in `ALUMNO_MATERIA`; `DVV` is the sum of all DVH mod 10, stored in `DVV_NOTAS`. Recalculated on every note update and inscription. Admin can verify integrity for all students via `Integridad.aspx`.

**Card-based materia selection:** Checkboxes in `Inscripcion.aspx` use `name="materiaId"` inside `<label>` wrappers; server reads them via `Request.Form.GetValues("materiaId")` (not from a CheckBoxList).
