# Story: #003, Darkmode mit Auto/Hell/Dunkel

**Status:** Open

## Beschreibung

Flowtr unterstützt einen dunklen Modus. Der User wählt zwischen **Automatisch** (folgt der System- bzw. Browser-Einstellung), **Hell** und **Dunkel**. Der Umschalter sieht aus wie in Bau-Ticker und sitzt in der AppBar.

## Hintergrund

- **Vorbild Bau-Ticker** (`baugesuch-ticker/src/server/BauTicker.Api`):
  - `Features/Website/ColourScheme.cs`: Die Wahl liegt in einem Cookie (nur «hell»/«dunkel», ein Jahr gültig, `HttpOnly`, `SameSite=Lax`). Ohne Cookie gilt «automatisch», und `prefers-color-scheme` entscheidet.
  - `Pages/Shared/_Layout.cshtml`: Der Server setzt `data-theme="light|dark"` auf `<html>`; bei «automatisch» fehlt das Attribut.
  - Umschalter: ein Formular (`POST /darstellung`) mit drei Icon-Buttons (Bildschirm = automatisch, Sonne = hell, Mond = dunkel), `aria-pressed` für die aktive Wahl. Funktioniert ohne Skript.
  - Aussehen (`wwwroot/css/site.css`, `.theme-switch`): pillenförmige Gruppe mit feinem Rand, 30×30-px-Buttons, die aktive Wahl hervorgehoben (heller Hintergrund, Akzentfarbe, leichter Schatten).
- **Besonderheiten in Flowtr:**
  - Die Konto-Seiten sind statisches SSR, die App-Seiten WebAssembly ([ADR-003](../adrs/003-mudblazor-staticinput-fuer-konto-seiten.md)). Umschalter und Theme müssen in **beiden** Modi funktionieren. Ein Formular-POST mit Cookie wie in Bau-Ticker erfüllt das; `MudMenu` o.ä. geht auf den Konto-Seiten nicht.
  - MudBlazor steuert die Farben über `MudThemeProvider` (`IsDarkMode`) und schreibt die Palette als CSS-Variablen. Die System-Erkennung von MudBlazor (`GetSystemDarkModeAsync`) braucht Interaktivität – für «Automatisch» auf SSR-Seiten braucht es eine Lösung über `prefers-color-scheme` (z.B. dunkle Palette zusätzlich in einem `@media (prefers-color-scheme: dark)`-Block).
  - Kein Aufblitzen des falschen Themes beim Laden: Der Server kennt die Wahl aus dem Cookie und rendert direkt richtig.
  - Für den dunklen Hintergrund gibt es bereits `assets/logo/flowtr_logo_dark.svg` (Schriftzug hell).
- Die Dark-Palette existiert schon in `FlowtrTheme` (Primär Indigo Lighten1), muss aber geprüft und ergänzt werden (AppBar hell/dunkel, Flächen, Linien).

## Akzeptanzkriterien

- [ ] In der AppBar gibt es einen Umschalter mit drei Optionen: Automatisch, Hell, Dunkel – im Aussehen wie in Bau-Ticker (Icons Bildschirm/Sonne/Mond, aktive Wahl hervorgehoben, Tooltips und `aria-label` auf Deutsch).
- [ ] «Automatisch» folgt der System-/Browser-Einstellung, auch wenn diese während der Sitzung wechselt.
- [ ] Die Wahl bleibt erhalten (Cookie) und gilt auf allen Seiten – auf den Konto-Seiten (SSR) wie in der App (WebAssembly).
- [ ] Beim Laden blitzt nie das falsche Theme auf.
- [ ] Im dunklen Modus sind alle Seiten gut lesbar: Logo mit hellem Schriftzug, AppBar, Formulare, Alerts, Fehlerleiste.
- [ ] Der Umschalter funktioniert auf iPhone-Breite (Platzierung ggf. kompakter).
- [ ] Der gewählte Ansatz für «Automatisch» auf SSR-Seiten ist als ADR festgehalten.

## Aufgaben

- [ ] Cookie-Logik analog `ColourScheme` (Auto/Hell/Dunkel) und Endpunkt `POST /darstellung` mit Rücksprung auf die aktuelle Seite
- [ ] `MudThemeProvider` anhand der Wahl steuern; Lösung für «Automatisch» ohne Interaktivität
- [ ] Dark-Palette in `FlowtrTheme` vervollständigen
- [ ] Umschalter-Komponente in der AppBar (gleiches Markup/CSS-Prinzip wie Bau-Ticker)
- [ ] Logo je nach Modus (`logo.svg` / `logo-dark.svg`)
- [ ] Fehlerleiste und eigene Styles in `app.css` für dunklen Modus anpassen
- [ ] Tests: Cookie-Wahl → richtiges Theme (Unit-Test der Cookie-Logik)
- [ ] Im Browser prüfen: alle drei Modi, Systemwechsel bei «Automatisch», Konto-Seiten und App-Seiten, Desktop und iPhone-Breite
- [ ] ADR erfassen, `ux.md` ergänzen

## Definition of Done

- [ ] Applikation buildet, automatische Tests laufen grün
- [ ] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [ ] `README.md` und `/specs` aktualisiert, falls nötig
- [ ] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [ ] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [ ] Story aus `/specs/backlog.md` entfernt
