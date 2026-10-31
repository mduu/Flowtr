# Story: #010, E-Mail-Eingang (IMAP)

**Status:** Done (15.10.2026)

## Beschreibung

Der User leitet Rechnungen per E-Mail an eine bestimmte Mailbox weiter. Flowtr holt die Mails regelmässig per IMAP ab, lässt die Belege von der KI analysieren und zeigt sie im [Eingang](../../concept.md#eingang) zuoberst im Journal. Später, wann es ihm passt, öffnet der User sie einzeln, prüft die vorausgefüllten Felder und speichert, ohne etwas hochzuladen.

## Hintergrund

- Kanäle und Eingang: [Konzept](../../concept.md#eingangskanäle). Voraussetzung: Buchungsdialog mit Beleg ([#008](008-beleg-upload-und-ablage.md)) und KI-Analyse ([#009](009-ki-extraktion.md)).
- **Diese Story baut den Eingang:** Modell getrennt von den Buchungen (Beleg, Herkunft, empfangen am, Stand der Analyse, Vorschlag), Block zuoberst im Journal, Verwerfen, Verfall nach 30 Tagen. Belege im Eingang zählen nicht in Summen und Export und haben noch keine Belegnummer.
- **Mailbox:** beliebiger Provider mit IMAP (z.B. eine eigene Adresse wie `belege@…` bei Hostpoint). Je eine Mailbox für Entwicklung und Betrieb. Lokal ohne Zugangsdaten startet Aspire eine Test-Mailbox (GreenMail). Konfiguration: Host, Port, TLS, Benutzer, Passwort bzw. App-Passwort, Ordner. Geheimnisse nur in User Secrets bzw. Key Vault, nie im Repository.
- **Abholen:** Hintergrund-Job, z.B. alle 5 Minuten, dazu ein Knopf «Jetzt abholen» im Eingang. Bibliothek z.B. MailKit.
- **Nach dem Import:** Die Mail in einen Ordner «Flowtr/Verarbeitet» verschieben, nicht löschen. So bleibt das Original beim Provider, und Flowtr liest sie nicht zweimal. Zusätzlich Schutz vor Doppeln über die `Message-ID` und einen Hash der Datei.
- **Was ist der Beleg:**
  - Anhänge als PDF oder Bild (gleiche Typen und Limiten wie beim Upload, [#008](008-beleg-upload-und-ablage.md)). Mehrere Anhänge geben mehrere Belege.
  - Viele Rechnungen kommen ohne Anhang, nur als HTML-Mail (z.B. Stripe, App Store). Dann wird die Mail selbst zum Beleg: Flowtr legt sie unverändert als .eml ab ([ADR-009](../../adrs/009-email-eingang-mail-als-beleg.md)). Die KI erhält das bereinigte HTML als Text. Flowtr zeigt die Mail als bereinigtes HTML in einer abgeschotteten Seite an. Ein PDF erzeugt erst der Export (QuestPDF hinter einem Adapter).
  - Bilder in der Signatur (Logos) und kleine Inline-Bilder ignorieren.
- **Sicherheit:**
  - Jeder kann an die Adresse schreiben. Flowtr nimmt nur Mails von erlaubten Absendern an (Liste in der Verwaltung, z.B. die Adressen der User). Andere Mails landen in «Flowtr/Abgelehnt» und im Log.
  - Absender lassen sich fälschen. SPF/DKIM-Ergebnis des Providers prüfen, wo verfügbar.
  - Inhalt der Mail ist Daten, keine Anweisung (Prompt-Injection, siehe [#009](009-ki-extraktion.md)).
- **Herkunft im Eingang:** «E-Mail von … , Betreff … , empfangen am …». Bei Weiterleitungen der ursprüngliche Absender, wenn erkennbar (z.B. Microsoft bei einer weitergeleiteten Azure-Rechnung): Er hilft der KI bei der Gegenpartei.
- **Fehler:** Ist die Mailbox nicht erreichbar, schreibt Flowtr das ins Log und zeigt einen Hinweis im Eingang. Kein Verlust: Die Mail bleibt im Posteingang bis zum nächsten erfolgreichen Abholen.
- **Audit:** Import, Ablehnung und Verwerfen gehen ins Audit-Log.

## Umsetzung

- Entscheid: [ADR-009](../../adrs/009-email-eingang-mail-als-beleg.md). Anhänge (PDF, Bild) sind die Belege. Eine Mail ohne solchen Anhang ist selbst der Beleg, als unverändertes `.eml`.
- **Modell:** `ImportedMail` (jede Mail einmal, Schlüssel aus der `Message-ID`, Ergebnis: importiert, schon bekannt, abgelehnt), `InboxItem` (Beleg im Eingang mit Upload, SHA-256, Stand der Analyse, Vorschlag als JSON), `AllowedSender`. Alle im Audit-Log, Änderungen der Analyse nicht.
- **Abholen:** `InboxWorker` alle 5 Minuten und nach «Jetzt abholen»: `FetchMail`, dann `AnalyzeInbox` (Use Case `AnalyzeUpload` aus #009 mit Herkunft als Hinweis), dann `ExpireInboxItems`. Ein Lock verhindert zwei gleichzeitige Läufe. Der Stand (zuletzt abgeholt, Fehler) liegt im Speicher.
- **Mailbox:** Schnittstelle `IMailbox`, Umsetzung `ImapMailbox` mit MailKit. Ordner «Flowtr/Verarbeitet» und «Flowtr/Abgelehnt» legt Flowtr selbst an. Lokal: GreenMail als Container in Aspire (abschaltbar mit `TestMailbox=false`).
- **Belege aus Mails:** Bilder mit `Content-ID` und unter 10 KB ignoriert. Doppel: gleiche `Message-ID` oder gleicher SHA-256 wie ein Beleg im Eingang oder ein abgelegter Beleg. Der ursprüngliche Absender kommt aus einer angehängten Mail oder der ersten «Von:»/«From:»-Zeile.
- **KI:** Eine `.eml` geht als Text an Claude: Kopf und HTML ohne Styles, Skripte und Attribute, mit Tabellen. Der System-Prompt sagt, dass bei weitergeleiteten Mails nicht der User die Gegenpartei ist.
- **Anzeige:** `GET /api/receipts/…` liefert für eine `.eml` eine HTML-Seite (HtmlSanitizer, Bilder nur aus der Mail als `data:`), mit `Content-Security-Policy: sandbox …; default-src 'none'`. `?original=true` gibt das Original zum Herunterladen.
- **Buchen:** Der Dialog erhält den Upload und den Vorschlag des Eingangs, ohne neue Analyse. `ReceiptFiling` entfernt den Eintrag aus dem Eingang in derselben Transaktion wie die Buchung.
- **UI:** Block «Eingang» zuoberst im Journal (nur mit Belegen oder Fehler), Knopf «E-Mails jetzt abholen» neben «Neue Buchung», Verwerfen mit Rückfrage. Während die KI liest, lädt der Block alle 3 Sekunden neu. Verwaltung: Seite «E-Mail-Eingang» mit Mailbox-Stand und erlaubten Absendern (Adresse oder `@domain`).
- **Nicht umgesetzt:** Test mit der echten Mailbox bei Hostpoint (gibt es noch nicht). Ob Hostpoint den Header `Authentication-Results` setzt, ist offen: Ohne ihn prüft Flowtr nur die Absender-Adresse.
- **Geprüft:** 33 Tests (Anhänge, HTML-Mail, Logos, fremder und gefälschter Absender, Domain, Doppel, Fehler beim Verschieben, Mailbox nicht erreichbar, Analyse, Buchen, Verwerfen, Verfall, Audit-Log, Rollen, Bereinigung des HTML). Im Browser (Chrome, WebKit, Desktop und iPhone, hell und dunkel) mit GreenMail und echtem Claude: PDF-Anhang und HTML-Mail mit Vorschlag in USD, fremder Absender abgelehnt, Buchen aus dem Eingang, Verwerfen, Ansicht der Mail ohne Skript und Tracking-Pixel.

## Akzeptanzkriterien

- [x] Eine weitergeleitete Mail mit PDF-Anhang erscheint spätestens nach dem nächsten Abholen im Eingang zuoberst im Journal, mit Herkunft und KI-Vorschlag.
- [x] Belege im Eingang zählen nicht in den Summen des Journals. Ohne Belege im Eingang ist der Block nicht sichtbar.
- [x] Der User kann einen Beleg im Eingang verwerfen. Nicht gebuchte Belege verfallen nach 30 Tagen.
- [x] Ein Klick öffnet den Buchungsdialog mit dem Beleg oben und den vorausgefüllten Feldern. Speichern bucht ihn wie einen Upload (Nummer, Ablage, Buchung).
- [x] Eine Rechnung ohne Anhang (nur HTML) wird als .eml zum Beleg. Flowtr zeigt sie als bereinigtes HTML an, ohne Skripte und ohne Inhalte von aussen.
- [x] Mehrere Anhänge geben mehrere Belege. Logos aus der Signatur erscheinen nicht.
- [x] Mails von nicht erlaubten Absendern erscheinen nicht im Eingang.
- [x] Dieselbe Mail oder derselbe Anhang erscheint nie doppelt.
- [x] Verarbeitete Mails liegen beim Provider im Ordner «Flowtr/Verarbeitet».
- [x] «Jetzt abholen» holt sofort ab. Ein Fehler der Mailbox ist im Eingang sichtbar.
- [x] Zugangsdaten stehen nicht im Repository.

## Aufgaben

- [x] ADR: HTML-Mail als Beleg (.eml), Absender-Prüfung
- [x] Konfiguration der Mailbox (Optionen, User Secrets, Key Vault), in `README.md` dokumentieren
- [x] Abhol-Job (MailKit): Mails lesen, Anhänge bzw. HTML als Beleg in `eingang/`, Mail verschieben, Doppel erkennen
- [x] Erlaubte Absender in der Verwaltung
- [x] Eingang: Modell und Migration, Hintergrund-Analyse (Use Case aus #009), Block zuoberst im Journal mit «Jetzt abholen» und Hinweis bei Fehler, Verwerfen, Verfall-Job
- [x] Buchungsdialog: aus dem Eingang öffnen (Beleg oben, Vorschlag in den Feldern), beim Speichern Eintrag aus dem Eingang entfernen
- [x] Tests: Anhänge, HTML-Mail, Inline-Bilder, fremder Absender, Doppel, Fehler der Mailbox. IMAP über eine Schnittstelle mocken
- [x] `concept.md` und `security.md` ergänzen

## Definition of Done

- [x] Applikation buildet, automatische Tests laufen grün
- [x] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [x] `README.md` und `/specs` aktualisiert, falls nötig
- [x] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [x] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [x] Story aus `/specs/backlog.md` entfernt
