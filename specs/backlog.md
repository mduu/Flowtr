# Backlog

Dieses Backlog listet Ideen für künftige Stories. Setze diese Ideen nur um, wenn der User es verlangt.

Ausgearbeitete Stories liegen in [`stories/`](stories/) (Vorlage: [`_template.md`](stories/_template.md)). Dieses Backlog verlinkt sie.

## Als Nächstes

### Deployment nach Azure

> Detail-Story: [#011](stories/011-azure-deployment.md)

- *Flowtr* in Azure mit minimalen Kosten (Scale to Zero, Azure SQL Free Tier), Deployment aus GitHub Actions, Security-Header, Minimum an Backups.

### Abrechnungen automatisch abholen

> Detail-Stories: [#015](stories/015-stripe-abrechnung-automatisch.md) (Stripe), [#016](stories/016-apple-abrechnung-automatisch.md) (Apple)

- *Flowtr* holt nach jedem Monat die Abrechnung per API und legt fertige Vorschläge in den Eingang: bei Stripe Einnahme (brutto) und Gebühren, bei Apple den Erlös. Baut auf [#014](stories/done/014-csv-als-beleg.md) auf (CSV als Beleg). Seit [#017](stories/done/017-mehrere-buchungen-pro-beleg.md) schlägt die KI beide Buchungen aus einem hochgeladenen Beleg vor: weniger dringend.

## Später

### E-Mail-Versand
- Einladungen, Passwort-Reset und Bestätigungen per E-Mail statt Link kopieren.

### Backup & Restore
- Siehe [Backup](backup.md).

### Jahresabschluss
- Jahr abschliessen: danach read-only, Immutability-Policy auf den Belegen (siehe [Journal](journal.md#bearbeitung--abschluss)).

### Dossier für den Treuhänder
- Link mit Code per E-Mail, 14 Tage gültig, widerrufbar (siehe [Journal](journal.md#dossier-für-den-treuhänder)). Braucht zuerst den E-Mail-Versand. Bis dahin genügt der Export als ZIP ([#013](stories/done/013-export.md)).

### Native App als «Teilen»-Ziel
- Belege direkt aus iOS über «Senden an» oder «Teilen» an *Flowtr* übergeben (siehe [Konzept](concept.md#eingangskanäle)).

### MWST
- Aktuell nicht MWST-pflichtig. Das Datenmodell ist vorbereitet (siehe [Journal](journal.md#felder-einer-buchung)). Erfassung und Auswertung folgen bei Bedarf.
