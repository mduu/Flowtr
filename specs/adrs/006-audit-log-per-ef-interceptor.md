# ADR-006: Audit-Log per EF-Core-Interceptor

## Status

**Akzeptiert** (12.10.2026)

## Kontext

Flowtr muss jede Änderung an Buchungen, Belegen, Benutzern und Rollen nachvollziehbar machen: wer, wann, was, vorher und nachher ([Sicherheit](../security.md#nachvollziehbarkeit), [Story #006](../stories/done/006-audit-log.md)).

- Das Protokoll darf nie fehlen, auch wenn jemand einen neuen Use Case schreibt und nicht daran denkt.
- Änderung und Protokoll müssen zusammen gelingen oder zusammen scheitern.
- Das Protokoll darf keine Geheimnisse enthalten (Passwort-Hash, Security-Stamp, Passkey-Daten).
- Niemand soll Einträge ändern oder löschen können.
- Viele Änderungen laufen über ASP.NET Core Identity (`UserManager`), nicht über eigene Handler.

## Entscheidung

Wir entscheiden uns für einen **`SaveChangesInterceptor`** (`AuditInterceptor`), der für jede geänderte Entität mit einer Regel in `AuditRules` einen `AuditEntry` in dasselbe `SaveChanges` legt.

- **Regeln pro Entität** (`AuditRule`): stabiler Schlüssel (`User`, `UserRole`, `Passkey`), verborgene Properties (nur «geändert», Wert `***`), ignorierte Properties (z.B. `ConcurrencyStamp`). Eine Änderung nur an ignorierten Properties gibt keinen Eintrag.
- **Wer:** angemeldeter User, «Anonym» für Requests ohne Anmeldung (z.B. Einladung annehmen), «System» ohne Request (Jobs, Start).
- **Schutz:** Der Interceptor wirft, wenn jemand einen Eintrag ändert oder löscht. Ein Datenbank-Trigger (`INSTEAD OF UPDATE, DELETE`) lehnt dasselbe auch ausserhalb der App ab.
- **Lesen:** Query `GetAuditEntries` übersetzt Schlüssel, IDs und Werte in deutsche Texte und Schweizer Formate. Nur Admins, Seite «Verwaltung → Protokoll».

## Optionen

### Option A: EF-Core-Interceptor (gewählt)

**Vorteile:**
- Vollständig: jede Änderung über den DbContext, auch über Identity
- Gleiche Transaktion ohne zusätzlichen Code
- Neue Entitäten brauchen nur eine Regel

**Nachteile:**
- Kennt den fachlichen Grund nicht (z.B. «Einladung erneuert» ist «Benutzer geändert: Eingeladen am, Sicherheitsstempel»)
- Änderungen ausserhalb des Change Trackers fehlen: `ExecuteUpdate`/`ExecuteDelete`, rohes SQL und Cascade-Deletes der Datenbank (z.B. Rollen eines gelöschten Benutzers, wenn sie nicht geladen sind)
- Neue Entitäten brauchen ihren Schlüssel vor dem Speichern (z.B. GUID), sonst fehlt die ID im Eintrag

### Option B: Explizit im Handler

**Vorteile:**
- Fachliche Aktion mit sprechendem Namen

**Nachteile:**
- Man kann es vergessen. Änderungen über Identity brauchen eigene Aufrufe
- Gleiche Transaktion muss jeder Handler selbst sicherstellen

### Option C: Temporal Tables / Change Data Capture von SQL Server

**Vorteile:**
- Vollständig auf Datenbankebene

**Nachteile:**
- Kennt den User der App nicht
- Lokal und in Azure SQL unterschiedlich aufwendig, schwer filterbar für die UI

## Konsequenzen

### Positive Auswirkungen

- Neue Features (Buchungen, Belege) werden protokolliert, sobald ihre Entität eine Regel in `AuditRules` hat.
- Keine Geheimnisse im Protokoll, per Test abgesichert.

### Negative Auswirkungen / Risiken

- **Regeln für neue Entitäten:** Ohne Eintrag in `AuditRules` protokolliert Flowtr nichts. `AGENTS.md` verlangt die Regel und einen Test.
- **Kein `ExecuteUpdate`/`ExecuteDelete`** auf auditierten Entitäten, da der Interceptor sie nicht sieht.
- **Trigger und DBA:** Wer Schema-Rechte hat, kann den Trigger deaktivieren. Der Schutz richtet sich gegen Fehler und die App, nicht gegen einen Datenbank-Admin. Ein externer, unveränderbarer Speicher wäre ein späterer Schritt.
- EF Core muss den Trigger kennen (`HasTrigger`), sonst schlägt `OUTPUT` beim Einfügen fehl.

### Aufwand

- Gering: Interceptor, Regeln, Tabelle mit Trigger, eine Query und eine Seite.

## Referenzen

- [Story #006: Audit-Log](../stories/done/006-audit-log.md)
- [EF Core Interceptors](https://learn.microsoft.com/ef/core/logging-events-diagnostics/interceptors)
