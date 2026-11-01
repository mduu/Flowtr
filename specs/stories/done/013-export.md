# Story: #013, Export für Treuhänder und Steuererklärung

**Status:** Done (01.11.2026)

## Beschreibung

Der User lädt Journal und Belege eines Zeitraums als ZIP herunter: das Journal als Excel und PDF, die Belege in Ordnern. Er gibt das ZIP dem Treuhänder oder braucht es für die Steuererklärung.

## Hintergrund

- Anforderungen: [Journal](../../journal.md#export). Belege: [ADR-001](../../adrs/001-belegablage-in-blob-storage.md), mehrere Dateien pro Beleg: [#012](012-beleg-mit-mehreren-dateien.md).
- Mails als Beleg (`.eml`) brauchen für den Treuhänder ein PDF. [ADR-009](../../adrs/009-email-eingang-mail-als-beleg.md): QuestPDF hinter einem Adapter, ersetzbar.
- Nicht in dieser Story: Jahresabschluss (read-only, Immutability-Policy) und Dossier-Link mit Code per E-Mail. Beide sind eigene Stories, der Link braucht zuerst den E-Mail-Versand.

## Entscheide

- **Ein ZIP**, z.B. `Flowtr-Export_2026-01-01_2026-12-31_erstellt-2026-11-01-1432.zip`:
  - `Journal.xlsx`: Blatt «Journal» (eine Zeile pro Buchung) und Blatt «Kategorien» (Summen pro Kategorie und Art).
  - `Journal.pdf`: dasselbe zum Lesen und Ablegen, A4 quer, mit Summen.
  - `Belege/Ausgaben/…` und `Belege/Einnahmen/…`: alle Dateien unverändert, mit ihrem Namen aus der Ablage (z.B. `A-2026-001_claude-pro.pdf`, `A-2026-001_claude-pro_2.pdf`).
  - Eine Mail als Beleg kommt zweimal: das Original (`.eml`) und ein PDF zum Lesen (gleicher Name, `.pdf`).
  - `Belegverzeichnis.csv`: jede Datei mit Belegnummer, Datum, Pfad im ZIP, Originalname, Grösse und SHA-256 (Prüfspur nach GeBüV).
- **Formale Angaben** (Entscheid 01.11.2026): Im PDF und im Excel (Blatt «Info») stehen der Name des Geschäfts, der Zeitraum, «Erstellt am … von …» und der Status: «Provisorisch: 2026 ist nicht abgeschlossen» oder «Abgeschlossen am …». Buchungen ohne Beleg sind markiert («ohne Beleg») und gezählt. Den Namen des Geschäfts pflegen Admins unter «Verwaltung → Geschäft» (Variante A).
- **Spalten des Journals:** Datum, Belegnummer, Art, Kategorie, Beschreibung, Gegenpartei, Zahlungsart, Einnahmen CHF, Ausgaben CHF, Originalbetrag, Währung, Notiz, Anzahl Dateien. Im Excel echte Zahlen und Daten, damit der Treuhänder rechnen kann.
- **Zeitraum:** frei wählbar, Standard das ganze Jahr des Journals. «Belege mitnehmen» lässt sich abwählen (nur Journal).
- **Bibliotheken:** ClosedXML (MIT) für Excel. QuestPDF (Community-Lizenz: Einzelpersonen und Firmen unter USD 1 Mio. Umsatz) für PDF, hinter der Schnittstelle `IPdfDocuments`.
- **Rollen:** wer das Journal sieht (Admin, Contributor).
- **Erzeugen:** auf dem Server in eine temporäre Datei, dann als Download. Kein Speicher im Blob Storage, keine Kopie bleibt liegen.

## Umsetzung

- `Features/Export`: Abfrage `ExportJournal` (Zeitraum, mit Belegen) baut das ZIP in einer temporären Datei, der Download löscht sie beim Schliessen. Endpoint `GET /api/export?from=…&to=…&receipts=…`.
- `JournalWorkbook` (ClosedXML): Blatt «Journal» mit echten Daten und Zahlen, Summen als Formeln, Autofilter, Blatt «Kategorien».
- `IPdfDocuments` mit `QuestPdfDocuments`: Journal (A4 quer, Summen, Kategorien, Seitenzahl) und Mail als PDF.
- Fehlt eine Datei in der Ablage, steht sie in `Fehlende Belege.txt` im ZIP. Der Export läuft trotzdem.
- `SixLabors.Fonts` (Abhängigkeit von ClosedXML) auf 1.0.1 gepinnt: Version 1 ist Apache-2.0.
- Client: «Exportieren» im Journal, Dialog mit Zeitraum (Standard: Jahr des Journals) und «Belege mitnehmen».
- **Geschäft:** `BusinessSettings` (eine Zeile, im Audit-Log), `GetBusiness`, `SetBusiness`, `GET/PUT /api/business` (Ändern nur Admins), Seite `/geschaeft`. Migration `AddBusiness`.
- **Formale Angaben:** `ExportReport` mit Geschäft, Zeitpunkt (Schweizer Zeit), Ersteller, Status aus `ClosedYears` und Belegverzeichnis. Excel: Blätter «Info», «Journal», «Kategorien», «Belege». CSV mit BOM und Semikolon.
- **Geprüft (Nachtrag):** 211 Tests (Info, Status provisorisch und abgeschlossen, Belegverzeichnis mit SHA-256, Geschäft). PDF mit Kopf, Status und Markierung angesehen. Seite «Geschäft» im Browser.
- **Geprüft:** 205 Tests (8 neue mit echtem QuestPDF und ClosedXML). Im Image (`noble-chiseled-extra`) im Modus Production mit Login über die API: ZIP mit Excel und PDF. Das Basis-Image hat kein `libz`, `libQuestPdfSkia` braucht sie nicht. Dialog im Browser, ZIP per `fetch`.

## Akzeptanzkriterien

- [x] Im Journal gibt es «Exportieren». Der User wählt Zeitraum und «Belege mitnehmen» und lädt ein ZIP herunter.
- [x] Das ZIP enthält `Journal.xlsx`, `Journal.pdf` und die Belege in Ordnern nach Art.
- [x] Das Excel hat echte Zahlen und Daten, Summen der Einnahmen, Ausgaben und des Gewinns, und ein Blatt mit den Summen pro Kategorie.
- [x] PDF und Excel nennen Geschäft, Zeitraum, Erstellungszeitpunkt, Ersteller und Status des Jahres. Buchungen ohne Beleg sind markiert.
- [x] Das Belegverzeichnis nennt den SHA-256 jeder Datei.
- [x] Ein Beleg mit mehreren Dateien kommt mit allen Dateien. Eine Mail als Beleg kommt als Original und als PDF.
- [x] Ein Export ohne Buchungen im Zeitraum gibt ein ZIP mit leerem Journal, keinen Fehler.
- [x] Der Export läuft auch im Container in Azure (Schriften und native Bibliotheken von QuestPDF).

## Aufgaben

- [x] Abfrage `ExportJournal` (Zeitraum, mit Belegen), Endpoint `GET /api/export`
- [x] Excel mit ClosedXML
- [x] Schnittstelle `IPdfDocuments` mit QuestPDF: Journal und Mail
- [x] Dialog «Exportieren» im Journal
- [x] Tests (Inhalt des ZIP, Excel, Zeitraum, mehrere Dateien, Mail)
- [x] Image lokal prüfen (QuestPDF im Container)
- [x] Specs: [Journal](../../journal.md), ADR-009 (Nachtrag), README

## Definition of Done

- [x] Applikation buildet, automatische Tests laufen grün
- [x] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [x] `README.md` und `/specs` aktualisiert, falls nötig
- [x] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [x] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [x] Story aus `/specs/backlog.md` entfernt
