# Backlog

In diesem Backlog werden Ideen für künftige Stories gelistet. Gehe diese Ideen nicht an, wenn nicht explizit angefordert.

Ausgearbeitete Stories liegen in [`stories/`](stories/) (Vorlage: [`_template.md`](stories/_template.md)) und werden hier verlinkt.

## Als Nächstes

### Darkmode mit Auto/Hell/Dunkel

> Detail-Story: [#003](stories/003-darkmode.md)

- Umschalter in der AppBar wie in Bau-Ticker (Automatisch/Hell/Dunkel), Wahl im Cookie, funktioniert auf SSR- und WebAssembly-Seiten.

### Zugang nur auf Einladung durch Admin

> Detail-Story: [#004](stories/004-einladung-durch-admin.md)

- Keine öffentliche Registrierung: Admin lädt User per E-Mail ein, der User schliesst über den Link ab. Nicht angenommene Einladungen werden nach 30 Tagen gelöscht ([ADR-004](adrs/004-zugang-nur-auf-einladung.md)).

## Später

### Native App als «Teilen»-Ziel
- Belege direkt aus iOS über «Senden an» bzw. «Teilen» an *Flowtr* übergeben (siehe [Konzept](concept.md#eingangskanäle)).

### MWST
- Aktuell nicht MWST-pflichtig. Das Datenmodell ist vorbereitet (siehe [Journal](journal.md#felder-einer-buchung)), Erfassung und Auswertung folgen bei Bedarf.
