# Agent Instructions

**Flowtr** is a simple bookkeeping web app for the income and expenses of a side business (Swiss sole proprietorship). The user uploads a receipt or forwards it by email. AI extracts the booking data and the user confirms it. Flowtr then files the numbered receipt and writes the journal entry. Once a year, the user exports the journal and the receipts for the tax return or the fiduciary.

Stack: .NET (latest), Blazor Interactive WebAssembly, ASP.NET Core WebAPI, MudBlazor, EF Core on SQL Server (Azure SQL in production), Aspire for local development.

This file gives the short rules that always apply. The specs give the details. Before you change an area, read its spec:

| Area | Spec |
|---|---|
| Receipt capture, AI extraction, input channels | `specs/concept.md` |
| Journal, closing, export, fiduciary dossier | `specs/journal.md` |
| Security requirements | `specs/security.md` |
| Backup and restore | `specs/backup.md` |
| Stack, principles, components, roles | `specs/architecture.md` |
| Coding rules | `specs/coding-guidelines.md` |
| UX, colours | `specs/ux.md` |
| Domain terms | `specs/glossary.md` |
| Decisions | `specs/adrs/` |
| Stories | `specs/stories/` (template `_template.md`) |
| Backlog (do not start work on it unless the user asks) | `specs/backlog.md` |

## Repository Layout

| Folder | Content |
|---|---|
| `src/` | Source code and tests. Solution: `src/Flowtr.slnx`. Package versions: only in `src/Directory.Packages.props`. |
| `src/Flowtr.AppHost/` | Aspire AppHost: SQL Server and Azurite containers, server |
| `src/Flowtr.ServiceDefaults/` | Aspire service defaults: telemetry, health checks, resilience |
| `src/Flowtr.Server/` | ASP.NET Core host: API, Identity, EF Core, feature slices in `Features/`, CQS base in `CrossCutting/Cqs/` |
| `src/Flowtr.Client/` | Blazor WebAssembly UI with MudBlazor |
| `src/Flowtr.Shared/` | Types that the server and the client share (`Result`, DTOs) |
| `src/Flowtr.Tests/` | Tests (xUnit and Shouldly) |
| `deploy/` | Infrastructure as Code (Bicep) and deployment scripts |
| `docs/` | Operating and user documentation (how to run and use Flowtr) |
| `specs/` | Specifications (what we build and why) |
| `assets/` | Logo and images |
| `.github/workflows/` | CI (`ci.yml`): build and test on each push to `main` and each pull request |
| `.claude/skills/` | Claude Code skills |

## Running

Run `dotnet run --project src/Flowtr.AppHost`. Docker must run.

- The AppHost starts the SQL Server and Azurite containers (persistent volumes) and the server.
- The AppHost shows the URL of the Aspire dashboard on start.
- The server alone has no database connection. Always start Flowtr with the AppHost.
- In Development, the server applies the EF migrations on start.

Build: `dotnet build src/Flowtr.slnx`. Test: `dotnet test src/Flowtr.slnx`.

## GitHub Actions

Use as little GitHub storage as possible. The repo keeps artifacts and logs for 1 day.

- CI must stay green. Tests run without Docker and without external services.

- Upload an artifact only when a later job or a person needs it. Always set `retention-days: 1`.
- Deploy to Azure directly from the build job. Do not keep the build output as an artifact.
- Cache NuGet packages only with a key from `src/Directory.Packages.props`. This stops old caches from collecting.

## Key Principles

- **Vertical slices and CQS**: one use case is one file in `Commands/` or `Queries/`. The file has a record and a handler with the method `Handle`. `Handle` returns `Result` or `Result<T>`.
- **Single tenant** per installation. Do not write multi-tenancy code.
- **RBAC**: each user has one or more roles (Admin, Contributor).
- **Never lose data**: the journal and the receipts are critical. A closed calendar year is read-only. This includes its receipts.
- **Audit everything**: each create, update and delete of a booking or a receipt goes into the audit log (ADR-006).
  - A new audited entity needs a rule in `CrossCutting/Audit/AuditRules.cs`, a key that it gets before the save (for example a GUID), and a test.
  - Do not use `ExecuteUpdate`, `ExecuteDelete` or raw SQL on audited entities: the audit log does not see them.
- **Security is part of the foundation**. It is not an add-on. Flowtr keeps accounting data.

## Coding

- Do not use a `_` prefix on fields.
- Do not use an `Async` suffix on method names.
- A page loads its data one call after the other. Do not use `Task.WhenAll`: during prerendering, the server implementations use the same DbContext.
- Tests use `TestApp` (EF Core in memory, fakes for blob storage, AI and exchange rates). EF Core in memory does not roll back a failed save: for tests of retries and transactions, use `new TestApp(transactional: true)` (SQLite in memory).
- Do not call external services in tests. Put them behind an interface (for example `IReceiptReader`, `IExchangeRates`, `IReceiptStore`).
- Comments (details in `specs/coding-guidelines.md`):
  - First make the code clearer, for example with Extract Method. Give the method a name that tells what it does.
  - Write a comment only when the code cannot show the information and the comment adds real value.
  - Keep each comment as short as possible.
  - Write comments on members as XML documentation.
  - Usually, do not comment private members.

## Writing

- Write English text (comments, commits, this file) in ASD-STE100 Simplified Technical English. Use the skill in `.claude/skills/asd-ste100/`.
- Write German text (UI, docs, specs) with the same principles: short sentences, one statement in each sentence, active voice, no filler words.

## UX

- **Account pages (`/Account/*`) are static SSR**, because Identity needs `HttpContext`. See ADR-003.
  - On these pages, and in all components that they render (for example `MainLayout`), use only MudBlazor components that work without interactivity.
  - Permitted: `MudStatic*` inputs (MudBlazor.StaticInput), `MudPaper`, `MudText`, `MudAlert`, `MudLink`, `MudButton` with `Href` or `ButtonType.Submit`.
  - Not permitted: `MudMenu`, `MudDialog`, `MudSelect` and other interactive components.
- The UI is responsive (iPhone to desktop) and feels like a desktop app. Use MudBlazor with the primary colour Indigo.
- The UI has a dark mode (ADR-005). In your own CSS, use the MudBlazor variables (`--mud-palette-*`). Do not use fixed colours.
- Long operations show progress, or at minimum a spinner. Uploads show progress.

## Language

- UI: German.
- Code, data structures, comments and commits: English.
- Docs and specs: German.
- Formats: Swiss (dd.MM.yyyy, CHF).
