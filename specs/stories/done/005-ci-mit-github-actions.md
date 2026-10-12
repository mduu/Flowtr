# Story: #005, CI mit GitHub Actions

**Status:** Done (12.10.2026)

## Beschreibung

Bei jedem Push auf `main` und bei jedem Pull Request baut GitHub Actions die Solution und führt alle Tests aus. Ein roter Build fällt sofort auf.

## Hintergrund

- Regeln für GitHub Actions in [AGENTS.md](../../../AGENTS.md#github-actions): möglichst wenig Speicher, Artefakte und Logs nur 1 Tag, NuGet-Cache mit Schlüssel aus `src/Directory.Packages.props`.
- Das Repo ist öffentlich. Actions-Minuten und Speicher sind darum kostenlos.
- Die Tests brauchen weder Docker noch externe Dienste (EF Core In-Memory). So läuft die CI ohne Container.
- Deployment gehört nicht in diese Story. Es folgt mit der Story für Azure.

## Umsetzung

- Workflow `.github/workflows/ci.yml`: `restore`, `build` (Release), `test`. Auslöser: Push auf `main`, Pull Request, manuell.
- Nur Leserechte (`contents: read`), Timeout 15 Minuten. Ein neuer Push bricht den laufenden Build desselben Branches ab.
- Keine Artefakte. NuGet-Cache mit Schlüssel aus `src/Directory.Packages.props`.
- Badge im `README.md`.
- Lokal in einem frischen Klon mit denselben Befehlen geprüft (Build ohne Warnungen, 48 Tests grün).

## Akzeptanzkriterien

- [x] Push auf `main` und Pull Requests starten Build und Tests.
- [x] Der Workflow lädt keine Artefakte hoch.
- [x] Der NuGet-Cache hängt nur von `src/Directory.Packages.props` ab.
- [x] Der Status ist im `README.md` sichtbar.

## Aufgaben

- [x] Workflow erstellen
- [x] Lokal mit denselben Befehlen prüfen
- [x] `README.md` und `AGENTS.md` ergänzen

## Definition of Done

- [x] Applikation buildet, automatische Tests laufen grün
- [x] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft (keine UI-Änderung)
- [x] `README.md` und `/specs` aktualisiert, falls nötig
- [x] Architekturentscheide als ADR in `/specs/adrs` erfasst (keiner nötig)
- [x] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [x] Story aus `/specs/backlog.md` entfernt (nie im Backlog)
