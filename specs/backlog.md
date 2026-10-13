# Backlog

Dieses Backlog listet Ideen für künftige Stories. Setze diese Ideen nur um, wenn der User es verlangt.

Ausgearbeitete Stories liegen in [`stories/`](stories/) (Vorlage: [`_template.md`](stories/_template.md)). Dieses Backlog verlinkt sie.

## Als Nächstes

Reihenfolge: Jede Story baut auf der vorherigen auf.

### Beleg hochladen und ablegen

> Detail-Story: [#008](stories/008-beleg-upload-und-ablage.md)

- Upload mit Fortschritt, fortlaufende Belegnummer, Ablage im Blob Storage nach ADR-001, Beleg im Journal öffnen.

### KI-Extraktion aus dem Beleg

> Detail-Story: [#009](stories/009-ki-extraktion.md)

- Claude liest den Beleg und schlägt die Buchung vor. Der User bestätigt. Erst dann Ablage und Buchung.

## Später

### E-Mail-Versand
- Einladungen, Passwort-Reset und Bestätigungen per E-Mail statt Link kopieren.

### E-Mail-Eingang (IMAP)
- Belege per E-Mail an eine Mailbox weiterleiten (siehe [Konzept](concept.md#eingangskanäle)). Nutzt die Analyse aus #009.

### Deployment nach Azure
- Bicep in `deploy/`, Deployment aus GitHub Actions, Managed Identity, Key Vault.

### Security-Header
- Content-Security-Policy, HSTS und weitere (siehe [Sicherheit](security.md#web-sicherheit)).

### Backup & Restore
- Siehe [Backup](backup.md).

### Jahresabschluss und Export
- Jahr abschliessen (read-only, Immutability-Policy auf Belegen), Export als Excel/PDF, Dossier für den Treuhänder (siehe [Journal](journal.md)).

### Native App als «Teilen»-Ziel
- Belege direkt aus iOS über «Senden an» oder «Teilen» an *Flowtr* übergeben (siehe [Konzept](concept.md#eingangskanäle)).

### MWST
- Aktuell nicht MWST-pflichtig. Das Datenmodell ist vorbereitet (siehe [Journal](journal.md#felder-einer-buchung)). Erfassung und Auswertung folgen bei Bedarf.
