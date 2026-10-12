# Sicherheit

Flowtr verarbeitet Buchhaltungsdaten. Darum ist Security ein Key-Feature und Teil der Basis.

- Flowtr muss aktuelle Sicherheitsstandards implementieren.
- Zugriffe über Rollen (RBAC), siehe [Architektur](architecture.md#rollen).
- Freigabe des Treuhänder-Dossiers nur mit zusätzlichem Code, siehe [Journal](journal.md#dossier-für-den-treuhänder).

## Authentifizierung
- **Passkeys und Passwort** (ASP.NET Core Identity): Passkey ist der bevorzugte Login. Passwort bleibt als Fallback. Keine TOTP-2FA, keine externen Logins ([ADR-002](adrs/002-passkeys-und-passwort.md)).

## Zugang
- Keine öffentliche Registrierung. Ein Admin lädt neue User ein ([ADR-004](adrs/004-zugang-nur-auf-einladung.md)).

## Nachvollziehbarkeit
- **Audit-Log:** Wer hat wann welche Buchung oder welchen Beleg erstellt, geändert oder gelöscht.

## Unveränderbarkeit
- Belege liegen im Blob Storage mit Versionierung und Soft-Delete.
- Eine Immutability-Policy schützt Belege abgeschlossener Jahre (10 Jahre, OR 958f). Siehe [ADR-001](adrs/001-belegablage-in-blob-storage.md).

## Web-Sicherheit
- Security-Header: strikte Content-Security-Policy, HSTS und weitere.
- Verschlüsselung at rest: Azure-Standard (keine eigenen Keys).
