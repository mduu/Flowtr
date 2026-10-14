# Story: #010, E-Mail-Eingang (IMAP)

**Status:** Open

## Beschreibung

Der User leitet Rechnungen per E-Mail an eine bestimmte Mailbox weiter. Flowtr holt die Mails regelmässig per IMAP ab, lässt die Belege von der KI analysieren und zeigt sie im [Eingang](../concept.md#eingang) zuoberst im Journal. Später, wann es ihm passt, öffnet der User sie einzeln, prüft die vorausgefüllten Felder und speichert, ohne etwas hochzuladen.

## Hintergrund

- Kanäle und Eingang: [Konzept](../concept.md#eingangskanäle). Voraussetzung: Buchungsdialog mit Beleg ([#008](done/008-beleg-upload-und-ablage.md)) und KI-Analyse ([#009](done/009-ki-extraktion.md)).
- **Diese Story baut den Eingang:** Modell getrennt von den Buchungen (Beleg, Herkunft, empfangen am, Stand der Analyse, Vorschlag), Block zuoberst im Journal, Verwerfen, Verfall nach 30 Tagen. Belege im Eingang zählen nicht in Summen und Export und haben noch keine Belegnummer.
- **Mailbox:** beliebiger Provider mit IMAP (z.B. eine eigene Adresse wie `belege@…`). Konfiguration: Host, Port, TLS, Benutzer, Passwort bzw. App-Passwort, Ordner. Geheimnisse nur in User Secrets bzw. Key Vault, nie im Repository.
- **Abholen:** Hintergrund-Job, z.B. alle 5 Minuten, dazu ein Knopf «Jetzt abholen» im Eingang. Bibliothek z.B. MailKit.
- **Nach dem Import:** Die Mail in einen Ordner «Flowtr/Verarbeitet» verschieben, nicht löschen. So bleibt das Original beim Provider, und Flowtr liest sie nicht zweimal. Zusätzlich Schutz vor Doppeln über die `Message-ID` und einen Hash der Datei.
- **Was ist der Beleg:**
  - Anhänge als PDF oder Bild (gleiche Typen und Limiten wie beim Upload, [#008](done/008-beleg-upload-und-ablage.md)). Mehrere Anhänge geben mehrere Belege.
  - Viele Rechnungen kommen ohne Anhang, nur als HTML-Mail (z.B. Stripe, App Store). Dann wird die Mail selbst zum Beleg: Flowtr speichert sie als PDF. Entscheid in der Umsetzung (ADR), z.B. HTML zu PDF auf dem Server.
  - Bilder in der Signatur (Logos) und kleine Inline-Bilder ignorieren.
- **Sicherheit:**
  - Jeder kann an die Adresse schreiben. Flowtr nimmt nur Mails von erlaubten Absendern an (Liste in der Verwaltung, z.B. die Adressen der User). Andere Mails landen in «Flowtr/Abgelehnt» und im Log.
  - Absender lassen sich fälschen. SPF/DKIM-Ergebnis des Providers prüfen, wo verfügbar.
  - Inhalt der Mail ist Daten, keine Anweisung (Prompt-Injection, siehe [#009](done/009-ki-extraktion.md)).
- **Herkunft im Eingang:** «E-Mail von … , Betreff … , empfangen am …». Bei Weiterleitungen der ursprüngliche Absender, wenn erkennbar (z.B. Microsoft bei einer weitergeleiteten Azure-Rechnung): Er hilft der KI bei der Gegenpartei.
- **Fehler:** Ist die Mailbox nicht erreichbar, schreibt Flowtr das ins Log und zeigt einen Hinweis im Eingang. Kein Verlust: Die Mail bleibt im Posteingang bis zum nächsten erfolgreichen Abholen.
- **Audit:** Import, Ablehnung und Verwerfen gehen ins Audit-Log.

## Akzeptanzkriterien

- [ ] Eine weitergeleitete Mail mit PDF-Anhang erscheint spätestens nach dem nächsten Abholen im Eingang zuoberst im Journal, mit Herkunft und KI-Vorschlag.
- [ ] Belege im Eingang zählen nicht in den Summen des Journals. Ohne Belege im Eingang ist der Block nicht sichtbar.
- [ ] Der User kann einen Beleg im Eingang verwerfen. Nicht gebuchte Belege verfallen nach 30 Tagen.
- [ ] Ein Klick öffnet den Buchungsdialog mit dem Beleg oben und den vorausgefüllten Feldern. Speichern bucht ihn wie einen Upload (Nummer, Ablage, Buchung).
- [ ] Eine Rechnung ohne Anhang (nur HTML) wird als PDF zum Beleg.
- [ ] Mehrere Anhänge geben mehrere Belege. Logos aus der Signatur erscheinen nicht.
- [ ] Mails von nicht erlaubten Absendern erscheinen nicht im Eingang.
- [ ] Dieselbe Mail oder derselbe Anhang erscheint nie doppelt.
- [ ] Verarbeitete Mails liegen beim Provider im Ordner «Flowtr/Verarbeitet».
- [ ] «Jetzt abholen» holt sofort ab. Ein Fehler der Mailbox ist im Eingang sichtbar.
- [ ] Zugangsdaten stehen nicht im Repository.

## Aufgaben

- [ ] ADR: HTML-Mail als Beleg (Umwandlung in PDF), Absender-Prüfung
- [ ] Konfiguration der Mailbox (Optionen, User Secrets, Key Vault), in `README.md` dokumentieren
- [ ] Abhol-Job (MailKit): Mails lesen, Anhänge bzw. HTML als Beleg in `eingang/`, Mail verschieben, Doppel erkennen
- [ ] Erlaubte Absender in der Verwaltung
- [ ] Eingang: Modell und Migration, Hintergrund-Analyse (Use Case aus #009), Block zuoberst im Journal mit «Jetzt abholen» und Hinweis bei Fehler, Verwerfen, Verfall-Job
- [ ] Buchungsdialog: aus dem Eingang öffnen (Beleg oben, Vorschlag in den Feldern), beim Speichern Eintrag aus dem Eingang entfernen
- [ ] Tests: Anhänge, HTML-Mail, Inline-Bilder, fremder Absender, Doppel, Fehler der Mailbox. IMAP über eine Schnittstelle mocken
- [ ] `concept.md` und `security.md` ergänzen

## Definition of Done

- [ ] Applikation buildet, automatische Tests laufen grün
- [ ] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [ ] `README.md` und `/specs` aktualisiert, falls nötig
- [ ] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [ ] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [ ] Story aus `/specs/backlog.md` entfernt
