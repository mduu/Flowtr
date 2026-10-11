# Backup & Restore

Die Daten (Journal und Belege) dürfen auf gar keinen Fall verloren gehen können. Die Lösung muss zwingend sauber gesichert werden.

## Anforderungen
- Redundante Sicherung: Azure (inkl. Point-in-Time-Restore von Azure SQL, Blob-Versionierung) und zusätzlich offsite/on-prem.
- *Flowtr* sendet Alerts, wenn das Backup eine Zeit lang nicht funktioniert.

## Offsite-Backup auf Synology (Pull)
- *Flowtr* exportiert regelmässig einen Datenbank-Dump nach Azure Blob Storage.
- Das on-prem Synology NAS holt Dumps und Belege selbst ab (z.B. Cloud Sync / Hyper Backup). Es braucht keinen von aussen erreichbaren Port.
- *Flowtr* überwacht den Zeitstempel des letzten erfolgreichen Abholens bzw. Exports und alarmiert bei Ausfall.

## Restore-Test
- Manuell, **einmal pro Jahr** (z.B. vor dem Jahresabschluss), anhand einer Checkliste.
