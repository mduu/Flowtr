# Coding Guidelines

## Allgemein
- Keine `_`-Präfixe
- Keine `Async`-Suffixe
- Ansonsten best-practices gem. Microsoft

## CQS-Handler
- Ein Use Case = eine Datei in `Commands/` oder `Queries/` mit Command/Query-Record + Handler-Klasse.
- Handler implementieren `ICommandHandler<TCommand, TResult>` bzw. `IQueryHandler<TQuery, TResult>`.
- Die Methode heisst `Handle`.
- Commands ohne Rückgabewert liefern ein einfaches `Result`; Queries sowie Commands mit Resultatwert liefern `Result<T>`.
