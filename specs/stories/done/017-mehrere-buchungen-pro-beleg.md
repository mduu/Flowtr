# Story: #017, Mehrere Buchungen aus einem Beleg

**Status:** Done (04.11.2026)

## Beschreibung

Die KI schlägt aus einem Beleg mehrere Buchungen vor, wenn der Beleg sie braucht. Beispiel: Die Abrechnung eines Zahlungsanbieters gibt eine Einnahme und eine Ausgabe für die Gebühren. Der User lädt den Beleg einmal hoch und bestätigt die Buchungen nacheinander. Er muss nichts mehr von Hand umrechnen.

## Hintergrund

- **Heute:** Für die Saldenübersicht von Stripe lädt der User das PDF zweimal hoch und korrigiert jedes Mal den Vorschlag der KI. Die KI kennt das Bruttoprinzip nicht und nimmt oft die falsche Zeile, z.B. «Gesamt» statt «Aktivität».
- **Bruttoprinzip:** Einnahmen und Gebühren werden getrennt gebucht und nicht verrechnet. Die Auszahlung auf die Bank ist keine Buchung.
  - Stripe Oktober 2026: Zahlungen 108.00, Rückerstattung −99.00, Gebühren 4.48 (davon 0.69 + 0.06 Gebühr von Stripe selbst) → Einnahme 9.00 und Ausgabe 4.48.
  - TWINT Oktober 2026: Zahlung 10.00, Gebühr 0.44 → Einnahme 10.00 und Ausgabe 0.44.
- **App Store Connect** zahlt netto aus, ohne Gebührenrechnung ([#016](../016-apple-abrechnung-automatisch.md)): eine Buchung.
- Eine Buchung hat höchstens einen Beleg mit einer Belegnummer pro Art (E- oder A-, [ADR-007](../../adrs/007-belegnummern-per-sequenz.md)). Einnahme und Ausgabe können also nicht denselben Beleg teilen.
- Nachfolger: [#015](../015-stripe-abrechnung-automatisch.md) (automatisch abholen). Mit dieser Story ist #015 weniger dringend.

## Entscheide

- **KI:** Das Tool `record_booking` erhält `more_bookings`, eine Liste weiterer Buchungen mit denselben Feldern. Jede Buchung hat neu eine `note`. Der Prompt erklärt Abrechnungen von Zahlungsanbietern: Einnahme brutto nach Rückerstattungen, alle Gebühren als eine Ausgabe, Auszahlungen ignorieren, Datum Ende der Periode, Gegenpartei der Anbieter. Eine normale Rechnung mit mehreren Positionen bleibt eine Buchung.
- **Prüfung:** *Flowtr* prüft jede weitere Buchung wie die erste (Datum, Betrag, Kategorie der richtigen Art, Kurs). Höchstens 5 Buchungen pro Beleg.
- **Dialog:** Der User bestätigt jede Buchung selbst. Der Dialog zeigt «Buchung 1 von 2 zu diesem Beleg» und einen Hinweis mit den weiteren Buchungen. Nach dem Speichern öffnet *Flowtr* die nächste Buchung mit demselben Beleg. Schliesst der User den Hinweis, gibt es nur diese Buchung.
- **Beleg:** Jede Buchung legt die Datei als eigenen Beleg ab, mit eigener Nummer (z.B. E-2026-001 und A-2026-002). Die Datei liegt also mehrfach in der Ablage, mit gleichem SHA-256. Kein gemeinsamer Beleg für mehrere Buchungen: Das würde Belegnummern, Export und Abschluss umbauen.
- **Upload:** Die erste Buchung speichert mit `KeepUploads`: Der Upload bleibt in `eingang`, die nächste Buchung legt ihn wieder ab. Die letzte löscht ihn. Bricht der User ab, löscht die Housekeeping den Upload nach 30 Tagen.
- **Eingang:** Der Vorschlag im Eingang enthält die weiteren Buchungen («+1 Buchung»). Die erste Buchung nimmt den Eintrag aus dem Eingang, die weiteren folgen direkt im Dialog.

## Umsetzung

- `ReceiptReading` und `BookingProposal` mit `Note` und `MoreBookings`. Alte Vorschläge im Eingang (JSON) lesen sich weiter.
- `AnthropicReceiptReader`: Schema mit `more_bookings` und `note`, Prompt für Zahlungsanbieter.
- `AnalyzeUploads`: prüft jede Buchung gleich, höchstens `MaxBookings` (5).
- `BookingInput.KeepUploads`, `ReceiptFiling.Save(…, keepUploads)`.
- Client: `BookingDialog` mit Position, Hinweis und Notiz aus dem Vorschlag, Ergebnis `BookingSaved`. `JournalPage` öffnet die nächste Buchung. `InboxPanel` zeigt «+N Buchungen».
- **Geprüft:** 237 Tests (7 neue). Im Browser lokal mit der echten Saldenübersicht von Stripe für Oktober 2026: Vorschlag Einnahme 9.00 (31.10.2026, Umsatz und Honorare, Stripe, Notiz «2 Zahlungen 108.00, 1 Rückerstattung −99.00»), danach Ausgabe 4.48 (Bank- und Kreditkartenspesen). Gespeichert als E-2026-001 und A-2026-002. Dialog in Handy-Breite.

## Akzeptanzkriterien

- [x] Die Saldenübersicht von Stripe für Oktober 2026 gibt einen Vorschlag mit Einnahme 9.00 und einer weiteren Buchung Ausgabe 4.48, beide mit Datum 31.10.2026.
- [x] Nach dem Speichern der ersten Buchung öffnet der Dialog die zweite mit demselben Beleg. Jede Buchung hat eine eigene Belegnummer.
- [x] Eine normale Rechnung gibt weiterhin eine Buchung.
- [x] Ein Beleg aus dem Eingang zeigt die Anzahl weiterer Buchungen und bucht sie gleich.
- [x] Der User kann die weiteren Buchungen ablehnen.

## Aufgaben

- [x] KI: Schema, Prompt, Antwort lesen
- [x] Vorschlag: weitere Buchungen prüfen, Notiz
- [x] Ablage: Upload für die nächste Buchung behalten
- [x] Dialog und Journal: nächste Buchung öffnen, Hinweis, Position
- [x] Eingang: Anzahl weiterer Buchungen
- [x] Tests: Reader, Vorschlag, Grenze, Ablage zweimal, Eingang
- [x] Specs: [Konzept](../../concept.md), [Journal](../../journal.md), ADR-008 (Nachtrag)

## Definition of Done

- [x] Applikation buildet, automatische Tests laufen grün
- [x] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [x] `README.md` und `/specs` aktualisiert, falls nötig
- [x] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [x] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [x] Story aus `/specs/backlog.md` entfernt
