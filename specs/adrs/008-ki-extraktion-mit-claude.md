# ADR-008: KI-Extraktion mit Claude, Kurse der EZB

## Status

**Akzeptiert** (14.10.2026)

## Kontext

Nach dem Upload eines Belegs soll Flowtr die Buchung vorschlagen: Datum, Art, Betrag, Währung, Gegenpartei, Kategorie, Beschreibung, Zahlungsart ([Story #009](../stories/done/009-ki-extraktion.md)). Belege sind PDFs, Fotos und Scans, oft in Fremdwährung (USD, EUR).

- Belege enthalten Geschäftsdaten: Der Anbieter muss sie vertraulich behandeln.
- Text auf dem Beleg darf das Verhalten von Flowtr nicht steuern (Prompt-Injection).
- Für Fremdwährungen braucht es einen Tageskurs zu CHF.
- Die KI darf ausfallen: Der User kann immer von Hand erfassen.

## Entscheidung

- **Anbieter:** Claude über die **Anthropic Messages API**, Modell konfigurierbar (`Anthropic:Model`, Standard ein aktuelles Sonnet-Modell). PDF und Bild gehen direkt als Dokument bzw. Bild mit, ohne eigene Texterkennung.
- **Feste Struktur:** Das Modell antwortet mit dem Tool `record_booking`, ein JSON-Schema statt freiem Text. Aktuelle Modelle nehmen kein erzwungenes Tool (`tool_choice` `tool`/`any`): Flowtr bietet nur dieses eine Tool an (`auto`), und der System-Prompt verlangt es. Eine Antwort ohne Tool gilt als «nicht gelesen».
- **Prüfung in Flowtr:** Flowtr übernimmt nur gültige Werte: Datum im erlaubten Bereich, Betrag > 0, Währung mit drei Buchstaben, Kategorie aus der Liste und von der richtigen Art, nur bekannte Zahlungsarten. Frühere Buchungen derselben Gegenpartei bestimmen Kategorie und Zahlungsart.
- **Prompt-Injection:** Der System-Prompt sagt, dass der Beleg nur Daten ist. Die Antwort füllt nur ein Formular, das der User prüft und speichert. Flowtr führt nichts aus der Antwort aus.
- **Nur leere Felder:** Der Dialog überschreibt keine Eingaben des Users. Vorschläge sind als «Von der KI erkannt» markiert, bis der User sie ändert.
- **Kurse:** Referenzkurse der Europäischen Zentralbank über den freien Dienst **Frankfurter** (`api.frankfurter.app`). Kurs des Belegdatums bzw. des letzten Geschäftstags davor. Ohne Kurs bleibt der CHF-Betrag leer.
- **Schlüssel:** `Anthropic:ApiKey` nur in User Secrets (lokal) bzw. Key Vault (Betrieb). Ohne Schlüssel zeigt Flowtr einen Hinweis, und der User erfasst von Hand.
- **Kosten:** Flowtr loggt pro Beleg die Tokens.

## Optionen

### Option A: Claude mit Tool-Schema (gewählt)

**Vorteile:**
- Liest PDFs und Fotos direkt, auch schlechte Scans und verschiedene Sprachen
- Strukturierte Antwort, leicht zu prüfen
- Kategorie und Gegenpartei aus dem Kontext (Listen, frühere Buchungen)

**Nachteile:**
- Externer Dienst, Kosten pro Beleg
- Belege verlassen Flowtr (siehe Datenschutz)

### Option B: Azure AI Document Intelligence (Rechnungsmodell)

**Vorteile:**
- Auf Rechnungen spezialisiert, Daten bleiben in Azure

**Nachteile:**
- Liefert Felder, aber keine Zuordnung zu Kategorie und Gegenpartei. Dafür bräuchte es zusätzlich ein Sprachmodell
- Schwächer bei Quittungen, Fotos und App-Store-Abrechnungen

### Option C: Eigene Texterkennung und Regeln

**Nachteile:**
- Viel Aufwand, bricht bei jedem neuen Belegformat

## Konsequenzen

### Positive Auswirkungen

- Ein Beleg genügt für einen Buchungsvorschlag, auch in Fremdwährung.
- Die Analyse ist ein eigener Use Case (`AnalyzeUpload`). Der E-Mail-Eingang ([#010](../stories/010-email-eingang-imap.md)) nutzt ihn wieder.

### Negative Auswirkungen / Risiken

- **Datenschutz:** Belege gehen zu Anthropic. Die API speichert Eingaben nach eigener Policy nur begrenzt und nutzt sie nicht für Training. Vor dem Betrieb prüfen und in der Datenschutzerklärung nennen.
- **Bilder:** Die API nimmt Bilder bis 5 MB und kein HEIC. Für solche Fotos erfasst der User von Hand (später: verkleinern bzw. umwandeln auf dem Server).
- **Frankfurter** ist ein freier Dienst ohne Garantie. Fällt er aus, trägt der User den CHF-Betrag selbst ein.
- Die Qualität hängt vom Modell ab: Bei einem Modellwechsel die Testbelege wieder prüfen.

### Aufwand

- Mittel: Reader, Prüfung, Kurse, Dialog.

## Referenzen

- [Anthropic Messages API: Tool use](https://docs.anthropic.com/en/docs/build-with-claude/tool-use)
- [Anthropic: PDF support](https://docs.anthropic.com/en/docs/build-with-claude/pdf-support)
- [Frankfurter: ECB reference rates](https://frankfurter.dev)
