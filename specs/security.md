# Sicherheit

Flowtr verarbeitet Buchhaltungsdaten. Darum ist Security ein Key-Feature und Teil der Basis.

- Flowtr muss aktuelle Sicherheitsstandards implementieren.
- Zugriffe über Rollen (RBAC), siehe [Architektur](architecture.md#rollen).
- Freigabe des Treuhänder-Dossiers nur mit zusätzlichem Code, siehe [Journal](journal.md#dossier-für-den-treuhänder).

## Authentifizierung
- **Passkeys und Passwort** (ASP.NET Core Identity): Passkey ist der bevorzugte Login. Passwort bleibt als Fallback. Keine TOTP-2FA, keine externen Logins ([ADR-002](adrs/002-passkeys-und-passwort.md)).

## Zugang
- Keine öffentliche Registrierung. Ein Admin lädt neue User ein ([ADR-004](adrs/004-zugang-nur-auf-einladung.md)).
- Ein Einladungslink ist 7 Tage gültig und funktioniert nur einmal. «Neuer Link» in der Benutzerliste macht den alten ungültig. Ein Neustart nicht. Flowtr löscht nicht angenommene Einladungen nach 30 Tagen.
- Beim Annehmen setzt der User ein Passwort (Fallback, [ADR-002](adrs/002-passkeys-und-passwort.md)). Danach kann er einen Passkey einrichten.
- Ohne Rolle erhält ein User keine Daten: App-Seiten und `/api` verlangen eine Rolle, die Benutzerverwaltung die Rolle Admin.
- Flowtr behält immer mindestens einen Admin. Die Adressen in der Konfiguration `Admins` erhalten die Rolle Admin bei Start und Anmeldung, wenn die Adresse bestätigt ist (Wiederherstellung).
- Rollenänderungen wirken spätestens nach einer Minute auch in bestehenden Sitzungen.

## Nachvollziehbarkeit
- **Audit-Log:** Wer hat wann welche Buchung oder welchen Beleg erstellt, geändert oder gelöscht. Auch Benutzer, Rollen und Passkeys. Gleiche Transaktion wie die Änderung, ohne Geheimnisse, nur Admins sehen es. Ein Datenbank-Trigger verhindert Änderungen und Löschungen ([ADR-006](adrs/006-audit-log-per-ef-interceptor.md)).

## KI und externe Dienste
- Belege gehen zur Analyse an die Anthropic API ([ADR-008](adrs/008-ki-extraktion-mit-claude.md)). Der API-Key steht nur in User Secrets bzw. Key Vault.
- Der Inhalt eines Belegs ist Daten, nie eine Anweisung: Die Antwort der KI füllt nur ein Formular, das der User prüft. Flowtr prüft jeden Wert.
- Wechselkurse holt Flowtr bei Frankfurter (EZB). Dabei gehen nur Währung und Datum hinaus, kein Beleg.

## E-Mail-Eingang
- Zugang zur Mailbox per IMAP mit TLS. Passwort nur in User Secrets bzw. Key Vault ([ADR-009](adrs/009-email-eingang-mail-als-beleg.md)).
- Nur erlaubte Absender: die Adressen der User und eine Liste, die nur Admins pflegen. Bei `dmarc=fail` oder bei `spf=fail` ohne `dkim=pass` (Header `Authentication-Results` des Providers) gilt die Mail als gefälscht. Abgelehnte Mails landen in «Flowtr/Abgelehnt» und im Audit-Log.
- Inhalt einer Mail ist Daten, nie eine Anweisung (wie bei Belegen).
- HTML aus Mails zeigt Flowtr nur bereinigt (HtmlSanitizer) und mit einer eigenen Content-Security-Policy mit `sandbox`: kein Skript, keine Cookies, nichts von fremden Servern (keine Tracking-Pixel).

## Unveränderbarkeit
- Belege liegen im Blob Storage mit Versionierung und Soft-Delete. Ein Beleg überschreibt nie einen anderen, die Nummern vergibt eine Sequenz ([ADR-007](adrs/007-belegnummern-per-sequenz.md)).
- Flowtr prüft Uploads am Inhalt (PDF und Bilder, höchstens 20 MB) und speichert den SHA-256 jedes Belegs. Eine Mail als Beleg bleibt das unveränderte Original (`.eml`).
- Eine Immutability-Policy schützt Belege abgeschlossener Jahre (10 Jahre, OR 958f). Siehe [ADR-001](adrs/001-belegablage-in-blob-storage.md).

## Web-Sicherheit
- Security-Header auf jeder Antwort: Content-Security-Policy (nur eigene Skripte, `'wasm-unsafe-eval'` für Blazor, eine Nonce pro Request für die Import Map, `frame-ancestors 'none'`, `object-src 'none'`), HSTS ein Jahr, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, `Cross-Origin-Opener-Policy`. Die Mail-Ansicht hat eine strengere Policy mit `sandbox`.

## Betrieb
- App und Job greifen mit einer Managed Identity auf SQL, Storage und Key Vault zu. Kein Passwort und kein Schlüssel in der Konfiguration ([Betrieb](../docs/betrieb.md)).
- SQL nur mit Entra-Anmeldung (Gruppe der SQL-Admins). Storage ohne Shared Keys, ohne öffentlichen Zugriff.
- Geheimnisse (Anthropic, Mailbox) nur im Key Vault. GitHub meldet sich per OIDC an und darf nur das Image von App und Job ändern.
- Die Keys von Data Protection (Login-Cookies, Einladungslinks, Formulare) liegen im Blob Storage, geschützt mit einem Key im Key Vault.
- Logs ohne Daten aus Belegen: keine Beträge, keine Betreffs, keine Inhalte von Mails, keine Antworten der KI.
- Locks verhindern das Löschen von Datenbank und Belegen.
- Verschlüsselung at rest: Azure-Standard (keine eigenen Keys).
