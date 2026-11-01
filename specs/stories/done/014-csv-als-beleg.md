# Story: #014, CSV als Beleg

**Status:** Done (01.11.2026)

## Beschreibung

*Flowtr* nimmt CSV-Dateien als Beleg an, im Buchungsdialog und als Anhang einer Mail. Die KI liest sie wie ein PDF. So kann der User Abrechnungen ablegen, die ein Anbieter nur als Datei liefert (z.B. App Store Connect), ohne Screenshot.

## Hintergrund

- Heute nimmt *Flowtr* nur PDF und Bilder an (`ReceiptFiles.Detect`). Viele Abrechnungen gibt es als CSV oder TSV:
  - **App Store Connect:** «Payments and Financial Reports» bietet pro Monat einen Download an. Heute macht der User einen Screenshot der Seite.
  - **Stripe:** Die Berichte (z.B. Saldenübersicht) lassen sich als CSV oder PDF exportieren. PDF geht schon heute.
- Eine CSV-Datei ist der Originalbeleg, so wie der Anbieter ihn liefert. *Flowtr* legt sie unverändert ab (GeBüV, [ADR-001](../../adrs/001-belegablage-in-blob-storage.md)).
- Die KI liest heute schon Text: Eine Mail (`.eml`) geht als Text an Claude ([ADR-008](../../adrs/008-ki-extraktion-mit-claude.md)).
- Automatisch abholen ist eine eigene Story ([#015](../015-stripe-abrechnung-automatisch.md), [#016](../016-apple-abrechnung-automatisch.md)). Diese Story ist der kleine Schritt davor: Der User lädt die Datei selbst herunter.

## Entscheide

- **Typen:** CSV und TSV (`.csv`, `.tsv`, `.txt` mit Tabulatoren oder Semikolons). Kein Excel (`.xlsx`): Die KI kann es nicht direkt lesen, und die Anbieter liefern auch CSV.
- **Erkennen:** Eine CSV-Datei hat keine Signatur in den ersten Bytes. *Flowtr* prüft deshalb den Inhalt: gültiges UTF-8 (mit oder ohne BOM) oder UTF-16 mit BOM, keine Steuerzeichen ausser Tabulator und Zeilenumbruch, mindestens zwei Zeilen mit demselben Trennzeichen. Sonst lehnt *Flowtr* die Datei ab wie heute.
- **Ablage:** unverändert, Endung `.csv` (auch für TSV), Typ `text/csv`.
- **Ansehen:** *Flowtr* zeigt die Datei als Tabelle (HTML, bereinigt, wie die Ansicht einer Mail). «Original» lädt die Datei herunter. Die Ansicht interpretiert nichts: keine Formeln, keine Links.
- **KI:** Die Datei geht als Text an Claude, höchstens die ersten 200 KB. Der Prompt sagt: Eine Tabelle kann eine Abrechnung über einen Zeitraum sein. Dann sind Datum das Ende des Zeitraums und Betrag die Summe der Abrechnung (z.B. «Proceeds» bei Apple).
- **Mail:** Ein Anhang als CSV ist eine Datei des Belegs, wie ein PDF ([ADR-009](../../adrs/009-email-eingang-mail-als-beleg.md)).
- **Export:** Die CSV-Datei kommt unverändert ins ZIP. Kein zusätzliches PDF: Der Treuhänder kann sie direkt öffnen.

## Umsetzung

- **Erkennen:** `ReceiptFiles.Detect` prüft den ganzen Inhalt. `TextTable` liest UTF-8 (mit oder ohne BOM) und UTF-16 mit BOM, lehnt Steuerzeichen und Markup ab und verlangt zwei Zeilen mit mindestens zwei Zellen. Das Trennzeichen ist das häufigste der ersten Zeile (`;`, `,`, Tabulator, `|`). Zellen in Anführungszeichen dürfen Trennzeichen und Zeilenumbrüche enthalten.
- **Typ:** `ReceiptContentTypes.Table` (`text/csv`), Endung `.csv`.
- **KI:** `AnalyzeUploads` schickt die Tabelle als Text-Dokument mit dem Titel «Table (CSV)», höchstens 200'000 Zeichen. Der System-Prompt erklärt Abrechnungen über einen Zeitraum.
- **Ansicht:** `TableView`, eigene CSP mit `sandbox allow-downloads`. Höchstens 5000 Zeilen, Zahlen rechtsbündig.
- **Mail:** `MailReceipts` übernimmt `text/*`-Teile, wenn sie Anhang sind.
- **Client:** `ReceiptFileTypes` mit `Accept`, Symbol und Farbe für Dialog, Journal und Eingang.
- **Geprüft:** 230 Tests (19 neue). Im Browser lokal mit einer Abrechnung im Format von App Store Connect: Upload, Vorschlag der KI (Einnahme 7.35, 30.09.2026, Umsatz und Honorare, Apple), Ansicht als Tabelle, Dialog in Handy-Breite. In Produktion vom User mit der echten Abrechnung von App Store Connect geprüft (01.11.2026): Vorschlag korrekt.

## Akzeptanzkriterien

- [x] Eine CSV-Datei lässt sich im Buchungsdialog hochladen (Klick und Ziehen), allein oder zusammen mit einem PDF.
- [x] Die Abrechnung von App Store Connect für einen Monat gibt einen Vorschlag der KI mit Datum (Monatsende) und Betrag in CHF.
- [x] Eine Mail mit CSV-Anhang gibt einen Eintrag im Eingang mit dieser Datei.
- [x] Die Datei liegt unverändert in der Ablage (gleicher SHA-256) und kommt unverändert in den Export.
- [x] «Ansehen» zeigt die Datei als Tabelle, «Original» lädt sie herunter.
- [x] Eine Binärdatei mit der Endung `.csv` lehnt *Flowtr* ab.

## Aufgaben

- [x] `ReceiptFiles.Detect`: Text erkennen (Encoding, Trennzeichen), Typ `text/csv`
- [x] KI: CSV als Text senden, Grenze 200 KB, Prompt für Abrechnungen über einen Zeitraum
- [x] Ansicht als Tabelle (bereinigt, eigene CSP wie bei der Mail)
- [x] Client: `Accept` um `.csv,.tsv,.txt,text/csv` erweitern, Symbol für Tabellen
- [x] Mail: CSV-Anhänge übernehmen
- [x] Tests: Erkennen (UTF-8, UTF-16, Binärdatei), KI-Anfrage, Ansicht, Mail, Export
- [x] Mit einer echten Abrechnung von App Store Connect prüfen (Betrag, Datum)
- [x] Specs: [Konzept](../../concept.md), ADR-001 und ADR-009 (Nachtrag), [Security](../../security.md) (Ansicht ohne aktive Inhalte)

## Definition of Done

- [x] Applikation buildet, automatische Tests laufen grün
- [x] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [x] `README.md` und `/specs` aktualisiert, falls nötig
- [x] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [x] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [x] Story aus `/specs/backlog.md` entfernt
