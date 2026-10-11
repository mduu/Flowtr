# Story: #001, 2FA und externe Logins entfernen

**Status:** Open

## Beschreibung

Das Identity-Template von .NET 10 bringt Zwei-Faktor-Authentifizierung (TOTP mit Authenticator-App, Recovery-Codes) und externe Logins mit. Flowtr kennt laut [Sicherheit](../security.md#authentifizierung) aber genau zwei Anmeldewege: **Passkey** (bevorzugt) und **Passwort** (Fallback). Alles andere wird entfernt, damit kein ungenutzter, per URL erreichbarer Code im Konto-Bereich liegen bleibt.

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

- [ ] Anmeldung ist nur mit Passkey oder Passwort möglich.
- [ ] Die entfernten Seiten (z.B. `/Account/LoginWith2fa`, `/Account/Manage/TwoFactorAuthentication`, `/Account/Manage/ExternalLogins`) liefern 404.
- [ ] Im Konto-Menü erscheinen keine Einträge mehr zu 2FA oder externen Logins.
- [ ] Login- und Registrierungsseite zeigen keinen Bereich «Use another service to log in» mehr.
- [ ] Passkeys hinzufügen, umbenennen und löschen funktioniert weiterhin.
- [ ] Die Entscheidung ist als ADR festgehalten.

## Aufgaben

- [ ] Seiten und Komponenten aus der Tabelle oben löschen
- [ ] Verweise in `Login.razor`, `Register.razor`, `ManageNavMenu.razor` bereinigen
- [ ] Endpunkte für externe Logins (`PerformExternalLogin`, `LinkExternalLogin`) entfernen
- [ ] Authentifizierungs-Setup in `Program.cs` vereinfachen (kein External-Scheme mehr nötig)
- [ ] Prüfen, ob die Identity-Tabellen `AspNetUserLogins` und die 2FA-Spalten bleiben dürfen (Schema von Identity) – keine Migration nur fürs Aufräumen
- [ ] ADR «Passkeys + Passwort, keine 2FA und keine externen Logins» erfassen
- [ ] Im Browser durchklicken: Registrieren, Login mit Passwort, Passkey einrichten, Login mit Passkey

## Definition of Done

- [ ] Applikation buildet, automatische Tests laufen grün
- [ ] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [ ] `README.md` und `/specs` aktualisiert, falls nötig
- [ ] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [ ] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [ ] Story aus `/specs/backlog.md` entfernt
