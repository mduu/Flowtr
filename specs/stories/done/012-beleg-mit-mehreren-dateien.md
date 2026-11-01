# Story: #012, Beleg mit mehreren Dateien

**Status:** Done (01.11.2026)

## Beschreibung

Ein Beleg kann aus mehreren Dateien bestehen, zum Beispiel Rechnung und Quittung derselben Zahlung oder eine Rechnung als mehrere Fotos. Eine Mail mit mehreren Anhängen gibt einen Eintrag im Eingang und eine Buchung mit einer Belegnummer. Die KI liest alle Dateien zusammen.

## Hintergrund

- Bisher: Ein Beleg ist eine Datei. Eine Mail mit mehreren Anhängen gibt mehrere Belege ([#010](010-email-eingang-imap.md)). Stripe (z.B. Anthropic) schickt Invoice und Receipt zusammen: Das gab zwei Einträge im Eingang für eine Zahlung.
- Die GeBüV verlangt unveränderte Belege: Flowtr legt jede Datei einzeln und unverändert ab, kein Zusammenfügen zu einem PDF.
- Belegnummern: [ADR-007](../../adrs/007-belegnummern-per-sequenz.md). Ablage: [ADR-001](../../adrs/001-belegablage-in-blob-storage.md). Mail als Beleg: [ADR-009](../../adrs/009-email-eingang-mail-als-beleg.md).

## Entscheide

- **Ein Beleg, eine Nummer, 1 bis 10 Dateien.** Die Buchung hat weiterhin höchstens einen Beleg.
- **Dateinamen:** Die erste Datei heisst wie bisher `A-2026-007_azure-abo.pdf`, die weiteren `A-2026-007_azure-abo_2.pdf`, `_3` usw. Eine Position wird nie wiederverwendet: Wird die zweite Datei entfernt, heisst die nächste `_3`. Bestehende Belege behalten ihre Namen.
- **Eingang:** ein Eintrag pro Mail mit allen ihren Dateien (Anhänge als PDF oder Bild, ohne Logos). Ohne solchen Anhang ist die Mail selbst die einzige Datei (`.eml`).
- **KI:** liest alle Dateien des Belegs in einer Anfrage und macht einen Vorschlag. Der System-Prompt sagt: mehrere Dateien sind ein Geschäftsfall (z.B. Rechnung und Quittung). Höchstens 5 Dateien und 30 MB gehen an die KI, sonst die ersten.
- **Buchungsdialog:** Liste der Dateien mit Ansehen und Entfernen pro Datei. «Datei hinzufügen» nimmt eine oder mehrere Dateien. Nach dem Hinzufügen liest die KI alle neuen Dateien zusammen und füllt nur leere Felder.
- **Bestehende Buchung:** Hinzufügen behält die Nummer. Entfernen einzelner Dateien behält die Nummer. Alle Dateien entfernen entfernt den Beleg (Lücke in der Nummerierung, wie bisher).
- **Jahr und Art ändern:** nur, wenn alle bisherigen Dateien entfernt werden. Neue Dateien erhalten dann eine neue Nummer (wie bisher).
- **Journal:** Spalte «Beleg» zeigt die Nummer und bei mehreren Dateien die Anzahl. Der Link öffnet die erste Datei.
- **Doppel:** pro Datei über den SHA-256, gegen Eingang und abgelegte Dateien.
- **Audit:** Jede Datei eines Belegs geht ins Audit-Log (hinzugefügt, entfernt).
- **Migration:** Jeder bestehende Beleg erhält seine Datei als Position 1. Jeder bestehende Eintrag im Eingang erhält seine Datei. Einträge derselben Mail führt die Migration nicht zusammen: Der User verwirft einen davon.

## Umsetzung

- **Modell:** `Receipt` hat `Files` (`ReceiptFile`: Position, Blob, Datei, Typ, Grösse, SHA-256) und `LastFilePosition`. `InboxItem` (einer pro Mail) hat `Files` (`InboxFile`). Beide neuen Tabellen im Audit-Log («Belegdatei», «Eingang (Datei)»). Migration `AddFilesToReceiptsAndInbox` übernimmt jeden bestehenden Beleg und Eingangs-Eintrag als Position 1, geprüft auf SQL Server mit Testdaten.
- **Ablage:** `ReceiptFiling.Save` nimmt neue Uploads und entfernte Dateien. Ohne Dateien geht der Beleg. Jahr oder Art ändern nur ohne bisherige Dateien (`CheckChange`).
- **API:** `BookingInput.UploadIds` und `RemovedFileIds` statt `UploadId` und `RemoveReceipt`. `BookingView.ReceiptFiles`. Datei: `GET /api/receipts/files/{id}`. Analyse: `POST /api/receipts/uploads/analysis` mit allen Uploads.
- **KI:** `AnalyzeUploads` schickt höchstens 5 Dateien und 20 MB in einer Anfrage. Eine Datei, die die KI nicht lesen kann (HEIC, zu gross), bleibt weg, wenn eine andere lesbar ist. Der System-Prompt sagt: ein Geschäftsfall, nicht die Summe der Dateien.
- **UI:** Beleg-Panel mit Liste der Dateien (Ansehen, Entfernen) und «Weitere Datei hinzufügen» (auch mehrere). Journal: «A-2026-001 · 2». Eingang: Zeichen mit der Anzahl Dateien. Mehrere Dateien auf das Journal ziehen gibt eine Buchung.
- **Geprüft:** 190 Tests (8 neue). Im Browser lokal mit einer Mail mit Invoice und Receipt als PDF: ein Eintrag im Eingang, Vorschlag der KI CHF 16.46 (USD 20.00, nicht die Summe), Buchen mit einer Nummer und zwei Dateien, Entfernen einer Datei behält die Nummer, Ansicht in Handy-Breite.

## Akzeptanzkriterien

- [x] Eine Mail mit Invoice und Receipt als PDF gibt einen Eintrag im Eingang mit zwei Dateien und einem Vorschlag der KI.
- [x] Buchen gibt eine Belegnummer mit zwei Dateien in der Ablage (`…_azure-abo.pdf`, `…_azure-abo_2.pdf`).
- [x] Im Buchungsdialog kann der User Dateien hinzufügen (auch mehrere auf einmal) und einzeln entfernen. Jede Datei lässt sich ansehen.
- [x] Bei einer bestehenden Buchung bleibt die Nummer beim Hinzufügen und Entfernen einzelner Dateien.
- [x] Bestehende Belege und Einträge im Eingang funktionieren nach der Migration unverändert.
- [x] Die KI liest mehrere Dateien zusammen und macht einen Vorschlag.
- [x] Hinzufügen und Entfernen von Dateien steht im Audit-Log.
- [x] Ein abgeschlossenes Jahr bleibt read-only, auch für die Dateien.

## Aufgaben

- [x] Modell: `ReceiptFile` (Beleg hat Dateien), `InboxFile` (Eintrag im Eingang hat Dateien). Migration mit Übernahme der Daten
- [x] Ablage: mehrere Dateien pro Beleg, Namen mit Position, Hinzufügen und Entfernen
- [x] Eingang: ein Eintrag pro Mail, Verwerfen und Verfall für alle Dateien
- [x] KI: mehrere Dateien in einer Anfrage, Grenzen
- [x] API und Client: Liste der Dateien im Dialog, im Journal und im Eingang
- [x] Audit-Log, Housekeeping, Doppel
- [x] Tests
- [x] Specs: ADR-001, ADR-009 (Nachtrag), [Konzept](../../concept.md)

## Definition of Done

- [x] Applikation buildet, automatische Tests laufen grün
- [x] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [x] `README.md` und `/specs` aktualisiert, falls nötig
- [x] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [x] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [x] Story aus `/specs/backlog.md` entfernt
