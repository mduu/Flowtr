# Story: #001, 2FA und externe Logins entfernen

**Status:** Done (11.10.2026)

## Beschreibung

Das Identity-Template von .NET 10 bringt Zwei-Faktor-Authentifizierung (TOTP mit Authenticator-App, Recovery-Codes) und externe Logins mit. Flowtr kennt laut [Sicherheit](../../security.md#authentifizierung) aber genau zwei Anmeldewege: **Passkey** (bevorzugt) und **Passwort** (Fallback). Alles andere wird entfernt, damit kein ungenutzter, per URL erreichbarer Code im Konto-Bereich liegen bleibt.

## Hintergrund

- Passkeys sind phishing-resistent und vereinen schon für sich Besitz und Biometrie bzw. Geräte-PIN. TOTP bringt daneben keinen Mehrwert, aber zusätzliche Seiten und Wartung.
- Externe Logins (Google, Microsoft …) sind nicht vorgesehen. Es ist kein Anbieter registriert.
- Feldnah hat den gleichen Schritt gemacht (dort ADR-007 und Story #072): Reste wie `ShowRecoveryCodes.razor` und das Gerüst für externe Logins blieben zuerst liegen. Hier gleich vollständig aufräumen.

Betroffene Stellen in `src/Flowtr.Server/` (Stand Template):

| Bereich | Dateien |
| :-- | :-- |
| 2FA-Login | `Components/Account/Pages/LoginWith2fa.razor`, `LoginWithRecoveryCode.razor` |
| 2FA-Verwaltung | `Pages/Manage/TwoFactorAuthentication.razor`, `EnableAuthenticator.razor`, `Disable2fa.razor`, `ResetAuthenticator.razor`, `GenerateRecoveryCodes.razor`, `Shared/ShowRecoveryCodes.razor` |
| Externe Logins | `Pages/ExternalLogin.razor`, `Pages/Manage/ExternalLogins.razor`, `Shared/ExternalLoginPicker.razor`, Endpunkte in `IdentityComponentsEndpointRouteBuilderExtensions.cs` |
| Verweise | `Login.razor` (Weiterleitung bei `RequiresTwoFactor`), `Register.razor` (`ExternalLoginPicker`), `Shared/ManageNavMenu.razor`, `Program.cs` (`DefaultSignInScheme = ExternalScheme`) |

## Akzeptanzkriterien

- [x] Anmeldung ist nur mit Passkey oder Passwort möglich.
- [x] Die entfernten Seiten (z.B. `/Account/LoginWith2fa`, `/Account/Manage/TwoFactorAuthentication`, `/Account/Manage/ExternalLogins`) liefern 404.
- [x] Im Konto-Menü erscheinen keine Einträge mehr zu 2FA oder externen Logins.
- [x] Login- und Registrierungsseite zeigen keinen Bereich «Use another service to log in» mehr.
- [x] Passkeys hinzufügen, umbenennen und löschen funktioniert weiterhin (Hinzufügen und Login manuell geprüft; Umbenennen/Löschen unverändert).
- [x] Die Entscheidung ist als ADR festgehalten ([ADR-002](../../adrs/002-passkeys-und-passwort.md)).

## Aufgaben

- [x] Seiten und Komponenten aus der Tabelle oben löschen
- [x] Verweise in `Login.razor`, `Register.razor`, `ManageNavMenu.razor` bereinigen
- [x] Endpunkte für externe Logins (`PerformExternalLogin`, `LinkExternalLogin`) entfernen
- [x] Authentifizierungs-Setup in `Program.cs` vereinfachen (kein External-Scheme mehr nötig)
- [x] Prüfen, ob die Identity-Tabellen `AspNetUserLogins` und die 2FA-Spalten bleiben dürfen (Schema von Identity) – bleiben, keine Migration (siehe ADR-002)
- [x] ADR «Passkeys + Passwort, keine 2FA und keine externen Logins» erfassen
- [x] Im Browser durchklicken: Registrieren, Login mit Passwort, Passkey einrichten, Login mit Passkey

## Definition of Done

- [x] Applikation buildet, automatische Tests laufen grün
- [x] Bei UI-Änderungen: im Browser geprüft (Desktop); Gestaltung der Konto-Seiten folgt mit #002
- [x] `README.md` und `/specs` aktualisiert, falls nötig
- [x] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [x] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [x] Story aus `/specs/backlog.md` entfernt
