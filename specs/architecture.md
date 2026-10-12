# Architektur

## Stack
- .NET, latest
- ASP.NET Blazor Interactive WebAssembly
- C#
- Azure Cloud Service
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

## Komponenten
- ASP.NET Server mit WebAPI
- ASP.NET Identity mit Blazor Pages (.NET Default)
- Web-Interface in Blazor WebAssembly

## Projektstruktur

Projekte und Ordner: siehe [Repository-Struktur im README](../README.md#repository-struktur). Paketversionen stehen zentral in `src/Directory.Packages.props`. Gemeinsame Build-Einstellungen stehen in `src/Directory.Build.props`.

## Rollen
- Jeder User hat Rollen (RBAC). Ein User kann mehrere Rollen haben.
- Rollen: Admin, Contributor
