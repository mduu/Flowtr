# Backlog

Dieses Backlog listet Ideen für künftige Stories. Setze diese Ideen nur um, wenn der User es verlangt.

Ausgearbeitete Stories liegen in [`stories/`](stories/) (Vorlage: [`_template.md`](stories/_template.md)). Dieses Backlog verlinkt sie.

## Später

### E-Mail-Versand
- Einladungen, Passwort-Reset und Bestätigungen per E-Mail statt Link kopieren.

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
