# Architektur

## Stack
- .NET, latest
- ASP.NET Blazor Interactive WebAssembly
- C#
- Azure Container Apps mit Scale to Zero, Azure SQL Free Tier ([ADR-010](adrs/010-hosting-auf-container-apps.md), [Betrieb](../docs/betrieb.md))
- EF Core, SQL Server, Azure SQL in Produktion
- MudBlazor als UI-Library
- Local Dev Stack:
	- Aspire
	- Azurite (via Aspire)
	- SQL Server als Container via Aspire, mit persistentem Volume (kein SQLite: so fallen Fehler in Migrationen früh auf)

## Prinzipien
- Vertical Slices / Feature Slices (VSA)
- CQS-Handler-Pattern. Details: siehe [Coding Guidelines](coding-guidelines.md#cqs-handler)
- Keine Mandanten / Tenants. Pro Installation gibt es nur einen Tenant.
- Eigengebrauch, keine SaaS-Lösung. Andere Betreiber forken das Repo und betreiben eine eigene Installation. Kein Code für Registrierung, Abos oder Abrechnung.

## Komponenten
- ASP.NET Server mit WebAPI
- ASP.NET Identity mit Blazor Pages (.NET Default)
- Web-Interface in Blazor WebAssembly
- Hintergrund-Jobs im Server: E-Mail-Eingang per IMAP (MailKit), Aufräumen der Uploads. Sie öffnen die Datenbank nur, wenn es etwas zu tun gibt: Sie pausiert sonst.
- Job für den E-Mail-Eingang: dasselbe Image mit `--job inbox`, in Azure stündlich als Container Apps Job

## Projektstruktur

Projekte und Ordner: siehe [Repository-Struktur im README](../README.md#repository-struktur). Paketversionen stehen zentral in `src/Directory.Packages.props`. Gemeinsame Build-Einstellungen stehen in `src/Directory.Build.props`.

## Rollen
- Jeder User hat Rollen (RBAC). Ein User kann mehrere Rollen haben.
- Rollen: Admin, Contributor
- Ohne Rolle erhält ein User keine Daten. Admins verwalten die Rollen unter «Benutzer» ([ADR-004](adrs/004-zugang-nur-auf-einladung.md)).
