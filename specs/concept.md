# Konzept: Belegerfassung

## Ziel
Das Erfassen der Buchungen (Einnahmen, Ausgaben) sollte möglichst effizient gehen, da dies oft nebenbei "schnell schnell" via Smartphone, Tablet oder Laptop geschieht. Dazu ist die Idee, dass man Belege wie *Microsoft Azure* Subscriptions, *Anthropic* Belege, *Stripe* Vergütungen, *Google Firebase* Abrechnungen, aber auch Belege für neue Computer uvm. in die Anwendung senden kann (z.B. via Beleg-Upload) und *Flowtr* sich smart darum kümmert.

## Ablauf
1. User reicht einen Beleg ein (siehe [Eingangskanäle](#eingangskanäle)).
2. *Flowtr* analysiert den Beleg u.a. mithilfe von KI (z.B. *Anthropic* API, Claude) und extrahiert die notwendigen Daten für die Buchung.
3. Die KI schlägt zusätzlich Kategorie und Gegenpartei vor – anhand des Belegs und früherer Buchungen derselben Gegenpartei (z.B. Azure → Software/Abos).
4. Die App zeigt die gefundenen Angaben an, sodass der User sie bestätigen oder korrigieren kann.
5. Beim finalen Speichern:
   - legt *Flowtr* den Beleg sauber nummeriert ab,
   - erstellt die Buchung (Zeile) im [Journal](journal.md) mit Referenz auf den nummerierten Beleg.

## Ablage der Belege
- Belege für Ein- und Ausgaben werden separat gespeichert.
- Belege sind als Dateien 1:1 abgelegt.
- Der Dateiname lässt eine Zuordnung zum Journal zu (= Umbenennen des Beleges beim Einlesen).
- Speicherort, Container und Namensschema: [ADR-001](adrs/001-belegablage-in-blob-storage.md).

## Eingangskanäle
- Interaktives Hochladen in der Web-App
- Als Email an eine bestimmte Mailbox weiterleiten. *Flowtr* fragt diese Mailbox per **IMAP** ab (beliebiger Provider) und verwendet die Anhänge als Input. Die erkannten Buchungen muss der User wie beim Upload bestätigen.
- Denkbar in Zukunft: native App als "Senden an"- bzw. "Teilen"-Ziel.
