# Story: #009, KI-Extraktion aus dem Beleg

**Status:** Open

## Beschreibung

Der User lädt nur den Beleg hoch. Flowtr liest ihn mit KI (Claude über die Anthropic API) und schlägt die Buchung vor: Datum, Art, Betrag, Währung, Gegenpartei, Kategorie, Beschreibung. Der User bestätigt oder korrigiert. Erst dann legt Flowtr Beleg und Buchung ab.

## Hintergrund

- Ablauf: [Konzept](../concept.md#ablauf). Upload → Analyse → Vorschlag → Bestätigen → nummerierte Ablage und Buchung.
- Voraussetzungen: Journal ([#007](007-journal-manuell-erfassen.md)), Upload und Ablage ([#008](008-beleg-upload-und-ablage.md)), Audit-Log ([#006](done/006-audit-log.md)).
- **Eingang:** Der Upload landet zuerst in `eingang/` ([ADR-001](../adrs/001-belegablage-in-blob-storage.md)). Erst beim Bestätigen kommt er nummeriert nach `belege/`. Nicht bestätigte Belege löscht eine Lifecycle-Regel nach 30 Tagen.
- **KI:** Claude über die Anthropic API, aktuelles Modell. PDF und Bild direkt als Dokument senden. Strukturierte Antwort (JSON-Schema bzw. Tool-Use), damit Flowtr die Felder sicher übernimmt.
- **Vorschlag für Kategorie und Gegenpartei:** aus dem Beleg und aus früheren Buchungen derselben Gegenpartei (z.B. Azure → Software/Abos). Die Liste bestehender Kategorien und Gegenparteien geht als Kontext mit.
- **Währung:** Betrag und Währung wie auf dem Beleg. Für Fremdwährungen schlägt Flowtr den CHF-Betrag per Tageskurs vor (Quelle festlegen, z.B. EZB oder SNB). Der User kann ihn korrigieren (z.B. gemäss Kreditkartenabrechnung).
- **Datenschutz und Sicherheit:** Belege enthalten Geschäftsdaten. API-Key nur in der Konfiguration bzw. im Key Vault, nie im Code. Zu klären und als ADR festhalten: Anbieter, Datenverarbeitung, Aufbewahrung beim Anbieter. Prompt-Injection: Text auf dem Beleg ist Daten, keine Anweisung. Die Antwort wird validiert, nie ausgeführt.
- **Kosten:** pro Beleg loggen (Tokens). Optional eine Monatslimite.
- **Fehler:** Wenn die KI nichts erkennt oder nicht erreichbar ist, kann der User die Buchung von Hand erfassen. Der Beleg geht nicht verloren.
- **Mehrere Belege:** Der User kann mehrere Dateien auf einmal hochladen. Jede wird einzeln analysiert und bestätigt (Eingangsliste).
- Der E-Mail-Eingang (IMAP) ist eine eigene, spätere Story. Er nutzt dieselbe Analyse.

## Akzeptanzkriterien

- [ ] Der User lädt einen oder mehrere Belege hoch und sieht pro Beleg den Fortschritt der Analyse (Spinner).
- [ ] Flowtr zeigt den Vorschlag neben dem Beleg an (Desktop nebeneinander, iPhone untereinander).
- [ ] Datum, Betrag, Währung, Art und Gegenpartei stimmen bei typischen Belegen (Azure, Anthropic, Stripe, Firebase, Hardware-Kauf). Für diese Beispiele gibt es Testdateien.
- [ ] Kategorie und Gegenpartei berücksichtigen frühere Buchungen derselben Gegenpartei.
- [ ] Bei Fremdwährung schlägt Flowtr den CHF-Betrag per Tageskurs vor.
- [ ] Erst beim Bestätigen vergibt Flowtr die Nummer, legt den Beleg ab und erstellt die Buchung.
- [ ] Unbestätigte Belege bleiben in einer Eingangsliste, bis der User sie bestätigt oder verwirft.
- [ ] Wenn die KI ausfällt, kann der User die Buchung von Hand erfassen.
- [ ] Der API-Key steht nicht im Repository.

## Aufgaben

- [ ] ADR: KI-Anbieter, Datenschutz, Prompt-Injection-Schutz, Wechselkursquelle
- [ ] Eingang: Upload nach `eingang/`, Status pro Beleg (neu, analysiert, bestätigt, verworfen)
- [ ] Analyse: Anthropic-Client, Prompt mit Schema, Kontext aus Kategorien und Gegenparteien, Validierung der Antwort
- [ ] Wechselkurs-Dienst mit Cache
- [ ] Commands: Beleg analysieren, Vorschlag bestätigen (nummerieren, ablegen, buchen), verwerfen
- [ ] UI: Eingangsliste, Prüf-Ansicht mit Beleg und Formular
- [ ] Tests: Antwort-Validierung, Zuordnung aus früheren Buchungen, Bestätigen (Reihenfolge Blob → Buchung), Ausfall der KI. Die KI selbst über eine Schnittstelle mocken
- [ ] Konfiguration und `README.md` (API-Key lokal per User Secrets)

## Definition of Done

- [ ] Applikation buildet, automatische Tests laufen grün
- [ ] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [ ] `README.md` und `/specs` aktualisiert, falls nötig
- [ ] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [ ] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [ ] Story aus `/specs/backlog.md` entfernt
