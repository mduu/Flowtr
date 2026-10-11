# Backup & Restore

Die Daten (Journal und Belege) dürfen auf gar keinen Fall verloren gehen können. Die Lösung muss zwingend sauber gesichert werden.

## Anforderungen
- Redundante Sicherung (z.B. 1× pro Monat zusätzlich offline/on-prem).
- Bevorzugt: laufendes Offsite-Backup von Azure nach on-prem über einen entsprechenden Cron-Job o.ä.
  - Ziel z.B. FTP-Upload oder Cloud Sync auf ein on-prem Synology NAS, sofern *Flowtr* die entsprechenden Funktionen anbietet. Das ist besser als manuelles on-prem Sichern.
- *Flowtr* sendet Alerts, wenn das Backup eine Zeit lang nicht funktioniert.

> TBD: Restore-Verfahren und regelmässiger Restore-Test.
