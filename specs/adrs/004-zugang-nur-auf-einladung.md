# ADR-004: Zugang nur auf Einladung durch Admin

## Status

**Akzeptiert** (12.10.2026)

## Kontext

Das Identity-Template bringt eine öffentliche Registrierung mit: Jeder kann ein Konto anlegen. Flowtr verwaltet Buchhaltungsdaten und wird von wenigen, bekannten Personen genutzt (Inhaber, ggf. Treuhänder). Niemand soll ohne Zustimmung eines Admins Zugang erhalten, und Spam-Registrierungen sollen keinen Aufwand verursachen.

## Entscheidung

Flowtr hat **keine öffentliche Registrierung**. Ein Admin lädt neue User ein, indem er ihre E-Mail-Adresse und Rolle erfasst. Der Eingeladene schliesst die Registrierung über einen Einladungslink ab (Passwort und/oder Passkey). Nicht angenommene Einladungen werden nach 30 Tagen gelöscht. Umsetzung: [Story #004](../stories/004-einladung-durch-admin.md).

## Optionen

### Option A: Einladung durch Admin — gewählt

**Vorteile:**
- Keine Spam-Registrierungen, weil es keine öffentliche Registrierung gibt
- Nur bekannte Personen erhalten Zugang
- Kein Honeypot, CAPTCHA oder Rate Limiting für die Registrierung nötig
- E-Mail-Adresse ist mit dem Annehmen der Einladung bestätigt

**Nachteile:**
- Braucht E-Mail-Versand – bis dahin kopiert der Admin den Link selbst
- Der erste Admin muss per Konfiguration eingerichtet werden

### Option B: Selbstregistrierung mit Freischaltung

**Beschreibung:** User registrieren sich selbst und sehen nichts, bis ein Admin sie freischaltet; nicht freigeschaltete Konten werden nach 30 Tagen gelöscht.

**Vorteile:**
- Interessierte können sich ohne vorherigen Kontakt melden

**Nachteile:**
- Spam-Registrierungen landen in der Datenbank und beim Admin; Schutz durch Honeypot, Mindest-Ausfüllzeit und Rate Limiting reduziert sie nur
- Zusätzliche Zustände (wartend) und eine Wartemeldungsseite
- Passt eher zu offenen Plattformen als zu einer privaten Buchhaltung

### Option C: Öffentliche Registrierung ohne Prüfung (Template)

**Nachteile:**
- Jeder erhält Zugang – für Buchhaltungsdaten nicht akzeptabel

## Konsequenzen

### Positive Auswirkungen

- Kleinere Angriffsfläche: Registrierungs- und Bestätigungsseiten entfallen
- Kein Spam, kein Moderationsaufwand

### Negative Auswirkungen / Risiken

- Ein echter E-Mail-Versand wird wichtiger; bis dahin Link manuell weitergeben.
- Geht der letzte Admin verloren (z.B. Passkey und Passwort), braucht es einen Wiederherstellungsweg über die Konfiguration.

### Aufwand

- Mittel: Admin-Bereich «Benutzer», Seite «Einladung annehmen», Hintergrund-Job, Migration.

## Referenzen

- [ADR-002: Passkey und Passwort](002-passkeys-und-passwort.md)
- [ADR-003: MudBlazor.StaticInput für die Konto-Seiten](003-mudblazor-staticinput-fuer-konto-seiten.md)
