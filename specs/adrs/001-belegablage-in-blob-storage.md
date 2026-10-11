# ADR-001: Belegablage in Azure Blob Storage

## Status

**Akzeptiert** (11.10.2026)

## Kontext

Belege (PDFs, Bilder, Email-Anhänge) sind neben dem Journal die wichtigsten Daten von *Flowtr*. Für ihre Ablage gelten diese Anforderungen:

- Belege werden 1:1 als Dateien abgelegt, nummeriert und so umbenannt, dass sie dem Journal zugeordnet werden können ([Konzept](../concept.md#ablage-der-belege)).
- Belege abgeschlossener Jahre müssen unveränderbar sein ([Sicherheit](../security.md#unveränderbarkeit)).
- Geschäftsbücher und Belege sind 10 Jahre aufzubewahren (OR 958f).
- Belege dürfen auf keinen Fall verloren gehen. Ein on-prem Synology NAS holt sie als Offsite-Backup ab ([Backup](../backup.md)).
- Lokal wird mit Aspire und Azurite entwickelt.

## Entscheidung

Wir legen Belege als **Blobs in einem Azure Storage Account** ab.

Blob Storage erfüllt als einzige Option die Anforderung an die Unveränderbarkeit (Immutability-Policies pro Blob) und wird lokal von Azurite emuliert.

### Container

```
belege/    Bestätigte Belege. Versionierung, Soft-Delete und Immutability auf Versionsebene aktiv.
  {jahr}/einnahmen/E-{jahr}-{nr}_{bezeichnung}.{ext}
  {jahr}/ausgaben/A-{jahr}-{nr}_{bezeichnung}.{ext}
eingang/   Noch unbestätigte Uploads und Email-Anhänge. Lifecycle-Regel löscht nach 30 Tagen.
backups/   Datenbank-Dumps für das Offsite-Backup.
```

### Jahresabschluss

- Beim Abschluss eines Jahres setzt *Flowtr* auf alle Belege dieses Jahres eine zeitbasierte Aufbewahrungsfrist von **10 Jahren**. Danach können sie weder geändert noch gelöscht werden, auch nicht durch einen Admin.
- Während der Entwicklung bleiben die Policies **unlocked**. Erst im Betrieb werden sie **locked**, denn eine gesperrte Policy lässt sich nicht mehr verkürzen oder entfernen.

### Zugriff

- Die App greift über das Azure SDK mit einer Managed Identity zu. Es gibt keine Account Keys im Code oder in der Konfiguration.
- Downloads (z.B. Treuhänder-Dossier) laufen über kurzlebige SAS-Links oder werden über die API gestreamt.
- Belege älterer Jahre wechseln per Lifecycle-Regel in den Cool- bzw. Cold-Tier.

## Optionen

### Option A: Azure Blob Storage — gewählt

**Vorteile:**
- Immutability-Policies (WORM) pro Blob, Versionierung und Soft-Delete
- Von Azurite emuliert, passt zum lokalen Aspire-Stack
- Synology Cloud Sync kann Azure Blob direkt als Quelle anbinden
- Günstig, mit Speicherklassen für ältere Jahre
- Zugriff per Managed Identity und SAS

**Nachteile:**
- Kein Durchblättern als Netzlaufwerk im Finder; dafür dienen die Web-App, der Azure Storage Explorer oder die Kopie auf dem NAS

### Option B: Azure Files

**Beschreibung:** SMB-Dateifreigabe, als Netzlaufwerk einbindbar.

**Vorteile:**
- Belege lassen sich im Finder durchblättern

**Nachteile:**
- Keine Immutability-Policies, nur Snapshots und Soft-Delete
- Nicht von Azurite emuliert
- SMB braucht Port 445, der von vielen Providern gesperrt wird
- Teurer als Blob Storage

### Option C: Belege in der Datenbank (varbinary)

**Vorteile:**
- Journal und Belege in einer Transaktion und einem Backup

**Nachteile:**
- Azure SQL Storage ist deutlich teurer und die Datenbank wächst stark
- Keine Unveränderbarkeit auf Dateiebene
- Belege sind nicht mehr 1:1 als Dateien verfügbar

## Konsequenzen

### Positive Auswirkungen

- Die Unveränderbarkeit abgeschlossener Jahre wird von der Plattform durchgesetzt, nicht nur von der App.
- Das Offsite-Backup lässt sich mit Bordmitteln der Synology umsetzen.

### Negative Auswirkungen / Risiken

- Journal (SQL) und Belege (Blob) lassen sich nicht transaktional gemeinsam speichern. Ein Beleg wird zuerst nach `belege/` geschrieben und erst danach die Buchung. Verwaiste Blobs ohne Buchung müssen erkannt werden.
- Eine gesperrte Policy ist endgültig. Fehlerhafte Belege abgeschlossener Jahre lassen sich nur noch durch Korrekturbuchungen mit neuem Beleg berichtigen.

### Aufwand

- Gering: Storage Account mit Versionierung, Soft-Delete und Immutability auf Versionsebene via IaC; Abschluss-Job setzt die Policies.

## Referenzen

- [Immutable storage for Azure Blob Storage](https://learn.microsoft.com/azure/storage/blobs/immutable-storage-overview)
- [Blob versioning](https://learn.microsoft.com/azure/storage/blobs/versioning-overview)
- OR 958f – Aufbewahrungspflicht
