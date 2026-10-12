# Story: #003, Darkmode mit Auto/Hell/Dunkel

**Status:** Done (12.10.2026)

## Beschreibung

Flowtr unterstützt einen dunklen Modus. Der User wählt zwischen **Automatisch** (folgt der System- bzw. Browser-Einstellung), **Hell** und **Dunkel**. Der Umschalter sieht aus wie in Bau-Ticker und sitzt in der AppBar.

## Hintergrund

- **Vorbild Bau-Ticker** (`baugesuch-ticker/src/server/BauTicker.Api`):
  - `Features/Website/ColourScheme.cs`: Die Wahl liegt in einem Cookie (nur «hell»/«dunkel», ein Jahr gültig, `HttpOnly`, `SameSite=Lax`). Ohne Cookie gilt «automatisch», und `prefers-color-scheme` entscheidet.
  - `Pages/Shared/_Layout.cshtml`: Der Server setzt `data-theme="light|dark"` auf `<html>`. Bei «automatisch» fehlt das Attribut.
  - Umschalter: ein Formular (`POST /darstellung`) mit drei Icon-Buttons (Bildschirm = automatisch, Sonne = hell, Mond = dunkel), `aria-pressed` für die aktive Wahl. Funktioniert ohne Skript.
  - Aussehen (`wwwroot/css/site.css`, `.theme-switch`): pillenförmige Gruppe mit feinem Rand, 30×30-px-Buttons, die aktive Wahl hervorgehoben (heller Hintergrund, Akzentfarbe, leichter Schatten).
- **Besonderheiten in Flowtr:**
  - Die Konto-Seiten sind statisches SSR, die App-Seiten WebAssembly ([ADR-003](../../adrs/003-mudblazor-staticinput-fuer-konto-seiten.md)). Umschalter und Theme müssen in **beiden** Modi funktionieren. Ein Formular-POST mit Cookie wie in Bau-Ticker erfüllt das. `MudMenu` o.ä. geht auf den Konto-Seiten nicht.
  - MudBlazor steuert die Farben über `MudThemeProvider` (`IsDarkMode`) und schreibt die Palette als CSS-Variablen. Die System-Erkennung von MudBlazor (`GetSystemDarkModeAsync`) braucht Interaktivität. Für «Automatisch» auf SSR-Seiten braucht es eine Lösung über `prefers-color-scheme` (z.B. dunkle Palette zusätzlich in einem `@media (prefers-color-scheme: dark)`-Block).
  - Kein Aufblitzen des falschen Themes beim Laden: Der Server kennt die Wahl aus dem Cookie und rendert direkt richtig.
  - Für den dunklen Hintergrund gibt es bereits `assets/logo/flowtr_logo_dark.svg` (Schriftzug hell).
- Die Dark-Palette existiert schon in `FlowtrTheme` (Primär Indigo Lighten1). Wir müssen sie aber prüfen und ergänzen (AppBar hell/dunkel, Flächen, Linien).

## Umsetzung

- Ansatz für «Automatisch»: [ADR-005](../../adrs/005-darstellung-per-cookie-und-media-query.md). `SystemDarkPalette` schreibt die dunkle Palette in `@media (prefers-color-scheme: dark)`. Die System-Beobachtung von MudBlazor ist aus.
- Der Server gibt die Wahl aus dem Cookie als Parameter an `Routes`. Sie gilt als Cascading Value bis zum nächsten vollen Laden. Der Umschalter macht immer einen vollen POST.
- Logo: bei «Automatisch» ein `<picture>` mit `logo-dark.svg` für das dunkle System.
- Dunkle Palette: Primär Indigo Lighten2 (besserer Kontrast auf dunklem Grund), Text etwas heller als der MudBlazor-Standard.
- Fehlerleiste und `app.css` nutzen die MudBlazor-Variablen. Reste von Bootstrap entfernt.
- Geprüft in Chrome und WebKit: alle drei Modi, Systemwechsel bei «Automatisch», Login (SSR) und App-Seiten (WebAssembly), Rücksprung auf die aktuelle Seite, Desktop und iPhone-Breite.

## Akzeptanzkriterien

- [x] In der AppBar gibt es einen Umschalter mit drei Optionen: Automatisch, Hell, Dunkel. Das Aussehen ist wie in Bau-Ticker (Icons Bildschirm/Sonne/Mond, aktive Wahl hervorgehoben, Tooltips und `aria-label` auf Deutsch).
- [x] «Automatisch» folgt der System-/Browser-Einstellung, auch wenn diese während der Sitzung wechselt.
- [x] Die Wahl bleibt erhalten (Cookie) und gilt auf allen Seiten: auf den Konto-Seiten (SSR) und in der App (WebAssembly).
- [x] Beim Laden blitzt nie das falsche Theme auf.
- [x] Im dunklen Modus sind alle Seiten gut lesbar: Logo mit hellem Schriftzug, AppBar, Formulare, Alerts, Fehlerleiste.
- [x] Der Umschalter funktioniert auf iPhone-Breite (Platzierung ggf. kompakter).
- [x] Der gewählte Ansatz für «Automatisch» auf SSR-Seiten ist als ADR festgehalten.

## Aufgaben

- [x] Cookie-Logik analog `ColourScheme` (Auto/Hell/Dunkel) und Endpunkt `POST /darstellung` mit Rücksprung auf die aktuelle Seite
- [x] `MudThemeProvider` anhand der Wahl steuern. Lösung für «Automatisch» ohne Interaktivität
- [x] Dark-Palette in `FlowtrTheme` vervollständigen
- [x] Umschalter-Komponente in der AppBar (gleiches Markup/CSS-Prinzip wie Bau-Ticker)
- [x] Logo je nach Modus (`logo.svg` / `logo-dark.svg`)
- [x] Fehlerleiste und eigene Styles in `app.css` für dunklen Modus anpassen
- [x] Tests: Cookie-Wahl → richtiges Theme (Unit-Test der Cookie-Logik)
- [x] Im Browser prüfen: alle drei Modi, Systemwechsel bei «Automatisch», Konto-Seiten und App-Seiten, Desktop und iPhone-Breite
- [x] ADR erfassen, `ux.md` ergänzen

## Definition of Done

- [x] Applikation buildet, automatische Tests laufen grün
- [x] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [x] `README.md` und `/specs` aktualisiert, falls nötig
- [x] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [x] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [x] Story aus `/specs/backlog.md` entfernt
