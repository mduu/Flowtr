# ADR-005: Darstellung per Cookie und CSS-Media-Query

## Status

**Akzeptiert** (12.10.2026)

## Kontext

Flowtr hat einen dunklen Modus mit drei Optionen: Automatisch, Hell und Dunkel ([Story #003](../stories/done/003-darkmode.md)). Die Wahl muss auf zwei Arten von Seiten gelten:

- Die Konto-Seiten sind statisches SSR ([ADR-003](003-mudblazor-staticinput-fuer-konto-seiten.md)). Dort läuft kein C#-Code im Browser.
- Die App-Seiten laufen in WebAssembly und werden auf dem Server vorgerendert.

MudBlazor setzt die Farben über `MudThemeProvider` als CSS-Variablen. Die Erkennung der System-Einstellung (`GetSystemDarkModeAsync`, `ObserveSystemDarkModeChange`) braucht JavaScript und Interaktivität. Beim Laden darf nie das falsche Theme aufblitzen.

## Entscheidung

Wir entscheiden uns für **Cookie plus CSS-Media-Query**, nach dem Vorbild von Bau-Ticker:

- Ein Cookie `flowtr.darstellung` hält nur `hell` oder `dunkel`. Ohne Cookie gilt «Automatisch».
- Der Umschalter ist ein normales Formular (`POST /darstellung`). Er funktioniert ohne Skript auf allen Seiten.
- Der Server liest das Cookie und gibt die Wahl als Parameter an `Routes` (auch an WebAssembly). Bei «Hell» und «Dunkel» rendert `MudThemeProvider` direkt die passende Palette.
- Bei «Automatisch» rendert `MudThemeProvider` die helle Palette. `SystemDarkPalette` schreibt zusätzlich die dunkle Palette in einen Block `@media (prefers-color-scheme: dark)`. Der Browser wählt, auch wenn das System während der Sitzung wechselt.
- Die System-Beobachtung von MudBlazor ist ausgeschaltet.

## Optionen

### Option A: Cookie plus CSS-Media-Query (gewählt)

**Vorteile:**
- Kein JavaScript, gleiches Verhalten auf SSR- und WebAssembly-Seiten
- Kein Aufblitzen: Der Server rendert sofort richtig, bei «Automatisch» entscheidet CSS
- Systemwechsel wirkt sofort, ohne Code

**Nachteile:**
- `SystemDarkPalette` nutzt `MudThemeProvider.BuildTheme()` und verlässt sich auf dessen Ausgabe (`<style>…</style>`). Ein MudBlazor-Update kann das ändern.
- Ein Wechsel lädt die Seite neu (Formular-POST).

### Option B: MudBlazor-System-Erkennung (`IsDarkMode` per JavaScript)

**Vorteile:**
- Standardweg von MudBlazor, kein eigener Code

**Nachteile:**
- Funktioniert nicht auf den statischen Konto-Seiten
- Beim Vorrendern ist die System-Einstellung unbekannt: Die App blitzt hell auf, bevor WebAssembly startet

### Option C: Client Hint `Sec-CH-Prefers-Color-Scheme`

**Vorteile:**
- Der Server kennt die System-Einstellung schon beim ersten Request

**Nachteile:**
- Safari unterstützt den Header nicht
- Ein Systemwechsel während der Sitzung wirkt erst beim nächsten Request

## Konsequenzen

### Positive Auswirkungen

- Eine Lösung für alle Seiten. Der Umschalter ist dieselbe Komponente auf Konto- und App-Seiten.
- Eigenes CSS nutzt die Variablen von MudBlazor (`--mud-palette-*`) und passt sich automatisch an.

### Negative Auswirkungen / Risiken

- Bei MudBlazor-Updates den Test «Automatisch» im Browser wiederholen (helles und dunkles System).
- Das Cookie ist technisch nötig und enthält keine Personendaten. Es muss in eine künftige Datenschutzerklärung.

### Aufwand

- Gering: Cookie, Endpunkt, Umschalter, `SystemDarkPalette`.

## Referenzen

- [Story #003: Darkmode](../stories/done/003-darkmode.md)
- [ADR-003: MudBlazor.StaticInput für die Konto-Seiten](003-mudblazor-staticinput-fuer-konto-seiten.md)
