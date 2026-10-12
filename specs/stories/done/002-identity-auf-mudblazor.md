# Story: #002, Identity-Seiten auf MudBlazor umstellen

**Status:** Done (12.10.2026)

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
- Nach #001 bleiben nur Passkey und Passwort als Anmeldewege ([ADR-002](../../adrs/002-passkeys-und-passwort.md)). Der Passkey-Login soll auf der Login-Seite prominent sein.

## Erkenntnisse aus der Umsetzung

- Nach dem Login stürzte die Startseite in Safari mit `parentNode.insertBefore` ab. Ursache: die Browser-Erweiterung LanguageTool verändert das DOM, das die WebAssembly-App verwaltet. Weder `data-lt-active`/`data-gramm`-Attribute noch abgeschaltetes Prerendering noch ein eigener App-Container halfen. Entscheid: keine Workarounds im Code, LanguageTool wird im Browser deaktiviert. Interaktive Inseln (Feldnah-Modell) wurden wegen Komplexität und langsamerer Navigation verworfen – Flowtr bleibt «UI = WebAssembly, Server = REST-API».
- Enter im Passwortfeld löste den Passkey-Login aus, weil der Browser die erste Absende-Schaltfläche nimmt. Der Passwort-Button steht deshalb im DOM zuerst, die Passkey-Schaltfläche wird per CSS `order` oben angezeigt.
- WebKit meldet auf `/Account/*` einen 404 für `Account/Flowtr.Client.*.bundle.scp.css`: Den relativen `Link: preload`-Header der Scoped-CSS löst WebKit gegen die Seite statt gegen die CSS-Datei auf. Harmlos, das CSS selbst wird korrekt geladen.

## Akzeptanzkriterien

- [x] Alle Seiten unter `/Account` sind mit MudBlazor gestaltet (Indigo-Theme, gleiche AppBar wie die App).
- [x] Die Seiten bleiben statisches SSR; Login, Logout, Registrierung und Passkey-Login funktionieren (statisches SSR per Test abgesichert, Passkey-Login manuell geprüft).
- [x] Alle Texte auf den Konto-Seiten sind Deutsch.
- [x] Auf der Login-Seite ist «Mit Passkey anmelden» gleichwertig sichtbar wie der Passwort-Login.
- [x] Die Konto-Verwaltung hat eine Navigation im MudBlazor-Stil (Profil, E-Mail, Passwort, Passkeys, Persönliche Daten).
- [x] In der AppBar gibt es ein Konto-Menü mit «Konto verwalten» und «Abmelden»; nicht angemeldet: «Anmelden».
- [x] Auf iPhone- und Desktop-Breite geprüft.
- [x] Die Wahl des Ansatzes für SSR-Formulare ist als ADR festgehalten ([ADR-003](../../adrs/003-mudblazor-staticinput-fuer-konto-seiten.md)).

## Aufgaben

- [x] Ansatz für SSR-Formulare wählen (MudBlazor.StaticInput vs. native Inputs mit MudBlazor-CSS), ADR erfassen
- [x] `ManageLayout` und Konto-Navigation auf MudBlazor umstellen
- [x] Login, Registrierung, Passwort vergessen/zurücksetzen, E-Mail bestätigen auf MudBlazor umstellen
- [x] Verwaltungsseiten umstellen: Profil, E-Mail, Passwort ändern/setzen, Passkeys, Passkey umbenennen, Persönliche Daten, Konto löschen
- [x] Status- und Fehlerseiten umstellen (Zugriff verweigert, Gesperrt, Ungültiger Benutzer …)
- [x] Texte auf Deutsch übersetzen
- [x] Konto-Menü mit Abmelden in die AppBar
- [x] Im Browser durchklicken auf Mobile- und Desktop-Breite

## Definition of Done

- [x] Applikation buildet, automatische Tests laufen grün
- [x] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [x] `README.md` und `/specs` aktualisiert, falls nötig
- [x] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [x] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [x] Story aus `/specs/backlog.md` entfernt
