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

- Responsive (iPhone to desktop), should feel like a desktop app; MudBlazor with primary colour Indigo.
- Long-running operations show progress or at least a spinner; uploads show progress.

## Language

- UI in German; code, data structures, comments and commits in English; docs and specs in German.
- Swiss formats (dd.MM.yyyy, CHF).
