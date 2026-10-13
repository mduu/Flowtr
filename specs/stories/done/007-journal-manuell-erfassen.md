# Story: #007, Journal: Buchungen manuell erfassen

**Status:** Done (13.10.2026)

## Beschreibung

Der User erfasst, bearbeitet und löscht Buchungen (Einnahmen und Ausgaben) von Hand und sieht sie im Journal. Damit ist Flowtr zum ersten Mal nutzbar, noch ohne Beleg-Upload und KI.

## Hintergrund

- Felder einer Buchung: [Journal](../../journal.md#felder-einer-buchung). Datum, Art, Betrag und Währung (Original), Betrag CHF, Beschreibung, Kategorie, Gegenpartei, Zahlungsart/Konto, Belegnummer, Notiz.
- **MWST:** noch nicht erfassen, aber das Modell nimmt Satz und Betrag später ohne Umbau auf (z.B. nullable Felder).
- **Beträge:** `decimal`, nie `double`. Anzeige im Schweizer Format (`CHF 1'234.50`, Datum `dd.MM.yyyy`). Der CHF-Betrag ist in dieser Story manuell. Der Vorschlag per Tageskurs folgt mit [#009](../009-ki-extraktion.md).
- **Kategorien:** vordefinierte Startliste angelehnt an das Formular «Selbständige Erwerbstätigkeit» (Seed), Admins können ergänzen ([Journal](../../journal.md#kategorien)).
- **Gegenparteien und Zahlungsarten:** eigene Listen, beim Erfassen neue Werte direkt anlegen (Autocomplete).
- **Belegnummer:** Die Nummer vergibt erst [#008](../008-beleg-upload-und-ablage.md). Hier bleibt sie leer.
- **Abschluss:** Read-only nach dem Jahresabschluss ([Journal](../../journal.md#bearbeitung--abschluss)). Der Abschluss selbst ist nicht Teil dieser Story. Das Modell soll ihn aber vorsehen (z.B. Tabelle der abgeschlossenen Jahre), damit Commands die Sperre von Anfang an prüfen.
- **Audit:** Jede Änderung geht ins Audit-Log ([#006](006-audit-log.md), vorher umsetzen).
- Rollen: Admin und Contributor dürfen erfassen. Wer Kategorien verwaltet: Admin.

## Umsetzung

- **Journal ist die Startseite** (`/`). Summen über die gefilterten Buchungen. Klick auf eine Zeile öffnet die Buchung.
- **Gruppiert nach Monat** (Wunsch des Users): Kopfzeile mit Monat und Anzahl (zuklappbar), Fusszeile mit Einnahmen, Ausgaben und Saldo des Monats. Auf dem iPhone kompakt: Beschreibung und Betrag, darunter Datum, Kategorie und Gegenpartei.
- **Buchungsdialog modern** (Wunsch des Users): Betrag gross auf einer Fläche in der Farbe der Art (rot/grün), Umschalter Ausgabe/Einnahme, Felder mit Symbolen, Fremdwährung und Notiz nur bei Bedarf. Auf dem iPhone als Vollbild.
- **Erfassen im Dialog:** Art als Umschalter (Ausgabe/Einnahme), Datum, Betrag CHF (Dezimal-Tastatur auf dem iPhone), Beschreibung, Kategorie (nur passende und aktive), Gegenpartei und Zahlungsart mit Autocomplete (neue Namen werden angelegt), Originalbetrag und Währung, Notiz. Auf dem iPhone stehen die Felder untereinander.
- **Modell:** `Booking`, `Category`, `Counterparty`, `PaymentMethod`, `ClosedYear` mit GUID v7-Schlüsseln (Audit, ADR-006). Beträge `decimal(18,2)`. MWST-Satz und -Betrag als leere Felder vorbereitet. `CreatedAt` sortiert Buchungen mit gleichem Datum.
- **Regeln** (`BookingRules`, für Erfassen und Ändern gleich): Betrag > 0 mit höchstens zwei Nachkommastellen (keine stille Rundung), Originalbetrag nur mit Währung (3 Buchstaben, nicht CHF: er ist reine Information zu Fremdwährungen), Kategorie passend zur Art. Eine bestehende Buchung darf ihre inzwischen deaktivierte Kategorie behalten.
- **Abschluss:** Tabelle `ClosedYears`. Erfassen, Ändern (alt und neu) und Löschen prüfen sie. Die UI zeigt das Jahr als abgeschlossen und öffnet Buchungen nur zum Ansehen. Das Abschliessen selbst folgt in einer eigenen Story.
- **Kategorien** unter «Verwaltung → Kategorien» (Admin): hinzufügen, umbenennen, (de)aktivieren, löschen wenn unbenutzt.
- **Audit:** Buchungen, Kategorien, Gegenparteien, Zahlungsarten und abgeschlossene Jahre haben Regeln. Das Protokoll zeigt Beträge, Daten, Kategorie- und Gegenpartei-Namen lesbar.
- **Aufgeräumt:** gemeinsamer HTTP-Helfer im Client (`Api/ApiResults`), gemeinsames Mapping `Result` → HTTP im Server (`CrossCutting/Api`), Test-Fixture `TestApp` für alle Feature-Tests.
- **Gefunden beim Testen:** Beim Vorrendern dürfen Abfragen nicht parallel laufen (ein DbContext). Das Namensfeld für neue Kategorien reagiert jetzt sofort.
- Geprüft in Chrome und WebKit: erfassen (mit Fremdwährung), Pflichtfelder, bearbeiten, suchen, löschen mit Rückfrage, Kategorien verwalten, Protokoll. Desktop und iPhone-Breite.

## Akzeptanzkriterien

- [x] Das Journal zeigt die Buchungen eines Jahres, neueste zuerst, mit Summen für Einnahmen, Ausgaben und Ergebnis in CHF.
- [x] Filter: Jahr, Art, Kategorie, Gegenpartei, Freitext.
- [x] Der User erfasst eine Buchung mit allen Feldern. Pflichtfelder: Datum, Art, Betrag CHF, Beschreibung, Kategorie.
- [x] Der User bearbeitet und löscht eine Buchung (Löschen mit Rückfrage).
- [x] Buchungen eines abgeschlossenen Jahres lassen sich weder ändern noch löschen (Server prüft).
- [x] Admins verwalten Kategorien. Eine benutzte Kategorie lässt sich nicht löschen, nur deaktivieren.
- [x] Erfassen geht auf dem iPhone schnell (Formular passt auf den Bildschirm, passende Tastaturen für Betrag und Datum).
- [x] Jede Änderung steht im Audit-Log.

## Aufgaben

- [x] Modell: `Booking`, `Category`, `Counterparty`, `PaymentMethod`, abgeschlossene Jahre. Migration mit Seed der Kategorien
- [x] Commands/Queries nach CQS: erfassen, ändern, löschen, Journal abfragen (mit Filtern und Summen), Kategorien verwalten
- [x] API unter `/api` (Rolle nötig)
- [x] Seiten: Journal (Liste, Filter, Summen), Buchung erfassen/bearbeiten (Dialog oder eigene Seite), Kategorien (Admin)
- [x] Navigation in der App (Journal als Startseite?)
- [x] Tests: Pflichtfelder, Beträge (Rundung, Vorzeichen), Summen, Sperre abgeschlossener Jahre, Rollen, Audit
- [x] `journal.md` und `glossary.md` ergänzen, Startliste der Kategorien festhalten

## Definition of Done

- [x] Applikation buildet, automatische Tests laufen grün
- [x] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [x] `README.md` und `/specs` aktualisiert, falls nötig
- [x] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [x] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [x] Story aus `/specs/backlog.md` entfernt
