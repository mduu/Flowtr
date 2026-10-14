# Story: #008, Beleg im Buchungsdialog hochladen und ablegen

**Status:** Done (14.10.2026)

## Beschreibung

Der Beleg steht **zuoberst im Buchungsdialog**: Der User lädt ihn zuerst hoch (PDF, Bild oder Foto), sieht ihn als Vorschau und füllt darunter die Buchung aus. Beim Speichern nummeriert Flowtr den Beleg, benennt ihn um und legt ihn ab. Im Journal öffnet der User den Beleg jeder Buchung. In dieser Story füllt der User die Felder noch von Hand aus. Das automatische Ausfüllen folgt mit [#009](009-ki-extraktion.md).

## Hintergrund

- Ablage, Container und Namensschema: [ADR-001](../../adrs/001-belegablage-in-blob-storage.md). Bestätigte Belege in `belege/{jahr}/einnahmen|ausgaben/E|A-{jahr}-{nr}_{bezeichnung}.{ext}`, unbestätigte Uploads in `eingang/`.
- [Konzept](../../concept.md#ablage-der-belege): Belege 1:1 als Datei, Einnahmen und Ausgaben getrennt, der Dateiname ordnet den Beleg dem Journal zu.
- **Reihenfolge:** zuerst der Blob nach `belege/`, dann die Buchung ([ADR-001](../../adrs/001-belegablage-in-blob-storage.md), Konsequenzen). Flowtr muss verwaiste Blobs erkennen (z.B. Prüf-Job mit Meldung).
- **Nummern:** fortlaufend pro Jahr und Art, ohne Lücken und ohne Doppel, auch bei gleichzeitigen Uploads (z.B. Sequenz-Tabelle mit Sperre). Das braucht einen Entscheid (ADR).
- **Upload:** Fortschritt anzeigen ([UX](../../ux.md)). Erlaubt: PDF, JPEG, PNG, HEIC (iPhone). Grössenlimite festlegen (z.B. 20 MB). Der Server prüft den Inhalt, nicht nur die Endung.
- **Download:** als Stream über die API, nur mit Rolle. Keine öffentlichen Blob-URLs.
- **Abschluss:** Belege abgeschlossener Jahre sind read-only. Die Immutability-Policy setzt erst der Jahresabschluss (eigene Story).
- **Ablauf im Dialog** (Wunsch des Users, 14.10.2026):
  1. «Neue Buchung» öffnet den Dialog. Zuoberst ist der Beleg-Bereich: Datei wählen, Datei hineinziehen oder (iPhone) Foto aufnehmen.
  2. Nach dem Upload liegt der Beleg in `eingang/`. Der Dialog zeigt eine kompakte Karte mit Vorschau (Bild bzw. erste PDF-Seite), Dateiname und Grösse, dazu «Ansehen» (gross) und «Entfernen».
  3. Darunter füllt der User die Felder aus (in #009 vorausgefüllt).
  4. «Speichern»: Flowtr vergibt die Belegnummer, verschiebt den Beleg nach `belege/` und speichert die Buchung.
  5. Bricht der User ab, bleibt der Upload in `eingang/` und verfällt nach 30 Tagen (Lifecycle-Regel).
- **Beide Wege bleiben:** Eine Buchung ohne Beleg ist weiterhin möglich (z.B. Barspesen, Beleg folgt). Ein Beleg lässt sich später anhängen oder ersetzen. Alle Felder bleiben immer von Hand bearbeitbar.
- **Beleg zuerst:** Ein Beleg lässt sich auch direkt aufs Journal ziehen. Das öffnet den Dialog mit dem Beleg schon oben. Mehrere Belege auf einmal braucht es nicht.
- Basis für die KI-Extraktion ([#009](009-ki-extraktion.md)): Dieselbe Karte zeigt dort zusätzlich «Beleg wird gelesen …» und füllt danach die Felder.
- Audit: Hochladen, Ersetzen und Entfernen eines Belegs gehen ins Audit-Log ([#006](006-audit-log.md)).

## Umsetzung

- **Ablage** hinter `IReceiptStore` (Blob Storage, lokal Azurite, in Tests im Speicher). Container `eingang` und `belege` legt der tägliche Job an, falls sie fehlen.
- **Upload** prüft den Inhalt (PDF, JPEG, PNG, HEIC, WebP an den ersten Bytes), nicht Endung oder Browser-Typ. Höchstens 20 MB. SHA-256 des Inhalts wird gespeichert.
- **Belegnummern** per Sequenz-Tabelle mit optimistischer Sperre ([ADR-007](../../adrs/007-belegnummern-per-sequenz.md)). Ein Beleg-Blob überschreibt nie einen anderen. Gefunden beim Testen: Ohne diese Bedingung hätte ein paralleles Speichern die Datei eines anderen Belegs überschrieben.
- **Ersetzen** behält die Nummer. **Entfernen** und **Löschen der Buchung** löschen Beleg und Datei: Die Nummer bleibt eine Lücke, das Audit-Log erklärt sie. Jahr und Art einer Buchung mit Beleg sind fix (Teil der Nummer).
- **Dialog:** Beleg-Bereich zuoberst. Datei wählen oder hineinziehen, auf dem iPhone «Foto aufnehmen». Karte mit Vorschau (Bild) bzw. PDF-Symbol, Ansehen (neuer Tab), Ersetzen, Entfernen.
- **Abweichungen:**
  - Upload-Fortschritt als Spinner und laufender Balken, nicht in Prozent: Der Browser meldet bei `fetch` keinen Upload-Fortschritt. Belege sind meist klein.
  - PDF-Vorschau als Symbol, Ansehen öffnet das PDF im Browser. Ein Vorschaubild der ersten Seite (pdf.js) lohnt sich noch nicht.
  - HEIC zeigt nur ein Symbol: Ausser Safari kann kein Browser HEIC anzeigen.
- **Journal:** Belegnummer als Link (öffnet den Beleg), auf dem iPhone ein Büroklammer-Symbol. Eine Datei aufs Journal ziehen öffnet eine neue Buchung mit diesem Beleg.
- **Abruf** über `/api/receipts/{id}` (Rolle nötig), inline mit `Cache-Control: private, no-store`, Dateiname = Belegnummer.
- **Täglicher Job:** löscht Uploads älter als 30 Tage, meldet Blobs ohne Beleg und Belege ohne Blob im Log.
- **Pakete:** ASP.NET Core und EF Core auf 10.0.13 (Sicherheitsupdates), `System.Security.Cryptography.Pkcs` zentral gepinnt (Transitive Pinning).
- Geprüft in Chrome und WebKit: Upload, falscher Typ, Speichern mit Nummer, Abruf, Ersetzen, Entfernen, Ablegen aufs Journal, iPhone-Breite.

## Akzeptanzkriterien

- [x] Der Beleg-Bereich steht zuoberst im Buchungsdialog. Der User lädt per Dateiauswahl, Ziehen und Ablegen oder Kamera hoch und sieht den Fortschritt.
- [x] Nach dem Upload zeigt der Dialog eine Vorschau des Belegs. «Ansehen» zeigt ihn gross, «Entfernen» löst ihn wieder von der Buchung.
- [x] Eine Buchung lässt sich weiterhin ohne Beleg speichern. Ein Beleg lässt sich später anhängen oder ersetzen.
- [x] Ein Beleg, aufs Journal gezogen, öffnet den Dialog mit diesem Beleg.
- [x] Flowtr vergibt die nächste Belegnummer (`E-2026-001` bzw. `A-2026-001`), benennt die Datei nach Schema um und legt sie in `belege/` ab.
- [x] Gleichzeitige Uploads erhalten nie dieselbe Nummer. Nummern haben keine Lücken.
- [x] Im Journal öffnet ein Klick den Beleg (PDF/Bild im Browser), nur für angemeldete User mit Rolle.
- [x] Ungültige Dateien (Typ, Grösse, Inhalt) lehnt Flowtr mit einer deutschen Meldung ab.
- [x] Ein Beleg ohne Buchung (verwaist) wird erkannt und gemeldet.
- [x] Belege abgeschlossener Jahre lassen sich nicht ersetzen oder entfernen.
- [x] Auf dem iPhone lässt sich ein Foto direkt mit der Kamera aufnehmen und hochladen.

## Aufgaben

- [x] ADR: Nummernvergabe ohne Lücken bei Parallelität
- [x] Blob-Zugriff (Aspire `blobs`, Container anlegen, lokal Azurite)
- [x] Commands: Beleg hochladen/zuordnen, ersetzen, entfernen. Query: Beleg herunterladen
- [x] API mit Upload (Streaming, Limite) und Download
- [x] UI: Beleg-Bereich zuoberst im Buchungsdialog (Upload mit Fortschritt, Vorschau, Ansehen, Entfernen), Drop-Zone auf dem Journal, Beleg-Link im Journal
- [x] Vorschau: Bild direkt, PDF erste Seite (im Browser, z.B. pdf.js oder Vorschaubild vom Server). Entscheid in der Umsetzung
- [x] Prüf-Job für verwaiste Blobs
- [x] Tests: Nummernvergabe (parallel), Namensschema, Dateiprüfung, Rollen, Sperre abgeschlossener Jahre
- [x] `concept.md` und ADR-001 bei Bedarf ergänzen

## Definition of Done

- [x] Applikation buildet, automatische Tests laufen grün
- [x] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [x] `README.md` und `/specs` aktualisiert, falls nötig
- [x] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [x] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [x] Story aus `/specs/backlog.md` entfernt
