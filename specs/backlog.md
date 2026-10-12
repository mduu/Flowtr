# Backlog

Dieses Backlog listet Ideen für künftige Stories. Setze diese Ideen nur um, wenn der User es verlangt.

Ausgearbeitete Stories liegen in [`stories/`](stories/) (Vorlage: [`_template.md`](stories/_template.md)). Dieses Backlog verlinkt sie.

## Als Nächstes

### Darkmode mit Auto/Hell/Dunkel

> Detail-Story: [#003](stories/003-darkmode.md)

- Umschalter in der AppBar wie in Bau-Ticker (Automatisch/Hell/Dunkel). Die Wahl liegt im Cookie. Der Umschalter funktioniert auf SSR- und WebAssembly-Seiten.

### Zugang nur auf Einladung durch Admin

> Detail-Story: [#004](stories/004-einladung-durch-admin.md)

- Keine öffentliche Registrierung. Der Admin lädt User per E-Mail ein. Der User schliesst die Registrierung über den Link ab. Flowtr löscht nicht angenommene Einladungen nach 30 Tagen ([ADR-004](adrs/004-zugang-nur-auf-einladung.md)).

## Später

### Native App als «Teilen»-Ziel
- Belege direkt aus iOS über «Senden an» oder «Teilen» an *Flowtr* übergeben (siehe [Konzept](concept.md#eingangskanäle)).

### MWST
- Aktuell nicht MWST-pflichtig. Das Datenmodell ist vorbereitet (siehe [Journal](journal.md#felder-einer-buchung)). Erfassung und Auswertung folgen bei Bedarf.
