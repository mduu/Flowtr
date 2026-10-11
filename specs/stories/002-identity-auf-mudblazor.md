# Story: #002, Identity-Seiten auf MudBlazor umstellen

**Status:** Open

## Beschreibung

Die Konto-Seiten aus dem Identity-Template (Login, Registrierung, Konto verwalten …) erwarten Bootstrap, das im Projekt nicht enthalten ist. Sie erscheinen deshalb als ungestaltetes HTML unter der AppBar. Die Seiten sollen im gleichen MudBlazor-Look wie der Rest der App erscheinen, auf Deutsch sein und auf dem iPhone gut bedienbar.

## Hintergrund

- Die Konto-Seiten laufen bewusst als **statisches SSR** und nicht interaktiv: Sie setzen und löschen das Login-Cookie über `HttpContext`. Das muss so bleiben.
- Die meisten MudBlazor-Eingabekomponenten (z.B. `MudTextField`) brauchen Interaktivität und funktionieren in einem SSR-Formular nicht direkt. Lösungsansätze:
  - Paket [MudBlazor.StaticInput](https://github.com/Anu6is/MudBlazor.StaticInput): SSR-fähige Varianten wie `MudStaticTextField`, `MudStaticCheckBox`, `MudStaticButton`
  - oder MudBlazor nur für Layout und Darstellung (`MudPaper`, `MudText`, `MudButton` als Submit) und native `InputText` mit MudBlazor-CSS-Klassen
  - Entscheid in einem ADR festhalten
- UI-Sprache ist Deutsch (`AGENTS.md`). Die Template-Texte sind Englisch.
- Es gibt aktuell keinen sichtbaren Weg, sich abzumelden oder zu den Konto-Einstellungen zu gelangen.
- Nach #001 bleiben nur Passkey und Passwort als Anmeldewege ([ADR-002](../adrs/002-passkeys-und-passwort.md)). Der Passkey-Login soll auf der Login-Seite prominent sein.

## Akzeptanzkriterien

- [ ] Alle Seiten unter `/Account` sind mit MudBlazor gestaltet (Indigo-Theme, gleiche AppBar wie die App).
- [ ] Die Seiten bleiben statisches SSR; Login, Logout, Registrierung und Passkey-Login funktionieren.
- [ ] Alle Texte auf den Konto-Seiten sind Deutsch.
- [ ] Auf der Login-Seite ist «Mit Passkey anmelden» gleichwertig sichtbar wie der Passwort-Login.
- [ ] Die Konto-Verwaltung hat eine Navigation im MudBlazor-Stil (Profil, E-Mail, Passwort, Passkeys, Persönliche Daten).
- [ ] In der AppBar gibt es ein Konto-Menü mit «Konto verwalten» und «Abmelden»; nicht angemeldet: «Anmelden».
- [ ] Auf iPhone- und Desktop-Breite geprüft.
- [ ] Die Wahl des Ansatzes für SSR-Formulare ist als ADR festgehalten.

## Aufgaben

- [ ] Ansatz für SSR-Formulare wählen (MudBlazor.StaticInput vs. native Inputs mit MudBlazor-CSS), ADR erfassen
- [ ] `ManageLayout` und Konto-Navigation auf MudBlazor umstellen
- [ ] Login, Registrierung, Passwort vergessen/zurücksetzen, E-Mail bestätigen auf MudBlazor umstellen
- [ ] Verwaltungsseiten umstellen: Profil, E-Mail, Passwort ändern/setzen, Passkeys, Passkey umbenennen, Persönliche Daten, Konto löschen
- [ ] Status- und Fehlerseiten umstellen (Zugriff verweigert, Gesperrt, Ungültiger Benutzer …)
- [ ] Texte auf Deutsch übersetzen
- [ ] Konto-Menü mit Abmelden in die AppBar
- [ ] Im Browser durchklicken auf Mobile- und Desktop-Breite

## Definition of Done

- [ ] Applikation buildet, automatische Tests laufen grün
- [ ] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [ ] `README.md` und `/specs` aktualisiert, falls nötig
- [ ] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [ ] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [ ] Story aus `/specs/backlog.md` entfernt
