# Flowtr
<img src="assets/logo/flowtr_logo.svg" alt="Flowtr Logo" width="260">

Einfache Buchhaltung für Ausgaben und Einnahmen als Nebenerwerb. Tracken von Buchungen mit passenden Belegen.

## Vision
Flowtr ist eine Web-App, welche das Erfassen von Ein- und Ausgaben für Einzelfirmen bzw. Nebenerwerb möglichst einfach macht. Das Ziel ist, dass einmal im Jahr auf Knopfdruck alle Unterlagen für den Treuhänder bzw. die Schweizer Steuerabrechnung exportiert werden können.

## Schlüsselkonzepte
- **Beleg rein, Buchung raus:** Beleg hochladen oder per Email weiterleiten – KI extrahiert die Buchungsdaten, der User bestätigt oder korrigiert.
- **Saubere Ablage:** Belege werden nummeriert, umbenannt und 1:1 abgelegt; jede Buchung im Journal referenziert ihren Beleg.
- **Journal (Milchbüchlein):** Enthält alles für die Schweizer Steuererklärung; nach Abgabe read-only.
- **Export auf Knopfdruck:** Journal + Belege als Excel/PDF oder als gesichertes *Dossier* für den Treuhänder.
- **Keine Daten verlieren:** Redundante Backups inkl. Offsite/On-Prem, Alerts bei Ausfall.
- **Security als Basis**, nicht als Zusatz.

## Repository-Struktur

```
├── src/      Quellcode inkl. Tests (Solution, Server, Client, Aspire AppHost)
├── deploy/   Infrastructure as Code (Bicep) und Deployment-Skripte
├── docs/     Betriebs- und Benutzerdokumentation (z.B. Synology-Backup einrichten, Restore-Checkliste)
├── specs/    Spezifikationen: Konzept, Architektur, ADRs, Stories, Backlog
└── assets/   Logo und Bildmaterial
```

**`specs/` oder `docs/`?** `specs/` beschreibt, *was* gebaut wird und *warum*. `docs/` beschreibt, wie man das Gebaute betreibt und benutzt.

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
