# Coding Guidelines

## Allgemein
- Keine `_`-Präfixe
- Keine `Async`-Suffixe
- Sonst gelten die Best Practices von Microsoft.

## Kommentare
- Zuerst den Code klarer strukturieren, z.B. mit Extract Method. Die Methode nach dem «Was» benennen.
- Nur kommentieren, was der Code nicht zeigt. Ein Kommentar muss einen echten Mehrwert bringen.
- Kommentare so kurz wie möglich halten.
- Kommentare zu Members immer als XML-Doc schreiben.
- Private Members in der Regel nicht kommentieren.
- Englische Kommentare in ASD-STE100 schreiben ([`.claude/skills/asd-ste100/`](../.claude/skills/asd-ste100/SKILL.md)).

## CQS-Handler
- Ein Use Case = eine Datei in `Commands/` oder `Queries/` mit Command/Query-Record + Handler-Klasse.
- Handler implementieren `ICommandHandler<TCommand, TResult>` oder `IQueryHandler<TQuery, TResult>`.
- Die Methode heisst `Handle`.
- Commands ohne Rückgabewert liefern ein einfaches `Result`.
- Queries und Commands mit Rückgabewert liefern `Result<T>`.
