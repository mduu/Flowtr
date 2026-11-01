# ADR-009: E-Mail-Eingang: Mail als Beleg (.eml), Absender-Prüfung

## Status

**Akzeptiert** (15.10.2026)

## Kontext

Der User leitet Rechnungen an eine Mailbox weiter. *Flowtr* holt sie per IMAP ab und legt die Belege in den [Eingang](../concept.md#eingang) ([Story #010](../stories/done/010-email-eingang-imap.md)).

- Viele Rechnungen kommen ohne Anhang, nur als HTML-Mail (z.B. Stripe, App Store). Die Mail selbst ist dann der Beleg.
- Die Geschäftsbücherverordnung (GeBüV) verlangt, dass Belege unverändert und prüfbar aufbewahrt werden.
- *Flowtr* läuft auf Azure App Service mit normalem Code-Deployment. Chromium läuft dort nicht (Sandbox unter Windows, fehlende Bibliotheken unter Linux).
- Die KI muss den Inhalt der Mail lesen können, möglichst mit der Struktur der Rechnung (Tabellen).
- Jeder kann an die Adresse schreiben. Absender lassen sich fälschen.
- Der Inhalt der Mail ist Daten, keine Anweisung. HTML aus einer Mail darf im Browser kein Skript ausführen und nichts nachladen.

## Entscheidung

**Was ist der Beleg:**

- Anhänge als PDF oder Bild sind die Belege, mit denselben Typen und Limiten wie beim Upload. Bilder mit `Content-ID` (im HTML eingebettet, z.B. Logos) und Bilder unter 10 KB ignoriert *Flowtr*.
- Hat die Mail keinen solchen Anhang, ist die **Mail selbst der Beleg**: *Flowtr* legt sie unverändert als **.eml** ab (`message/rfc822`). Sie erhält beim Buchen eine Belegnummer wie jeder andere Beleg.
- **Für die KI** wandelt *Flowtr* die Mail in Text um: Kopf (Von, An, Datum, Betreff) und das **bereinigte HTML** (ohne Styles, Skripte, Attribute), sonst der Text-Teil. Die Tabellenstruktur bleibt erhalten.
- **Zur Anzeige** wandelt *Flowtr* die Mail in eine HTML-Seite um: Kopf und bereinigter Inhalt (HtmlSanitizer). Bilder nur aus der Mail selbst (`cid:` als `data:`), nichts von aussen. Die Seite hat den Header `Content-Security-Policy: sandbox …; default-src 'none'`: kein Skript, keine Cookies, kein Nachladen, auch wenn der Bereiniger etwas übersieht. Das Original lässt sich als .eml herunterladen.
- **PDF erst beim Export:** Ein PDF der Mail braucht erst der Treuhänder. Die Export-Story erzeugt es mit QuestPDF hinter einer Schnittstelle (Adapter), damit sich die Bibliothek ersetzen lässt.

**Absender-Prüfung:**

- Erlaubt sind die Adressen der User mit einer Rolle und die Einträge in der Verwaltung (Adresse oder ganze Domain, z.B. `@stripe.com`).
- *Flowtr* liest den obersten Header `Authentication-Results`, den der Provider beim Empfang setzt. Bei `dmarc=fail`, oder bei `spf=fail`/`softfail` ohne `dkim=pass`, gilt die Mail als gefälscht.
- Abgelehnte Mails verschiebt *Flowtr* nach «Flowtr/Abgelehnt» und schreibt sie ins Log und ins Audit-Log. Sie erscheinen nicht im Eingang.

**Abholen:**

- MailKit, IMAP mit TLS. Zugangsdaten nur in User Secrets bzw. Key Vault.
- Verarbeitete Mails verschiebt *Flowtr* nach «Flowtr/Verarbeitet», nie löschen.
- Schutz vor Doppeln: jede Mail einmal pro `Message-ID` (Tabelle `ImportedMails`), jede Datei einmal pro SHA-256 (Eingang und abgelegte Belege).
- Reihenfolge: zuerst die Dateien in den Container `eingang`, dann die Datenbank, zuletzt das Verschieben der Mail. Scheitert das Verschieben, erkennt der nächste Lauf die Mail an der `Message-ID` und verschiebt sie nur noch.

## Optionen

### Option A: Mail als .eml, Anzeige als bereinigtes HTML (gewählt)

**Vorteile:**
- Das Original bleibt unverändert, inklusive Header und DKIM-Signatur: der beste Nachweis
- Die KI erhält das HTML mit seiner Struktur, günstiger als ein PDF (nur Text-Tokens)
- Keine PDF-Bibliothek und kein Browser auf dem Server, normales Code-Deployment

**Nachteile:**
- Der Browser zeigt .eml nicht an: *Flowtr* braucht eine eigene Ansicht
- HTML aus fremder Quelle im Browser: braucht Bereinigung und CSP-Sandbox

### Option B: PDF aus dem Text der Mail (QuestPDF)

**Vorteile:**
- Ein PDF wie jeder andere Beleg, überall lesbar

**Nachteile:**
- Das Layout der Rechnung geht verloren, die KI erhält weniger Struktur als im HTML
- Nur eine Kopie, nicht das Original

### Option C: PDF mit Chromium (Container) oder Gotenberg (Sidecar)

**Vorteile:**
- Das PDF sieht aus wie die Mail im Browser

**Nachteile:**
- Container-Deployment, grosses Image bzw. zweiter Dienst
- Ebenfalls nur eine Kopie

## Konsequenzen

### Positive Auswirkungen

- Ein Beleg aus einer HTML-Mail ist so prüfbar wie ein PDF.
- Der Eingang nutzt den Upload-Weg aus [#008](../stories/done/008-beleg-upload-und-ablage.md): Speichern legt den Beleg gleich ab.

### Negative Auswirkungen / Risiken

- Die Bereinigung des HTML ist sicherheitsrelevant. Die CSP-Sandbox ist die zweite Verteidigung.
- `Authentication-Results` hängt vom Provider ab. Fehlt der Header, prüft *Flowtr* nur die Absender-Adresse.
- Der Export braucht ein PDF der Mail (Export-Story).

### Aufwand

- Mittel: Abhol-Job, Eingang, Umwandlung für KI und Anzeige, Verwaltung der Absender.

## Nachtrag (01.11.2026, Story #011)

*Flowtr* läuft nicht auf App Service, sondern auf Azure Container Apps ([ADR-010](010-hosting-auf-container-apps.md)). Der Entscheid bleibt: Auch in einem Container wäre Chromium ein grosses Image mit langer Startzeit. Das Abholen übernimmt in Azure ein stündlicher Job. Er öffnet die Datenbank nur, wenn Mails da sind.

## Referenzen

- [Story #010: E-Mail-Eingang (IMAP)](../stories/done/010-email-eingang-imap.md)
- [ADR-001: Belegablage in Azure Blob Storage](001-belegablage-in-blob-storage.md)
- [ADR-008: KI-Extraktion mit Claude](008-ki-extraktion-mit-claude.md)
- [MailKit](https://github.com/jstedfast/MailKit), [HtmlSanitizer](https://github.com/mganss/HtmlSanitizer)
