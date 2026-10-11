# Sicherheit

Die Lösung muss sicher sein, da sie mit Buchhaltungsdaten umgeht. Security ist ein Key-Feature und Teil der Basis.

- Die Lösung muss aktuelle Sicherheitsstandards implementieren.
- Zugriffe über Rollen (RBAC), siehe [Architektur](architecture.md#rollen).
- Freigabe des Treuhänder-Dossiers nur mit zusätzlichem Code, siehe [Journal](journal.md#dossier-für-den-treuhänder).

## Authentifizierung
- **Passkeys + Passwort** (ASP.NET Core Identity): Passkey als bevorzugter Login, Passwort bleibt als Fallback.

## Nachvollziehbarkeit
- **Audit-Log:** Wer hat wann welche Buchung bzw. welchen Beleg erstellt, geändert oder gelöscht.

## Unveränderbarkeit
- Belege liegen im Blob Storage mit Versionierung und Soft-Delete.
- Belege abgeschlossener Jahre sind per Immutability-Policy geschützt.

## Web-Sicherheit
- Security-Header: strikte Content-Security-Policy, HSTS u.a.
- Verschlüsselung at rest: Azure-Standard (keine eigenen Keys).
