# Backlog

Dieses Backlog listet Ideen für künftige Stories. Setze diese Ideen nur um, wenn der User es verlangt.

Ausgearbeitete Stories liegen in [`stories/`](stories/) (Vorlage: [`_template.md`](stories/_template.md)). Dieses Backlog verlinkt sie.

## Als Nächstes

### Deployment nach Azure

> Detail-Story: [#011](stories/011-azure-deployment.md)

- *Flowtr* in Azure mit minimalen Kosten (Scale to Zero, Azure SQL Free Tier), Deployment aus GitHub Actions, Security-Header, Minimum an Backups.

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
