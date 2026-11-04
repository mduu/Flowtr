# Story: #018, Statistik

**Status:** Done (04.11.2026)

## Beschreibung

Eine Seite «Statistik» zeigt die Entwicklung der letzten Monate: Einnahmen, Ausgaben und Saldo pro Monat und dazu die kumulierte Summe.

## Hintergrund

- Das Journal zeigt die Summen eines Jahres. Den Verlauf über die Monate und über den Jahreswechsel zeigt es nicht.
- Diagramme: eigene Komponente (`MonthChart`) aus HTML und CSS, ohne Bibliothek. `MudChart` zeigt nur gruppierte Balken und Linien in Standardfarben: zu unübersichtlich.

## Entscheide

- **Zeitraum:** die letzten 6, 12, 24 oder 36 Monate, der laufende Monat zuletzt. Standard: 12 Monate.
- **Kopf:** Saldo des Zeitraums gross, grün im Plus, rot im Minus. Dazu Summe der Einnahmen und Ausgaben und der Saldo im Durchschnitt pro Monat.
- **Cashflow pro Monat:** Einnahmen als Balken nach oben, Ausgaben nach unten. Der Saldo (Einnahmen minus Ausgaben) ist eine Linie mit einem Punkt pro Monat. Monate ohne Buchungen zeigen 0.
- **Saldo über die Zeit:** ein Balken pro Monat mit dem kumulierten Saldo am Monatsende, grün im Plus, rot im Minus. Die Summe beginnt am Anfang des Zeitraums, nicht am Jahresanfang.
- **Monat wählen:** Mit der Maus darüber oder Antippen. Beide Diagramme zeigen denselben Monat mit seinen Beträgen. Vorgewählt ist der letzte Monat mit Buchungen.
- **Beträge:** in CHF (`AmountChf`), nach Datum der Buchung.
- **Zugriff:** alle Rollen. Link als Symbol in der App-Leiste.
- **Monatsnamen:** höchstens 12 (Handy: 6), der laufende Monat immer.

## Akzeptanzkriterien

- [x] Die Seite `/statistik` zeigt für die letzten 12 Monate Einnahmen, Ausgaben und Saldo pro Monat.
- [x] Ein zweites Diagramm zeigt den kumulierten Saldo als Balken: wie viel wir im Plus oder im Minus sind.
- [x] Der Zeitraum lässt sich auf 6, 24 oder 36 Monate ändern.
- [x] Ohne Rolle gibt es keine Daten.

## Aufgaben

- [x] Abfrage `GetStatistics` mit Summen pro Monat, API `GET /api/statistics?months=12`
- [x] Seite mit zwei Diagrammen, Link in der App-Leiste
- [x] Tests: Monate ohne Buchungen, Summen und Kumulation, Grenzen des Zeitraums, über den Jahreswechsel, ungültiger Zeitraum, Rollen

## Definition of Done

- [x] Applikation buildet, automatische Tests laufen grün
- [x] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [x] `README.md` und `/specs` aktualisiert, falls nötig ([Journal](../../journal.md#statistik))
- [x] Architekturentscheide als ADR in `/specs/adrs` erfasst (keiner nötig)
- [x] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [x] Story aus `/specs/backlog.md` entfernt (war nicht im Backlog)
