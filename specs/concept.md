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
- **Im Buchungsdialog (Web-App):** Ein Beleg pro Buchung. Er steht zuoberst im Dialog. Der User lädt ihn hoch (Datei, Ziehen, Kamera), die KI füllt die Felder vor, der User prüft und speichert ([#008](stories/done/008-beleg-upload-und-ablage.md), [#009](stories/done/009-ki-extraktion.md)). Mehrere Belege auf einmal hochladen braucht es nicht: Das ist selten, der User lädt sie einzeln hoch.
- **Per E-Mail:** Der User leitet Rechnungen an eine bestimmte Mailbox weiter. *Flowtr* fragt sie per **IMAP** ab (beliebiger Provider) und legt die Belege in den [Eingang](#eingang). Der User prüft und speichert sie später, einzeln ([#010](stories/010-email-eingang-imap.md)).
- Später möglich: native App als "Senden an"- oder "Teilen"-Ziel. Sie liefert ebenfalls in den Eingang.

## Eingang
Der Eingang sammelt die Belege aus E-Mails, die noch keine Buchung haben.

- Er steht **zuoberst im Journal**, über den Monaten, z.B. «3 Belege im Eingang». Ohne Belege ist er nicht sichtbar.
- Pro Beleg: Herkunft (Absender, Betreff, empfangen am), Stand der Analyse und der Vorschlag (Datum, Betrag, Gegenpartei).
- *Flowtr* analysiert jeden Beleg gleich beim Eintreffen. Der Vorschlag ist bereit, wenn der User den Beleg öffnet.
- Ein Klick öffnet den Buchungsdialog mit dem Beleg oben und den vorausgefüllten Feldern. Der User muss nichts hochladen. Er prüft, korrigiert und speichert. Danach ist es eine normale Buchung in ihrem Monat.
- Der User kann einen Beleg verwerfen (z.B. Werbung oder Doppel). Nicht gebuchte Belege verfallen nach 30 Tagen.
- Belege im Eingang sind **noch keine Buchungen**: Sie zählen nicht in Summen, Gewinn und Export, haben noch keine Belegnummer und erscheinen nicht als Buchung im Audit-Log.
