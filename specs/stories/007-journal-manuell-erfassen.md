# Story: #007, Journal: Buchungen manuell erfassen

**Status:** Open

## Beschreibung

Der User erfasst, bearbeitet und löscht Buchungen (Einnahmen und Ausgaben) von Hand und sieht sie im Journal. Damit ist Flowtr zum ersten Mal nutzbar, noch ohne Beleg-Upload und KI.

## Hintergrund

- Felder einer Buchung: [Journal](../journal.md#felder-einer-buchung). Datum, Art, Betrag und Währung (Original), Betrag CHF, Beschreibung, Kategorie, Gegenpartei, Zahlungsart/Konto, Belegnummer, Notiz.
- **MWST:** noch nicht erfassen, aber das Modell nimmt Satz und Betrag später ohne Umbau auf (z.B. nullable Felder).
- **Beträge:** `decimal`, nie `double`. Anzeige im Schweizer Format (`CHF 1'234.50`, Datum `dd.MM.yyyy`). Der CHF-Betrag ist in dieser Story manuell. Der Vorschlag per Tageskurs folgt mit [#009](009-ki-extraktion.md).
- **Kategorien:** vordefinierte Startliste angelehnt an das Formular «Selbständige Erwerbstätigkeit» (Seed), Admins können ergänzen ([Journal](../journal.md#kategorien)).
- **Gegenparteien und Zahlungsarten:** eigene Listen, beim Erfassen neue Werte direkt anlegen (Autocomplete).
- **Belegnummer:** Die Nummer vergibt erst [#008](008-beleg-upload-und-ablage.md). Hier bleibt sie leer.
- **Abschluss:** Read-only nach dem Jahresabschluss ([Journal](../journal.md#bearbeitung--abschluss)). Der Abschluss selbst ist nicht Teil dieser Story. Das Modell soll ihn aber vorsehen (z.B. Tabelle der abgeschlossenen Jahre), damit Commands die Sperre von Anfang an prüfen.
- **Audit:** Jede Änderung geht ins Audit-Log ([#006](006-audit-log.md), vorher umsetzen).
- Rollen: Admin und Contributor dürfen erfassen. Wer Kategorien verwaltet: Admin.

## Akzeptanzkriterien

- [ ] Das Journal zeigt die Buchungen eines Jahres, neueste zuerst, mit Summen für Einnahmen, Ausgaben und Ergebnis in CHF.
- [ ] Filter: Jahr, Art, Kategorie, Gegenpartei, Freitext.
- [ ] Der User erfasst eine Buchung mit allen Feldern. Pflichtfelder: Datum, Art, Betrag CHF, Beschreibung, Kategorie.
- [ ] Der User bearbeitet und löscht eine Buchung (Löschen mit Rückfrage).
- [ ] Buchungen eines abgeschlossenen Jahres lassen sich weder ändern noch löschen (Server prüft).
- [ ] Admins verwalten Kategorien. Eine benutzte Kategorie lässt sich nicht löschen, nur deaktivieren.
- [ ] Erfassen geht auf dem iPhone schnell (Formular passt auf den Bildschirm, passende Tastaturen für Betrag und Datum).
- [ ] Jede Änderung steht im Audit-Log.

## Aufgaben

- [ ] Modell: `Booking`, `Category`, `Counterparty`, `PaymentMethod`, abgeschlossene Jahre. Migration mit Seed der Kategorien
- [ ] Commands/Queries nach CQS: erfassen, ändern, löschen, Journal abfragen (mit Filtern und Summen), Kategorien verwalten
- [ ] API unter `/api` (Rolle nötig)
- [ ] Seiten: Journal (Liste, Filter, Summen), Buchung erfassen/bearbeiten (Dialog oder eigene Seite), Kategorien (Admin)
- [ ] Navigation in der App (Journal als Startseite?)
- [ ] Tests: Pflichtfelder, Beträge (Rundung, Vorzeichen), Summen, Sperre abgeschlossener Jahre, Rollen, Audit
- [ ] `journal.md` und `glossary.md` ergänzen, Startliste der Kategorien festhalten

## Definition of Done

- [ ] Applikation buildet, automatische Tests laufen grün
- [ ] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [ ] `README.md` und `/specs` aktualisiert, falls nötig
- [ ] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [ ] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [ ] Story aus `/specs/backlog.md` entfernt
