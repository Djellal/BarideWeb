# AGENTS.md

BaridUFAS ("سجل المراسلات الادارية") — Arabic/RTL administrative correspondence manager.
Single ASP.NET Core **.NET 10 Razor Pages** app backed by **PostgreSQL** (EF Core + ASP.NET Identity). No test project, no linter/formatter, no CI for this app. `dotnet build` is the only available verification; requires the .NET 10 SDK (may not be preinstalled — check `dotnet --version`).

## Commands

- Build: `dotnet build BarideWeb.csproj`
- Run (dev): `dotnet run --project BarideWeb.csproj`
- Publish: `dotnet publish` (Release output at `bin/Release/net10.0/publish/`)
- Deploy: `./publish.sh` — **server-only**. Hardcodes `/opt/baridufas/`, pulls `main`, restarts the `baridufas` systemd service. Do not run locally.
- There is no way to override the listen URL from the CLI: `Program.cs` ends with `app.Run("http://localhost:5226")` (hardcoded). `--urls`/`ASPNETCORE_URLS` are ignored; edit that line to change it.

## Database — read this before touching models

- `Program.cs` calls `db.Database.EnsureCreated()` on startup, **not** `Database.Migrate()`. The `Migrations/` folder is effectively dead: migrations are never applied at runtime.
- Consequence: `EnsureCreated()` only builds the schema when the database does not exist. Adding an entity/property will **not** alter an existing DB — you must drop/recreate it or apply SQL manually. There is no `dotnet ef` tools manifest; `Microsoft.EntityFrameworkCore.Design` is referenced but the CLI is not configured.
- Connection string is in the committed `appsettings.json` (DB `baridufas`, local Postgres). Real SMTP credentials are also committed there. Treat `appsettings.json` as containing production secrets; keep changes out of unrelated commits.
- Startup also seeds roles and a default user: **`admin` / `admin`**.

## Auth & multi-tenancy (the main source of subtle bugs)

- `AddRazorPages` applies `AuthorizeFolder("/")`, so **every page requires login by default**. Only `/Account/Login`, `/Account/AccessDenied`, and `/Correspondances/SharedView` are anonymous. A new public page must be added to that allowlist in `Program.cs`.
- Roles: `Admin` (global), `TenantAdmin`, `User`. Global admin has `TenantId == null`.
- `BarideDbContext` applies **global query filters** to `Categorie`, `Corresp`, `AppParameter`, `Transfert` based on the `TenantId` claim (added by `TenantClaimsPrincipalFactory`). A null tenant (global admin) bypasses filtering and sees all rows.
- When a page must read/write across tenants (admin/tenant management), call `.IgnoreQueryFilters()` explicitly — see `Pages/Tenants/Index.cshtml.cs` and `Pages/Parameters/Index.cshtml.cs`.
- `TenantId` and audit fields (`CreatedAt`/`CreatedBy`/`UpdatedAt`) are set automatically in the `BarideDbContext.SaveChanges*` overrides. Do not set them by hand.

## Layout & conventions

- `Program.cs` — all DI, Identity config, middleware, startup seeding, **and minimal API endpoints** (`/api/users/search`, `/api/contacts/search`, AG Grid server-side source `/api/correspondances`). These are inline, not controllers. Add APIs here.
- `Data/BarideDbContext.cs` — DbSets, seed data, tenant query filters, audit stamping.
- `Models/` — `TypeCorresp` enum: `Entrant_Interne`=0, `Entrant_Externe`=1, **`Sotant_Interne`=2 (misspelled, keep it)**, `Sortant_Externe`=3, `Divers`=4.
- `Services/` — `TenantService` (reads tenant claim), `EmailService` (MailKit/SMTP), `EmailTemplateService`, `FileValidation`.
- `Pages/` — Razor Pages. `Pages/Shared/_Layout.cshtml` is the authenticated shell; `_PublicLayout.cshtml` for `SharedView`. `Pages/Shared/_CorrespView.cshtml` + `_ScannerPanel.cshtml` are shared partials.
- `wwwroot/Dynamic-Web-TWAIN-19.3.2/` is a vendored third-party scanner SDK (has its own `.github/`); don't reformat or "clean up" it.
- UI is RTL Arabic. Arabic labels for `TypeCorresp` are **hardcoded in switch expressions** in `Pages/Index.cshtml.cs`, `Correspondances/Create.cshtml.cs`, `Correspondances/View.cshtml.cs`, `Correspondances/Print.cshtml.cs`, and `Services/EmailTemplateService.cs` — updating a type means updating all of them.
- Per-tenant settings live in the `AppParameter` table (keys `CorrespViewMode`, `ScannerDpi`, `ScannerPixelMode`, `ScannerImageFormat`, `EmailTemplate`), not `appsettings.json`. `ViewMode`: 0=NewTab, 1=Modal, 2=SameTab.
- Uploads are written to `wwwroot/uploads/` (gitignored) as GUID filenames; validate with `Services/FileValidation.cs` (max 20 MB; PDF/images only).
- Frontend libs are loaded from CDN in `_Layout.cshtml` (Bootstrap 5 RTL, AG Grid 32, Tom Select, AOS, SweetAlert2, Notyf, countUp). The layout defines globals `notyf` and `swalConfirm(message, formOrCallback)` — reuse these instead of re-importing.
