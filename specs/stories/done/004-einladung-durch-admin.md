# Story: #004, Zugang nur auf Einladung durch Admin

**Status:** Done (12.10.2026)

## Beschreibung

Es gibt keine öffentliche Registrierung. Ein Admin lädt neue User ein, indem er im Admin-Bereich ihre E-Mail-Adresse erfasst. Der Eingeladene erhält einen Link und schliesst damit seine Registrierung ab. Flowtr löscht nicht angenommene Einladungen nach 30 Tagen. Entscheid: [ADR-004](../../adrs/004-zugang-nur-auf-einladung.md).

## Hintergrund

- Flowtr verwaltet Buchhaltungsdaten. Security ist Teil der Basis ([Sicherheit](../../security.md)). Nur User, die ein Admin eingeladen hat, erhalten Zugang. Der Server setzt das durch, nicht nur die UI.
- Ohne öffentliche Registrierung gibt es keine Spam-Registrierungen. Honeypot, CAPTCHA oder Rate Limiting für die Registrierung entfallen.
- Rollen laut [Architektur](../../architecture.md#rollen): Admin, Contributor. Der Admin wählt beim Einladen die Rolle.
- Ablauf:
  1. Admin erfasst E-Mail und Rolle. Flowtr legt den User **ohne Passwort** an und erzeugt einen Einladungslink mit Token (z.B. 7 Tage gültig).
  2. Der Eingeladene öffnet den Link, setzt ein Passwort und/oder richtet direkt einen Passkey ein ([ADR-002](../../adrs/002-passkeys-und-passwort.md)). Da der Link per E-Mail kam, gilt die Adresse als bestätigt.
  3. Danach ist er angemeldet und landet in der App.
- Admins sehen offene Einladungen und können sie erneut senden oder widerrufen.
- **E-Mail-Versand** fehlt noch (`IdentityNoOpEmailSender`). Bis er eingerichtet ist, zeigt Flowtr dem Admin den Einladungslink zum Kopieren an.
- **Erster Admin:** Bei einer frischen Installation gibt es niemanden, der einladen kann. Lösung: Admin-E-Mail per Konfiguration. Beim Start erzeugt Flowtr für sie eine Admin-Einladung und schreibt den Link ins Log (Details in der Umsetzung festlegen).
- Identity speichert kein Erstellungsdatum. Für die 30-Tage-Frist braucht `ApplicationUser` ein Feld wie `InvitedAt` (Migration).
- Die Konto-Seiten sind statisches SSR ([ADR-003](../../adrs/003-mudblazor-staticinput-fuer-konto-seiten.md)). Das gilt auch für die Seite zum Annehmen der Einladung.
- Gemäss [AGENTS.md](../../../AGENTS.md) protokolliert Flowtr jede Änderung. Einladen, Widerrufen, Annehmen und automatisches Löschen gehören ins Audit-Log, sobald es existiert.

## Umsetzung

- **Passwort ist Pflicht** beim Annehmen. Danach landet der User auf «Passkeys» mit einem Hinweis. Grund: [ADR-002](../../adrs/002-passkeys-und-passwort.md) schliesst Konten nur mit Passkey aus.
- **Erneut senden** heisst «Neuer Link»: Flowtr versendet noch keine E-Mails. Der neue Link macht den alten ungültig (Security-Stamp), und die 30 Tage beginnen neu. Der Admin kopiert den Link.
- **Erster Admin:** Konfiguration `Admins` (wie in Bau-Ticker). Bei Start und bei jeder Anmeldung (Claims-Factory) erhalten bestätigte Adressen die Rolle Admin. Ohne Konto: Einladung beim Start, Link im Log (Warning). Flowtr **entzieht die Rolle nie automatisch** (Wunsch des Users).
- **Rollen verwalten** in der Benutzerliste (Checkboxen pro Rolle). Flowtr behält mindestens einen Admin. Rollenänderungen wirken nach höchstens einer Minute (`SecurityStampValidator`).
- **Migration:** Rollen mit festen IDs als Seed. Bestehende User erhalten `InvitedAt` und die Rolle Contributor.
- **Kein Zugriff:** Server-Requests auf Seiten ohne passende Rolle führen zu «Zugriff verweigert», Navigation in der App zeigt «Kein Zugriff». `/api` verlangt eine Rolle, `/api/users` die Rolle Admin. Die API antwortet mit 401/403 statt mit Redirect.
- **Audit-Log:** existiert noch nicht. Einladen, neuer Link, Widerrufen, Annehmen, Rollen und automatisches Löschen schreiben vorerst ins Log.
- Geprüft in Chrome und WebKit: Einladen, Link kopieren, Widerrufen (mit Rückfrage), Annehmen, benutzter und widerrufener Link, Contributor ohne Admin-Zugriff (UI und API), anonym (API 401), Mobile-Breite.

## Akzeptanzkriterien

- [x] Es gibt keine öffentliche Registrierung mehr: `/Account/Register` liefert 404, auf der Login-Seite fehlt «Neues Konto erstellen».
- [x] Admins können im Admin-Bereich einen User mit E-Mail und Rolle einladen.
- [x] Der Eingeladene schliesst über den Link die Registrierung ab (Passwort und/oder Passkey) und ist danach angemeldet.
- [x] Ein abgelaufener, widerrufener oder bereits benutzter Link führt zu einer verständlichen deutschen Meldung.
- [x] Admins sehen offene Einladungen und können sie erneut senden oder widerrufen. Ohne E-Mail-Versand können sie den Link kopieren.
- [x] Ein täglicher Hintergrund-Job löscht nicht angenommene Einladungen 30 Tage nach dem Erstellen.
- [x] Ohne Rolle erhält ein User keine Daten, weder in der UI noch über die API.
- [x] Bei einer frischen Installation lässt sich der erste Admin ohne manuellen Datenbankeingriff einrichten.
- [x] Bestehende User (heute ohne Rolle) sind nach der Migration nicht ausgesperrt.

## Aufgaben

- [x] Modell: `InvitedAt` am `ApplicationUser`, Migration. Bestehende User erhalten ein Datum und eine Rolle
- [x] Rollen Admin/Contributor anlegen (Seed). Erster Admin per Konfiguration, in `README.md` dokumentieren
- [x] Autorisierung: Standard-Policy «braucht Rolle» für App-Seiten und API
- [x] Öffentliche Registrierung entfernen (`Register.razor`, `RegisterConfirmation.razor`, `ResendEmailConfirmation.razor`, Links auf der Login-Seite)
- [x] Admin-Bereich «Benutzer»: einladen, offene Einladungen anzeigen, erneut senden, widerrufen, Link kopieren. Commands/Queries nach CQS-Muster
- [x] Seite «Einladung annehmen»: Token prüfen, Passwort setzen und/oder Passkey einrichten, E-Mail als bestätigt markieren, anmelden
- [x] Hintergrund-Job: nicht angenommene Einladungen nach 30 Tagen löschen, mit Logging
- [x] Tests: Einladen, Annehmen, abgelaufener/widerrufener/benutzter Link, Löschfrist (Grenzfall genau 30 Tage), kein Datenzugriff ohne Rolle, Registrierung nicht erreichbar
- [x] `security.md` ergänzen

## Definition of Done

- [x] Applikation buildet, automatische Tests laufen grün
- [x] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [x] `README.md` und `/specs` aktualisiert, falls nötig
- [x] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [x] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [x] Story aus `/specs/backlog.md` entfernt
