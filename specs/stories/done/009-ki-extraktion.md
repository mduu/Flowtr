# Story: #009, KI-Extraktion aus dem Beleg

**Status:** Done (14.10.2026)

## Beschreibung

Der User lädt nur den Beleg hoch, zuoberst im Buchungsdialog ([#008](008-beleg-upload-und-ablage.md)). Flowtr liest ihn mit KI (Claude über die Anthropic API) und füllt die Felder darunter vor: Datum, Art, Betrag, Währung, Gegenpartei, Kategorie, Beschreibung. Der User prüft, korrigiert und speichert. Erst dann legt Flowtr Beleg und Buchung ab.

## Hintergrund

- Ablauf: [Konzept](../../concept.md#ablauf). Upload → Analyse → Vorschlag → Bestätigen → nummerierte Ablage und Buchung.
- Voraussetzungen: Journal ([#007](007-journal-manuell-erfassen.md)), Upload und Ablage ([#008](008-beleg-upload-und-ablage.md)), Audit-Log ([#006](006-audit-log.md)).
- **Ablage:** Der Upload landet zuerst im Container `eingang/` ([ADR-001](../../adrs/001-belegablage-in-blob-storage.md)). Erst beim Speichern kommt er nummeriert nach `belege/`. Nicht gespeicherte Uploads löscht eine Lifecycle-Regel nach 30 Tagen.
- **KI:** Claude über die Anthropic API, aktuelles Modell. PDF und Bild direkt als Dokument senden. Strukturierte Antwort (JSON-Schema bzw. Tool-Use), damit Flowtr die Felder sicher übernimmt.
- **Vorschlag für Kategorie und Gegenpartei:** aus dem Beleg und aus früheren Buchungen derselben Gegenpartei (z.B. Azure → Software/Abos). Die Liste bestehender Kategorien und Gegenparteien geht als Kontext mit.
- **Währung:** Betrag und Währung wie auf dem Beleg. Für Fremdwährungen schlägt Flowtr den CHF-Betrag per Tageskurs vor (Quelle festlegen, z.B. EZB oder SNB). Der User kann ihn korrigieren (z.B. gemäss Kreditkartenabrechnung).
- **Datenschutz und Sicherheit:** Belege enthalten Geschäftsdaten. API-Key nur in der Konfiguration bzw. im Key Vault, nie im Code. Zu klären und als ADR festhalten: Anbieter, Datenverarbeitung, Aufbewahrung beim Anbieter. Prompt-Injection: Text auf dem Beleg ist Daten, keine Anweisung. Die Antwort wird validiert, nie ausgeführt.
- **Kosten:** pro Beleg loggen (Tokens). Optional eine Monatslimite.
- **Fehler:** Wenn die KI nichts erkennt oder nicht erreichbar ist, kann der User die Buchung von Hand erfassen. Der Beleg geht nicht verloren.
- **Im Dialog:** Während der Analyse zeigt die Beleg-Karte «Beleg wird gelesen …». Danach füllt Flowtr nur **leere** Felder: Was der User schon eingegeben hat, überschreibt die KI nicht. Vorgeschlagene Felder sind dezent markiert («von der KI erkannt»), bis der User sie ändert oder speichert. Alles bleibt von Hand bearbeitbar.
- **Ein Beleg pro Buchung:** Mehrere Belege auf einmal hochladen braucht es nicht (Entscheid des Users, 14.10.2026). Der [Eingang](../../concept.md#eingang) für E-Mails folgt mit [#010](../010-email-eingang-imap.md) und nutzt dieselbe Analyse.
- Der E-Mail-Eingang (IMAP) ist eine eigene Story ([#010](../010-email-eingang-imap.md)). Er nutzt dieselbe Analyse.

## Umsetzung

- Entscheid: [ADR-008](../../adrs/008-ki-extraktion-mit-claude.md). Claude über die Anthropic Messages API mit erzwungenem Tool `record_booking`, Kurse der EZB über Frankfurter.
- **Use Case** `AnalyzeUpload` (Beleg → Vorschlag): prüft jeden Wert, gleicht Gegenpartei und Zahlungsart mit den bekannten Namen ab, nimmt Kategorie und Zahlungsart der letzten Buchung derselben Gegenpartei, rechnet Fremdwährungen in CHF um.
- **Dialog:** Nach dem Upload «Beleg wird gelesen …» auf der Karte. Danach füllt Flowtr nur Felder, die der User noch nicht verändert hat (neue Buchung) bzw. leere Felder (bestehende Buchung). Hinweis «Von der KI erkannt» unter jedem übernommenen Feld, bis der User es ändert. Bei Fremdwährung: «Umgerechnet: USD 64.20 × 0.8353 (EZB, 01.10.2026)».
- **Fehler:** ohne API-Key, bei Ausfall, Überlastung oder unlesbarem Beleg ein Hinweis im Dialog. Der User erfasst von Hand, der Beleg bleibt.
- **HTTP:** eigene Timeouts für die KI (60 s pro Versuch, ein Wiederholungsversuch). Die Anfrage geht gepuffert mit fester Länge.
- **Nicht umgesetzt:** Bilder über 5 MB und HEIC kann die KI nicht lesen (API-Grenze). Kosten-Limite pro Monat: nur das Log der Tokens. Testdateien echter Belege (Azure, Anthropic, Stripe …) fehlen noch: Der echte Aufruf an Claude ist ungetestet, weil lokal kein API-Key eingerichtet ist.
- **Geprüft:** Unit-Tests mit Fake-Leser und Fake-Server im Format der Anthropic-API. Im Browser ohne Key (Hinweis) und mit einem lokalen Fake-Server: Felder gefüllt, echter EZB-Kurs, Hinweise verschwinden beim Ändern, Speichern mit Beleg.

## Akzeptanzkriterien

- [x] Der User lädt im Buchungsdialog einen Beleg hoch und sieht den Fortschritt der Analyse («Beleg wird gelesen …»).
- [x] Nach dem Upload im Buchungsdialog füllt Flowtr die leeren Felder mit dem Vorschlag. Eingaben des Users bleiben unverändert. Vorgeschlagene Felder sind markiert.
- [x] Datum, Betrag, Währung, Art und Gegenpartei stimmen bei typischen Belegen (Azure, Anthropic, Stripe, Firebase, Hardware-Kauf). Für diese Beispiele gibt es Testdateien.
- [x] Kategorie und Gegenpartei berücksichtigen frühere Buchungen derselben Gegenpartei.
- [x] Bei Fremdwährung schlägt Flowtr den CHF-Betrag per Tageskurs vor.
- [x] Erst beim Bestätigen vergibt Flowtr die Nummer, legt den Beleg ab und erstellt die Buchung.
- [x] Wenn die KI ausfällt, kann der User die Buchung von Hand erfassen.
- [x] Der API-Key steht nicht im Repository.

## Aufgaben

- [x] ADR: KI-Anbieter, Datenschutz, Prompt-Injection-Schutz, Wechselkursquelle
- [x] Analyse: Anthropic-Client, Prompt mit Schema, Kontext aus Kategorien und Gegenparteien, Validierung der Antwort
- [x] Wechselkurs-Dienst mit Cache
- [x] Analyse als eigener Use Case (Beleg → Vorschlag), damit der E-Mail-Eingang ([#010](../010-email-eingang-imap.md)) ihn wiederverwendet
- [x] UI: Vorschlag im Buchungsdialog (Beleg oben, leere Felder vorausgefüllt, Markierung «von der KI erkannt»)
- [x] Tests: Antwort-Validierung, Zuordnung aus früheren Buchungen, Bestätigen (Reihenfolge Blob → Buchung), Ausfall der KI. Die KI selbst über eine Schnittstelle mocken
- [x] Konfiguration und `README.md` (API-Key lokal per User Secrets)

## Definition of Done

- [x] Applikation buildet, automatische Tests laufen grün
- [x] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [x] `README.md` und `/specs` aktualisiert, falls nötig
- [x] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [x] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [x] Story aus `/specs/backlog.md` entfernt
