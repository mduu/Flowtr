# Konzept: Belegerfassung

## Ziel
Das Erfassen der Buchungen (Einnahmen, Ausgaben) soll schnell gehen. Der User erfasst oft nebenbei, mit Smartphone, Tablet oder Laptop. Er sendet Belege an *Flowtr* (z.B. per Upload): *Microsoft Azure* Subscriptions, *Anthropic* Belege, *Stripe* Vergütungen, *Google Firebase* Abrechnungen, Belege für neue Computer usw. *Flowtr* verarbeitet sie selbständig.

## Ablauf
1. User reicht einen Beleg ein (siehe [Eingangskanäle](#eingangskanäle)).
2. *Flowtr* analysiert den Beleg, unter anderem mit KI (z.B. *Anthropic* API, Claude). *Flowtr* extrahiert die Daten für die Buchung.
3. Die KI schlägt zusätzlich Kategorie und Gegenpartei vor. Sie verwendet dazu den Beleg und frühere Buchungen derselben Gegenpartei (z.B. Azure → Software/Abos).
4. Die App zeigt die gefundenen Angaben an. Der User bestätigt oder korrigiert sie.
5. Beim finalen Speichern:
   - *Flowtr* legt den Beleg nummeriert ab.
   - *Flowtr* erstellt die Buchung (Zeile) im [Journal](journal.md) mit Referenz auf den nummerierten Beleg.

## Ablage der Belege
- *Flowtr* speichert Belege für Einnahmen und Ausgaben separat.
- Belege liegen 1:1 als Dateien ab.
- Der Dateiname ordnet den Beleg dem Journal zu. *Flowtr* benennt den Beleg dazu beim Einlesen um.
- Speicherort, Container und Namensschema: [ADR-001](adrs/001-belegablage-in-blob-storage.md).

## Eingangskanäle
- Interaktives Hochladen in der Web-App
- Als Email an eine bestimmte Mailbox weiterleiten. *Flowtr* fragt diese Mailbox per **IMAP** ab (beliebiger Provider). Die Anhänge sind der Input. Der User bestätigt die erkannten Buchungen wie beim Upload.
- Später möglich: native App als "Senden an"- oder "Teilen"-Ziel.
