# Agent Instructions

**Flowtr** is a simple bookkeeping web app for income and expenses of a side business (Swiss sole proprietorship). Receipts are uploaded or forwarded by email, AI extracts the booking data, the user confirms, and Flowtr files the numbered receipt and writes the journal entry. Once a year, journal and receipts are exported for the tax return or the fiduciary.

.NET (latest), Blazor Interactive WebAssembly, ASP.NET Core WebAPI, MudBlazor, EF Core on SQL Server (Azure SQL in production), Aspire for local dev.

These rules are the short, always-on version. Details live in the specs — read the relevant one before changing that area:

| Area | Spec |
|---|---|
| Receipt capture, AI extraction, input channels | `specs/concept.md` |
| Journal, closing, export, fiduciary dossier | `specs/journal.md` |
| Security requirements | `specs/security.md` |
| Backup & restore | `specs/backup.md` |
| Stack, principles, components, roles | `specs/architecture.md` |
| Coding rules | `specs/coding-guidelines.md` |
| UX, colours | `specs/ux.md` |
| Domain terms | `specs/glossary.md` |
| Decisions | `specs/adrs/` |
| Stories | `specs/stories/` (template `_template.md`) |
| Backlog — do not pick up unless explicitly asked | `specs/backlog.md` |

## Repository Layout

| Folder | Content |
|---|---|
| `src/` | Source code including tests — solution `src/Flowtr.slnx`, package versions only in `src/Directory.Packages.props` |
| `src/Flowtr.AppHost/` | Aspire AppHost: SQL Server and Azurite containers, server |
| `src/Flowtr.ServiceDefaults/` | Aspire service defaults: telemetry, health checks, resilience |
| `src/Flowtr.Server/` | ASP.NET Core host: API, Identity, EF Core, feature slices in `Features/`, CQS base in `CrossCutting/Cqs/` |
| `src/Flowtr.Client/` | Blazor WebAssembly UI with MudBlazor |
| `src/Flowtr.Shared/` | Types shared by server and client (`Result`, DTOs) |
| `src/Flowtr.Tests/` | Tests (xUnit + Shouldly) |
| `deploy/` | Infrastructure as Code (Bicep) and deployment scripts |
| `docs/` | Operating and user documentation (how to run and use it) |
| `specs/` | Specifications (what is built and why) |
| `assets/` | Logo and images |

## Running

`dotnet run --project src/Flowtr.AppHost` (Docker required) → starts SQL Server and Azurite containers (persistent volumes) and the server; the Aspire dashboard URL is printed on start. The server alone has no database connection — always start via the AppHost. In Development, EF migrations are applied on startup.

Build and test: `dotnet build src/Flowtr.slnx`, `dotnet test src/Flowtr.slnx`.

## Key Principles

- **Vertical slices + CQS**: one use case = one file in `Commands/` or `Queries/` (record + handler, method `Handle`, returns `Result` or `Result<T>`).
- **Single tenant** per installation — no multi-tenancy code.
- **RBAC**: users have one or more roles (Admin, Contributor).
- **Never lose data**: journal and receipts are critical; a closed calendar year is read-only, including its receipts.
- **Audit everything**: every create/update/delete of bookings and receipts is written to the audit log.
- **Security is part of the foundation**, not an add-on — this is accounting data.

## Coding

- No `_` prefix on fields.
- No `Async` suffix on method names.

## UX

- **Account pages (`/Account/*`) are static SSR** (Identity needs `HttpContext`). On them — and in anything they render, like `MainLayout` — use only MudBlazor components that work without interactivity: `MudStatic*` inputs (MudBlazor.StaticInput), `MudPaper`, `MudText`, `MudAlert`, `MudLink`, `MudButton` with `Href` or `ButtonType.Submit`. No `MudMenu`, `MudDialog`, `MudSelect` etc. See ADR-003.

- Responsive (iPhone to desktop), should feel like a desktop app; MudBlazor with primary colour Indigo.
- Long-running operations show progress or at least a spinner; uploads show progress.

## Language

- UI in German; code, data structures, comments and commits in English; docs and specs in German.
- Swiss formats (dd.MM.yyyy, CHF).
