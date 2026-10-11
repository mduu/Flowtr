# ADR-002: Passkey und Passwort als einzige Anmeldewege

## Status

**Akzeptiert** (11.10.2026)

## Kontext

Das Identity-Template von .NET 10 bringt drei Anmeldewege mit: Passwort, Passkey und externe Logins (Google, Microsoft …). Dazu kommt Zwei-Faktor-Authentifizierung mit Authenticator-App (TOTP) und Recovery-Codes.

Flowtr verwaltet Buchhaltungsdaten; Security ist Teil der Basis ([Sicherheit](../security.md)). Jeder zusätzliche Anmeldeweg ist Angriffsfläche und Code, der gepflegt werden muss – auch wenn ihn niemand nutzt, denn die Seiten sind per URL erreichbar.

## Entscheidung

Flowtr kennt genau zwei Anmeldewege: **Passkey** (bevorzugt) und **Passwort** (Fallback). TOTP-2FA, Recovery-Codes und externe Logins werden vollständig entfernt (Story [#001](../stories/done/001-2fa-und-externe-logins-entfernen.md)).

## Optionen

### Option A: Passkey + Passwort — gewählt

**Vorteile:**
- Passkeys sind phishing-resistent und vereinen schon für sich Besitz und Biometrie bzw. Geräte-PIN
- Passwort bleibt als Fallback, wenn kein Passkey zur Hand ist
- Wenig Code, keine ungenutzten Seiten

**Nachteile:**
- Wer nur das Passwort nutzt, hat keinen zweiten Faktor

### Option B: Zusätzlich TOTP-2FA

**Vorteile:**
- Zweiter Faktor auch für reine Passwort-Logins

**Nachteile:**
- TOTP-Codes sind phishbar, Passkeys nicht
- Sieben zusätzliche Seiten (Einrichten, Deaktivieren, Recovery-Codes …)
- Zwei parallele Systeme verwirren

### Option C: Nur Passkeys

**Vorteile:**
- Kein Passwort, das gestohlen werden kann

**Nachteile:**
- Kein Zugang, wenn alle Passkeys verloren sind – bei Buchhaltungsdaten nicht tragbar ohne zusätzliches Recovery-Verfahren

## Konsequenzen

### Positive Auswirkungen

- Kleinere Angriffsfläche, weniger Code im Konto-Bereich
- Einfacher Login-Flow

### Negative Auswirkungen / Risiken

- Kein zweiter Faktor bei Passwort-Logins. Die Nutzung von Passkeys soll deshalb aktiv gefördert werden (z.B. Hinweis nach dem Login, solange kein Passkey registriert ist).
- Die Identity-Tabelle `AspNetUserLogins` und die 2FA-Spalten in `AspNetUsers` bleiben bestehen. Sie gehören zum Schema von Identity und werden nicht genutzt; eine Migration nur fürs Aufräumen lohnt nicht.

### Aufwand

- Gering: Template-Seiten löschen, Verweise und Endpunkte bereinigen.

## Referenzen

- [Passkeys in ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/passkeys)
