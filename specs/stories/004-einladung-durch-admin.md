# Story: #004, Zugang nur auf Einladung durch Admin

**Status:** Open

## Beschreibung

Es gibt keine öffentliche Registrierung. Ein Admin lädt neue User ein, indem er im Admin-Bereich ihre E-Mail-Adresse erfasst. Der Eingeladene erhält einen Link und schliesst damit seine Registrierung ab. Nicht angenommene Einladungen werden nach 30 Tagen gelöscht. Entscheid: [ADR-004](../adrs/004-zugang-nur-auf-einladung.md).

## Hintergrund

- Flowtr verwaltet Buchhaltungsdaten, Security ist Teil der Basis ([Sicherheit](../security.md)). Nur User, die ein Admin eingeladen hat, erhalten Zugang – serverseitig durchgesetzt, nicht nur in der UI.
- Ohne öffentliche Registrierung gibt es keine Spam-Registrierungen; Honeypot, CAPTCHA oder Rate Limiting für die Registrierung entfallen.
- Rollen laut [Architektur](../architecture.md#rollen): Admin, Contributor. Der Admin wählt beim Einladen die Rolle.
- Ablauf:
  1. Admin erfasst E-Mail und Rolle. Flowtr legt den User **ohne Passwort** an und erzeugt einen Einladungslink mit Token (z.B. 7 Tage gültig).
  2. Der Eingeladene öffnet den Link, setzt ein Passwort und/oder richtet direkt einen Passkey ein ([ADR-002](../adrs/002-passkeys-und-passwort.md)). Da der Link per E-Mail kam, gilt die Adresse als bestätigt.
  3. Danach ist er angemeldet und landet in der App.
- Admins sehen offene Einladungen und können sie erneut senden oder widerrufen.
- **E-Mail-Versand** fehlt noch (`IdentityNoOpEmailSender`). Bis er eingerichtet ist, zeigt Flowtr dem Admin den Einladungslink zum Kopieren an.
- **Erster Admin:** Bei einer frischen Installation gibt es niemanden, der einladen kann. Lösung: Admin-E-Mail per Konfiguration; beim Start wird für sie eine Admin-Einladung erzeugt und der Link ins Log geschrieben (Details in der Umsetzung festlegen).
- Identity speichert kein Erstellungsdatum. Für die 30-Tage-Frist braucht `ApplicationUser` ein Feld wie `InvitedAt` (Migration).
- Die Konto-Seiten sind statisches SSR ([ADR-003](../adrs/003-mudblazor-staticinput-fuer-konto-seiten.md)) – auch die Seite zum Annehmen der Einladung.
- Gemäss [AGENTS.md](../../AGENTS.md) wird jede Änderung protokolliert: Einladen, Widerrufen, Annehmen und automatisches Löschen gehören ins Audit-Log, sobald es existiert.

## Akzeptanzkriterien

- [ ] Es gibt keine öffentliche Registrierung mehr: `/Account/Register` liefert 404, auf der Login-Seite fehlt «Neues Konto erstellen».
- [ ] Admins können im Admin-Bereich einen User mit E-Mail und Rolle einladen.
- [ ] Der Eingeladene schliesst über den Link die Registrierung ab (Passwort und/oder Passkey) und ist danach angemeldet.
- [ ] Ein abgelaufener, widerrufener oder bereits benutzter Link führt zu einer verständlichen deutschen Meldung.
- [ ] Admins sehen offene Einladungen, können sie erneut senden oder widerrufen und – ohne E-Mail-Versand – den Link kopieren.
- [ ] Nicht angenommene Einladungen werden 30 Tage nach dem Erstellen automatisch gelöscht (täglicher Hintergrund-Job).
- [ ] Ohne Rolle erhält ein User keine Daten – weder in der UI noch über die API.
- [ ] Bei einer frischen Installation lässt sich der erste Admin ohne manuellen Datenbankeingriff einrichten.
- [ ] Bestehende User (heute ohne Rolle) sind nach der Migration nicht ausgesperrt.

## Aufgaben

- [ ] Modell: `InvitedAt` am `ApplicationUser`, Migration; bestehende User erhalten ein Datum und eine Rolle
- [ ] Rollen Admin/Contributor anlegen (Seed); erster Admin per Konfiguration, in `README.md` dokumentieren
- [ ] Autorisierung: Standard-Policy «braucht Rolle» für App-Seiten und API
- [ ] Öffentliche Registrierung entfernen (`Register.razor`, `RegisterConfirmation.razor`, `ResendEmailConfirmation.razor`, Links auf der Login-Seite)
- [ ] Admin-Bereich «Benutzer»: einladen, offene Einladungen anzeigen, erneut senden, widerrufen, Link kopieren – Commands/Queries nach CQS-Muster
- [ ] Seite «Einladung annehmen»: Token prüfen, Passwort setzen und/oder Passkey einrichten, E-Mail als bestätigt markieren, anmelden
- [ ] Hintergrund-Job: nicht angenommene Einladungen nach 30 Tagen löschen, mit Logging
- [ ] Tests: Einladen, Annehmen, abgelaufener/widerrufener/benutzter Link, Löschfrist (Grenzfall genau 30 Tage), kein Datenzugriff ohne Rolle, Registrierung nicht erreichbar
- [ ] `security.md` ergänzen

## Definition of Done

- [ ] Applikation buildet, automatische Tests laufen grün
- [ ] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [ ] `README.md` und `/specs` aktualisiert, falls nötig
- [ ] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [ ] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [ ] Story aus `/specs/backlog.md` entfernt
