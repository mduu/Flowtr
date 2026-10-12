# Story: #006, Audit-Log

**Status:** Done (12.10.2026)

## Beschreibung

Flowtr protokolliert jede Änderung an Buchungen, Belegen, Benutzern und Rollen: wer, wann, was, vorher und nachher. Admins sehen das Protokoll in der App. Niemand kann es ändern oder löschen.

## Hintergrund

- [AGENTS.md](../../../AGENTS.md): «Audit everything». [Sicherheit](../../security.md#nachvollziehbarkeit): wer hat wann welche Buchung oder welchen Beleg erstellt, geändert oder gelöscht.
- Das Audit-Log muss vor der ersten Buchung stehen ([#007](../007-journal-manuell-erfassen.md)), damit von Anfang an alles protokolliert ist.
- Die Benutzerverwaltung ([#004](004-einladung-durch-admin.md)) schreibt heute nur ins normale Log. Sie stellt auf das Audit-Log um: Einladen, neuer Link, Widerrufen, Annehmen, Rollen ändern, automatisches Löschen.
- Ansatz zum Entscheiden (ADR):
  - **Explizit im Handler** (z.B. `IAuditLog.Write(...)` in jedem Command): klar und gezielt, aber man kann es vergessen.
  - **Automatisch in EF Core** (`SaveChanges`-Interceptor über alle auditierten Entitäten): vergisst nichts, kennt aber den fachlichen Grund nicht.
  - Möglich ist auch eine Kombination: Interceptor für die Daten, Handler für die fachliche Aktion.
- Speicherort: eigene Tabelle in derselben Datenbank, damit Änderung und Protokoll in einer Transaktion liegen. Nur `INSERT` aus der App. Optional ein SQL-Trigger oder eine Berechtigung, die `UPDATE`/`DELETE` verhindert.
- Inhalt pro Eintrag: Zeitpunkt (UTC), User-ID und E-Mail, Aktion, Entität und ID, Änderungen als JSON (vorher/nachher). Keine Passwörter, Tokens oder Passkey-Daten.
- Aufbewahrung: mindestens so lange wie die Buchungen (10 Jahre, OR 958f).

## Umsetzung

- Ansatz: EF-Core-Interceptor mit Regeln pro Entität ([ADR-006](../../adrs/006-audit-log-per-ef-interceptor.md)). Auditiert: Benutzer, Rollen-Zuordnungen, Passkeys (nur Erstellen/Löschen, denn jede Passkey-Anmeldung ändert den Zähler).
- Die Benutzerverwaltung musste nicht umgebaut werden: Ihre Änderungen laufen über Identity und damit über den DbContext. Die fachlichen Log-Zeilen bleiben fürs Betriebs-Log.
- «Einladung annehmen» speichert Passwort und Bestätigung jetzt in einem Schritt, damit es ein Eintrag ist.
- Geheimnisse: Passwort-Hash und Security-Stamp erscheinen nur als «verborgen». `ConcurrencyStamp` und normalisierte Namen ignoriert.
- Schutz: Interceptor wirft bei Änderung/Löschung, SQL-Trigger lehnt `UPDATE`/`DELETE` ab (mit `sqlcmd` geprüft). Wer DBA-Rechte hat, kann den Trigger umgehen (siehe ADR).
- Gelöschte Benutzer: Die Ansicht holt ihre E-Mail aus dem eigenen Protokoll.
- Seite «Verwaltung → Protokoll» (Admin): Filter Von/Bis, Wer, Bereich, 50 Einträge pro Seite. Die AppBar hat dafür ein einziges Symbol «Verwaltung», damit sie auf dem iPhone Platz hat.
- Geprüft in Chrome und WebKit, Desktop und iPhone-Breite.

## Akzeptanzkriterien

- [x] Jedes Erstellen, Ändern und Löschen einer auditierten Entität schreibt genau einen Eintrag, in derselben Transaktion wie die Änderung.
- [x] Die Benutzerverwaltung schreibt ihre Aktionen ins Audit-Log.
- [x] Ein Eintrag zeigt wer, wann, was, vorher und nachher. Er enthält keine Geheimnisse.
- [x] Admins sehen das Audit-Log in der App, filterbar nach Zeitraum, Benutzer und Entität. Andere Rollen sehen es nicht.
- [x] Die App bietet keine Möglichkeit, Einträge zu ändern oder zu löschen.

## Aufgaben

- [x] ADR: Ansatz (Handler, Interceptor oder Kombination) und Schutz gegen Änderung
- [x] Modell `AuditEntry`, Migration
- [x] Schreiben (Interceptor und/oder `IAuditLog`), aktueller User aus dem `HttpContext`, System-Jobs als «System»
- [x] Benutzerverwaltung umstellen
- [x] Query und API für das Protokoll, Seite «Protokoll» für Admins
- [x] Tests: Eintrag pro Änderung, gleiche Transaktion (Fehler = kein Eintrag, keine Änderung), keine Geheimnisse, nur Admins
- [x] `security.md` und `architecture.md` ergänzen

## Definition of Done

- [x] Applikation buildet, automatische Tests laufen grün
- [x] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [x] `README.md` und `/specs` aktualisiert, falls nötig
- [x] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [x] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [x] Story aus `/specs/backlog.md` entfernt
