# ADR-007: Belegnummern per Sequenz mit optimistischer Sperre

## Status

**Akzeptiert** (14.10.2026)

## Kontext

Jeder abgelegte Beleg erhält eine fortlaufende Nummer pro Jahr und Art, z.B. `A-2026-007` (Ausgaben) oder `E-2026-003` (Einnahmen). Die Nummer steht im Dateinamen im Blob Storage ([ADR-001](001-belegablage-in-blob-storage.md)) und verbindet Beleg und Buchung.

- Zwei Belege dürfen nie dieselbe Nummer erhalten, auch wenn zwei User gleichzeitig speichern.
- Es soll keine Lücken geben, ausser ein User löscht eine Buchung bewusst.
- Blob und Datenbank lassen sich nicht in einer Transaktion schreiben ([ADR-001](001-belegablage-in-blob-storage.md), Konsequenzen).
- Die Tests laufen ohne SQL Server (EF Core In-Memory bzw. SQLite).

## Entscheidung

Wir entscheiden uns für eine **Sequenz-Tabelle mit optimistischer Sperre**:

- Tabelle `ReceiptSequences` mit einer Zeile pro Jahr und Art. `LastNumber` ist ein Concurrency-Token.
- Beim Speichern einer Buchung mit neuem Beleg erhöht Flowtr `LastNumber` und speichert Sequenz, Beleg und Buchung in **einem** `SaveChanges`, also in einer Transaktion. Die Nummer zählt nur, wenn die Buchung gespeichert ist.
- Speichert jemand gleichzeitig, scheitert der zweite Speichervorgang am Concurrency-Token. Flowtr lädt die Sequenz neu und versucht es mit der nächsten Nummer, höchstens fünfmal.
- **Reihenfolge:** zuerst der Blob nach `belege/`, dann die Datenbank. Scheitert die Datenbank, löscht Flowtr den Blob wieder. Ein Blob überschreibt nie einen bestehenden (Bedingung `If-None-Match: *`). Ein belegter Name gilt als belegte Nummer.
- **Ersetzen** eines Belegs behält die Nummer. **Entfernen** oder **Löschen der Buchung** gibt die Nummer nicht frei: Es entsteht eine Lücke, die das Audit-Log erklärt.
- Jahr und Art einer Buchung mit Beleg lassen sich nicht ändern, denn sie sind Teil der Nummer. Ausnahme: Der User hängt gleichzeitig einen neuen Beleg an. Dann erhält dieser eine neue Nummer.
- Ein täglicher Job meldet Blobs ohne Beleg und Belege ohne Blob und löscht Uploads, die älter als 30 Tage sind.

## Optionen

### Option A: Sequenz-Tabelle mit optimistischer Sperre (gewählt)

**Vorteile:**
- Keine Lücken durch Abbrüche, keine doppelten Nummern
- Reines EF Core: läuft auf SQL Server und in den Tests
- Kein Sperren über die Dauer eines Uploads

**Nachteile:**
- Bei sehr vielen gleichzeitigen Speichervorgängen gibt es Wiederholungen (bei Flowtr praktisch nie)

### Option B: SQL-Server-`SEQUENCE`

**Vorteile:**
- Einfach und schnell

**Nachteile:**
- Eine Sequenz macht keinen Rollback: Abbrüche hinterlassen Lücken
- Eine Sequenz pro Jahr und Art müsste Flowtr selbst anlegen
- Läuft nicht in den Tests ohne SQL Server

### Option C: `MAX(Nummer) + 1` mit Unique-Index

**Vorteile:**
- Keine zusätzliche Tabelle

**Nachteile:**
- Konflikte erst beim Unique-Index; nach dem Löschen der höchsten Nummer würde Flowtr sie erneut vergeben

## Konsequenzen

### Positive Auswirkungen

- Nummern sind eindeutig und lückenlos, ausser bei bewusstem Löschen.
- Kein Beleg überschreibt einen anderen, auch nicht bei Fehlern.

### Negative Auswirkungen / Risiken

- Zwischen Blob und Datenbank kann ein Fehler einen verwaisten Blob hinterlassen. Der tägliche Job meldet ihn.
- EF Core In-Memory speichert nicht transaktional. Tests von Wiederholungen laufen deshalb mit SQLite.

### Aufwand

- Gering: eine Tabelle, ein Service (`ReceiptFiling`), ein Job.

## Referenzen

- [ADR-001: Belegablage in Azure Blob Storage](001-belegablage-in-blob-storage.md)
- [Story #008](../stories/done/008-beleg-upload-und-ablage.md)
