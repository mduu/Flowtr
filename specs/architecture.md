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
	- SQL Server (via Aspire (TBD))

## Prinzipien
- Vertical Slices / Feature Slices (VSA)
- CQS-Handler-Pattern, Details siehe [Coding Guidelines](coding-guidelines.md#cqs-handler)
- Keine Mandanten / Tenants. Pro Installation gibt es nur einen Tenant.

## Komponenten
- ASP.NET Server mit WebAPI
- ASP.NET Identity mit Blazor Pages (.NET Default)
- Web-Interface in Blazor WebAssembly

## Rollen
- User werden Rollen zugeordnet (RBAC), ein User kann mehrere Rollen haben.
- Rollen: Admin, Contributor
