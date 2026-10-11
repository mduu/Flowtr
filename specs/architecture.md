
## Stack
- .NET, latest
- ASP.NET Blazor Interactive WebAssembly
- C#
- Azure Cloud Service
- EF Core, SQL-Server, Azure SQL in Produktion
- MudBlazor als UI-Library verwenden
- Local dev stack:
	- Aspire
	- Azurite (via Aspire)
	- SQL-Server (via Aspire (TBD))
## Principles
* Vertical Slices / Feature Slices (VSA)
* CQS-Handler-Pattern: Ein Use Case = eine Datei in `Commands/` oder `Queries/` mit Command/Query-Record + Handler-Klasse, die `ICommandHandler<TCommand, TResult>` bzw. `IQueryHandler<TQuery, TResult>` implementiert. Die Methode heisst `Handle`; Commands ohne Rückgabewert liefern ein einfaches ``Result`` und Queries sowie Commands mit Resultatwert liefern ``Result<T>``.
- Keine Mandanten / Tenants. Pro Installation gibt es nur einen Tenant.
- User werden Rollen zugeordnet (RBAC), ein User kann mehrere Rollen haben.
- Rollen: Admin, Contributor
## Komponenten
- ASP.NET Server mit WebAPI
- ASP.NET Identity mit Blazor Pages (.NET Default)
- Web-Interface in Blazor Web-Assembly
- */platform* für mandantenübergreifende Betriebsthemen wie User-Management, Tenant-Management
## Coding Guidelines
- keine ``_`` präfixe
- keine ``Async`` suffixe
## UX
- Modernes, freundliches Web-Interface. Es sollte sich nach Möglichkeit anfühlen wie eine Desktop-Anwendung.
- Responsive Design, da die Software sowohl via Mobile (iPhone) als auch via Tablet und Laptop/Desktop genutzt wird.
- Farben gemäss Material-Design bzw. MudBlazor. Verwende als Primärfarbe *Indigo*.
- Für Operationen, die länger dauern, muss ein Fortschritt oder mind. Spinner angezeigt werden. Upload mit entsprechendem Upload-Dialog/-Control, wo man den Fortschritt sieht bzw. Feedback erhält.

