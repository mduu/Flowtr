# ADR-003: MudBlazor.StaticInput für die Konto-Seiten

## Status

**Akzeptiert** (11.10.2026)

## Kontext

Die Konto-Seiten von ASP.NET Core Identity (Login, Registrierung, Konto verwalten …) laufen als **statisches SSR**: Sie setzen und löschen das Login-Cookie über `HttpContext` und sind deshalb mit `[ExcludeFromInteractiveRouting]` von der interaktiven WebAssembly-App ausgenommen. Das muss so bleiben.

Die App selbst nutzt MudBlazor ([UX](../ux.md)). Die meisten MudBlazor-Eingabekomponenten (`MudTextField`, `MudCheckBox` …) brauchen jedoch Interaktivität. Ohne Interaktivität binden sie keine Werte an das Formular. Die Template-Seiten erwarten zudem Bootstrap, das im Projekt fehlt ([Story #002](../stories/done/002-identity-auf-mudblazor.md)).

## Entscheidung

Wir gestalten die Konto-Seiten mit MudBlazor und verwenden für Formulare das Paket **[Extensions.MudBlazor.StaticInput](https://github.com/0phois/MudBlazor.StaticInput)** (`MudStaticTextField`, `MudStaticCheckBox`, `MudStaticButton`).

Regeln für Konto-Seiten und für Komponenten, die auch auf ihnen gerendert werden (z.B. `MainLayout`, `AccountMenu`):

- Eingaben nur mit `MudStatic*`-Komponenten, Absenden mit `MudStaticButton` oder `MudButton ButtonType="Submit"`.
- Für Darstellung nur Komponenten, die reines HTML ausgeben: `MudPaper`, `MudText`, `MudAlert`, `MudStack`, `MudGrid`, `MudLink`, `MudNavLink`, `MudButton` mit `Href`, `MudList`.
- Keine Komponenten, die Interaktivität voraussetzen: `MudMenu`, `MudDialog`, `MudSelect`, `MudAutocomplete`, `MudTooltip` usw.

## Optionen

### Option A: MudBlazor.StaticInput (gewählt)

**Vorteile:**
- Echte MudBlazor-Komponenten mit identischem Aussehen und Theme
- Validierung über `For="() => …"` wie gewohnt
- Für genau diesen Fall gebaut (Identity-Seiten). Aktiv gepflegt. Unterstützt .NET 10 und MudBlazor 9

**Nachteile:**
- Zusätzliche Abhängigkeit eines Drittanbieters. Bei MudBlazor-Upgrades müssen wir die Kompatibilität prüfen

### Option B: Native Eingaben mit MudBlazor-CSS-Klassen

**Beschreibung:** `InputText` usw. mit den CSS-Klassen von MudBlazor «nachbauen».

**Vorteile:**
- Keine zusätzliche Abhängigkeit

**Nachteile:**
- Wir müssen das Markup der MudBlazor-Komponenten nachbauen und bei Upgrades nachführen
- Wir müssen Label-Animation, Fehlerzustand und Adornments selbst umsetzen

### Option C: Konto-Seiten interaktiv machen

**Nachteile:**
- Identity braucht `HttpContext` für Cookies. Mit interaktivem Rendering geht das nur, wenn wir die Anmeldung über eigene API-Endpunkte neu bauen

## Konsequenzen

### Positive Auswirkungen

- Einheitlicher Look in der ganzen App. Konto-Seiten bleiben sicher als statisches SSR

### Negative Auswirkungen / Risiken

- Bei einem MudBlazor-Major-Upgrade muss StaticInput mitziehen. Sonst bleibt nur Option B.
- Die Einschränkung auf statische Komponenten gilt auch für alles, was im `MainLayout` steht.

### Aufwand

- Gering: Paket einbinden, Seiten umschreiben.

## Referenzen

- [MudBlazor.StaticInput](https://github.com/0phois/MudBlazor.StaticInput)
- [Blazor render modes: static SSR](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes)
