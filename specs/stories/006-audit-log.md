# Story: #006, Audit-Log

**Status:** Open

## Beschreibung

Flowtr protokolliert jede Änderung an Buchungen, Belegen, Benutzern und Rollen: wer, wann, was, vorher und nachher. Admins sehen das Protokoll in der App. Niemand kann es ändern oder löschen.

## Hintergrund

- [AGENTS.md](../../AGENTS.md): «Audit everything». [Sicherheit](../security.md#nachvollziehbarkeit): wer hat wann welche Buchung oder welchen Beleg erstellt, geändert oder gelöscht.
- Das Audit-Log muss vor der ersten Buchung stehen ([#007](007-journal-manuell-erfassen.md)), damit von Anfang an alles protokolliert ist.
- Die Benutzerverwaltung ([#004](done/004-einladung-durch-admin.md)) schreibt heute nur ins normale Log. Sie stellt auf das Audit-Log um: Einladen, neuer Link, Widerrufen, Annehmen, Rollen ändern, automatisches Löschen.
- Ansatz zum Entscheiden (ADR):
  - **Explizit im Handler** (z.B. `IAuditLog.Write(...)` in jedem Command): klar und gezielt, aber man kann es vergessen.
  - **Automatisch in EF Core** (`SaveChanges`-Interceptor über alle auditierten Entitäten): vergisst nichts, kennt aber den fachlichen Grund nicht.
  - Möglich ist auch eine Kombination: Interceptor für die Daten, Handler für die fachliche Aktion.
- Speicherort: eigene Tabelle in derselben Datenbank, damit Änderung und Protokoll in einer Transaktion liegen. Nur `INSERT` aus der App. Optional ein SQL-Trigger oder eine Berechtigung, die `UPDATE`/`DELETE` verhindert.
- Inhalt pro Eintrag: Zeitpunkt (UTC), User-ID und E-Mail, Aktion, Entität und ID, Änderungen als JSON (vorher/nachher). Keine Passwörter, Tokens oder Passkey-Daten.
- Aufbewahrung: mindestens so lange wie die Buchungen (10 Jahre, OR 958f).

## Akzeptanzkriterien

- [ ] Jedes Erstellen, Ändern und Löschen einer auditierten Entität schreibt genau einen Eintrag, in derselben Transaktion wie die Änderung.
- [ ] Die Benutzerverwaltung schreibt ihre Aktionen ins Audit-Log.
- [ ] Ein Eintrag zeigt wer, wann, was, vorher und nachher. Er enthält keine Geheimnisse.
- [ ] Admins sehen das Audit-Log in der App, filterbar nach Zeitraum, Benutzer und Entität. Andere Rollen sehen es nicht.
- [ ] Die App bietet keine Möglichkeit, Einträge zu ändern oder zu löschen.

## Aufgaben

- [ ] ADR: Ansatz (Handler, Interceptor oder Kombination) und Schutz gegen Änderung
- [ ] Modell `AuditEntry`, Migration
- [ ] Schreiben (Interceptor und/oder `IAuditLog`), aktueller User aus dem `HttpContext`, System-Jobs als «System»
- [ ] Benutzerverwaltung umstellen
- [ ] Query und API für das Protokoll, Seite «Protokoll» für Admins
- [ ] Tests: Eintrag pro Änderung, gleiche Transaktion (Fehler = kein Eintrag, keine Änderung), keine Geheimnisse, nur Admins
- [ ] `security.md` und `architecture.md` ergänzen

## Definition of Done

- [ ] Applikation buildet, automatische Tests laufen grün
- [ ] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [ ] `README.md` und `/specs` aktualisiert, falls nötig
- [ ] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [ ] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [ ] Story aus `/specs/backlog.md` entfernt
