# Flowtr

[![CI](https://github.com/mduu/Flowtr/actions/workflows/ci.yml/badge.svg)](https://github.com/mduu/Flowtr/actions/workflows/ci.yml)

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/logo/flowtr_logo_dark.svg">
  <img src="assets/logo/flowtr_logo.svg" alt="Flowtr Logo" width="260">
</picture>

Einfache Buchhaltung für Einnahmen und Ausgaben eines Nebenerwerbs. Jede Buchung hat ihren Beleg.

## Vision
Flowtr ist eine Web-App. Mit ihr erfassen Einzelfirmen und Nebenerwerbstätige ihre Einnahmen und Ausgaben einfach. Einmal im Jahr exportiert der User auf Knopfdruck alle Unterlagen für den Treuhänder oder die Schweizer Steuererklärung.

## Schlüsselkonzepte
- **Beleg rein, Buchung raus:** Der User lädt den Beleg hoch oder leitet ihn per Email weiter. Die KI extrahiert die Buchungsdaten. Der User bestätigt oder korrigiert sie.
- **Saubere Ablage:** Flowtr nummeriert die Belege, benennt sie um und legt sie 1:1 ab. Jede Buchung im Journal referenziert ihren Beleg.
- **Journal (Milchbüchlein):** Enthält alles für die Schweizer Steuererklärung. Nach der Abgabe ist es read-only.
- **Export auf Knopfdruck:** Journal + Belege als Excel/PDF oder als gesichertes *Dossier* für den Treuhänder.
- **Keine Daten verlieren:** Redundante Backups, auch offsite/on-prem. Alerts bei Ausfall.
- **Security als Basis**, nicht als Zusatz.

## Repository-Struktur

```
├── src/                        Quellcode und Tests, Solution Flowtr.slnx
│   ├── Flowtr.AppHost/         Aspire: startet SQL Server, Azurite und den Server
│   ├── Flowtr.ServiceDefaults/ Aspire Service Defaults: Telemetrie, Health Checks
│   ├── Flowtr.Server/          ASP.NET Core Host: API, Identity, EF Core, Features/
│   ├── Flowtr.Client/          Blazor WebAssembly UI mit MudBlazor
│   ├── Flowtr.Shared/          Gemeinsame Typen für Server und Client (Result, DTOs)
│   └── Flowtr.Tests/           Tests (xUnit, Shouldly)
├── deploy/                     Infrastructure as Code (Bicep) und Deployment-Skripte
├── docs/                       Betriebs- und Benutzerdokumentation (z.B. Synology-Backup einrichten, Restore-Checkliste)
├── specs/                      Spezifikationen: Konzept, Architektur, ADRs, Stories, Backlog
├── assets/                     Logo und Bildmaterial
└── .github/workflows/          CI: Build und Tests bei jedem Push auf main und jedem Pull Request
```

**`specs/` oder `docs/`?** `specs/` beschreibt, *was* wir bauen und *warum*. `docs/` beschreibt, wie man Flowtr betreibt und benutzt.

## Entwicklung

Voraussetzungen: .NET 10 SDK, Docker.

```bash
dotnet run --project src/Flowtr.AppHost   # startet SQL Server, Azurite und die App via Aspire
dotnet test src/Flowtr.slnx
```

**Erster Admin:** Flowtr hat keine öffentliche Registrierung ([ADR-004](specs/adrs/004-zugang-nur-auf-einladung.md)). Die Adressen in der Konfiguration `Admins` erhalten immer die Rolle Admin (lokal in `src/Flowtr.Server/appsettings.Development.json`, im Betrieb als App-Einstellung `Admins__0`, `Admins__1` …). Hat eine Adresse noch kein Konto, erstellt Flowtr beim Start eine Einladung und schreibt den Link ins Log. Flowtr entzieht die Rolle nie automatisch: Admins verwalten die Rollen unter «Benutzer». Für Links in Einladungen nimmt Flowtr die Adresse des Requests. `PublicUrl` überschreibt sie (z.B. hinter einem Proxy).

## Spezifikationen
| Dokument | Inhalt |
| :-- | :-- |
| [`specs/concept.md`](specs/concept.md) | Belegerfassung, KI-Extraktion, Eingangskanäle |
| [`specs/journal.md`](specs/journal.md) | Journal, Abschluss/Archiv, Export, Treuhänder-Dossier |
| [`specs/security.md`](specs/security.md) | Sicherheitsanforderungen |
| [`specs/backup.md`](specs/backup.md) | Backup & Restore, Offsite, Alerting |
| [`specs/architecture.md`](specs/architecture.md) | Stack, Prinzipien, Komponenten, Rollen |
| [`specs/coding-guidelines.md`](specs/coding-guidelines.md) | Verbindliche Coding-Regeln |
| [`specs/ux.md`](specs/ux.md) | UX & Design |
| [`specs/glossary.md`](specs/glossary.md) | Fachbegriffe und ihre Bedeutung |
| [`specs/adrs/`](specs/adrs/) | Architekturentscheide (ADR) |
| [`specs/backlog.md`](specs/backlog.md) | Ideen für künftige Stories |
| [`specs/stories/`](specs/stories/) | Ausgearbeitete Stories |

## Agents
Die agent-agnostische Konfiguration für KI-Coding-Agents liegt in [`AGENTS.md`](AGENTS.md). [`CLAUDE.md`](CLAUDE.md) verweist nur darauf.
