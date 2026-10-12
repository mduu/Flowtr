# Story: #008, Beleg hochladen und ablegen

**Status:** Open

## Beschreibung

Der User lädt zu einer Buchung einen Beleg hoch (PDF oder Bild). Flowtr nummeriert ihn, benennt ihn um und legt ihn im Blob Storage ab. Im Journal öffnet der User den Beleg jeder Buchung.

## Hintergrund

- Ablage, Container und Namensschema: [ADR-001](../adrs/001-belegablage-in-blob-storage.md). Bestätigte Belege in `belege/{jahr}/einnahmen|ausgaben/E|A-{jahr}-{nr}_{bezeichnung}.{ext}`, unbestätigte Uploads in `eingang/`.
- [Konzept](../concept.md#ablage-der-belege): Belege 1:1 als Datei, Einnahmen und Ausgaben getrennt, der Dateiname ordnet den Beleg dem Journal zu.
- **Reihenfolge:** zuerst der Blob nach `belege/`, dann die Buchung ([ADR-001](../adrs/001-belegablage-in-blob-storage.md), Konsequenzen). Flowtr muss verwaiste Blobs erkennen (z.B. Prüf-Job mit Meldung).
- **Nummern:** fortlaufend pro Jahr und Art, ohne Lücken und ohne Doppel, auch bei gleichzeitigen Uploads (z.B. Sequenz-Tabelle mit Sperre). Das braucht einen Entscheid (ADR).
- **Upload:** Fortschritt anzeigen ([UX](../ux.md)). Erlaubt: PDF, JPEG, PNG, HEIC (iPhone). Grössenlimite festlegen (z.B. 20 MB). Der Server prüft den Inhalt, nicht nur die Endung.
- **Download:** als Stream über die API, nur mit Rolle. Keine öffentlichen Blob-URLs.
- **Abschluss:** Belege abgeschlossener Jahre sind read-only. Die Immutability-Policy setzt erst der Jahresabschluss (eigene Story).
- Dieser Upload ist die Basis für die KI-Extraktion ([#009](009-ki-extraktion.md)): dort entsteht die Buchung aus dem Beleg. Hier gehört der Beleg zu einer manuell erfassten Buchung ([#007](007-journal-manuell-erfassen.md)).
- Audit: Hochladen, Ersetzen und Entfernen eines Belegs gehen ins Audit-Log ([#006](done/006-audit-log.md)).

## Akzeptanzkriterien

- [ ] Beim Erfassen oder Bearbeiten einer Buchung lädt der User einen Beleg hoch. Er sieht den Fortschritt.
- [ ] Flowtr vergibt die nächste Belegnummer (`E-2026-001` bzw. `A-2026-001`), benennt die Datei nach Schema um und legt sie in `belege/` ab.
- [ ] Gleichzeitige Uploads erhalten nie dieselbe Nummer. Nummern haben keine Lücken.
- [ ] Im Journal öffnet ein Klick den Beleg (PDF/Bild im Browser), nur für angemeldete User mit Rolle.
- [ ] Ungültige Dateien (Typ, Grösse, Inhalt) lehnt Flowtr mit einer deutschen Meldung ab.
- [ ] Ein Beleg ohne Buchung (verwaist) wird erkannt und gemeldet.
- [ ] Belege abgeschlossener Jahre lassen sich nicht ersetzen oder entfernen.
- [ ] Auf dem iPhone lässt sich ein Foto direkt mit der Kamera aufnehmen und hochladen.

## Aufgaben

- [ ] ADR: Nummernvergabe ohne Lücken bei Parallelität
- [ ] Blob-Zugriff (Aspire `blobs`, Container anlegen, lokal Azurite)
- [ ] Commands: Beleg hochladen/zuordnen, ersetzen, entfernen. Query: Beleg herunterladen
- [ ] API mit Upload (Streaming, Limite) und Download
- [ ] UI: Upload-Control mit Fortschritt im Buchungsformular, Beleg-Link im Journal
- [ ] Prüf-Job für verwaiste Blobs
- [ ] Tests: Nummernvergabe (parallel), Namensschema, Dateiprüfung, Rollen, Sperre abgeschlossener Jahre
- [ ] `concept.md` und ADR-001 bei Bedarf ergänzen

## Definition of Done

- [ ] Applikation buildet, automatische Tests laufen grün
- [ ] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [ ] `README.md` und `/specs` aktualisiert, falls nötig
- [ ] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [ ] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [ ] Story aus `/specs/backlog.md` entfernt
