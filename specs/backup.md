# Backup & Restore

Die Daten (Journal und Belege) dürfen nie verloren gehen. Flowtr muss sie zuverlässig sichern.

## Anforderungen
- Redundante Sicherung: in Azure (Point-in-Time-Restore von Azure SQL, Blob-Versionierung) und zusätzlich offsite/on-prem.
- *Flowtr* sendet Alerts, wenn das Backup eine Zeit lang nicht funktioniert.

## Offsite-Backup auf Synology (Pull)
- *Flowtr* exportiert regelmässig einen Datenbank-Dump nach Azure Blob Storage.
- Das on-prem Synology NAS holt Dumps und Belege selbst ab (z.B. mit Cloud Sync oder Hyper Backup). Es braucht keinen Port, der von aussen erreichbar ist.
- *Flowtr* überwacht den Zeitstempel des letzten erfolgreichen Abholens und Exports. Bei Ausfall sendet *Flowtr* einen Alert.

## Restore-Test
- Manuell, **einmal pro Jahr** (z.B. vor dem Jahresabschluss), mit einer Checkliste.
