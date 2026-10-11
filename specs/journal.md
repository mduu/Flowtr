# Journal & Export

## Inhalt
Das Journal (aka Milchbüchlein) muss alle Angaben enthalten, damit die Schweizer Steuererklärung (z.B. Aargau) damit gemacht werden kann. Wenn ein Treuhänder die Steuererklärung macht, muss es auch für diesen klar sein.

- Jede Buchung referenziert ihren nummerierten Beleg (siehe [Konzept](concept.md#ablage-der-belege)).
- Über das Journal sollen Abfragen und Analysen möglich sein → echte Datenbank.

> TBD: Konkrete Felder einer Buchung (Datum, Betrag, Währung, MWST, Kategorie, …) definieren.

## Bearbeitung & Abschluss
- Das Journal kann nachbearbeitet werden, solange es noch nicht für die Steuern abgegeben wurde.
- Danach ist der abgegebene Teil read-only (= Archiv).

## Export
- Auf Knopfdruck: Journal sowie alle dazugehörigen Belege – digital (Excel + Belegverzeichnisse).
- Journal als PDF und Excel exportierbar.
- Der Datumsbereich muss jeweils gewählt werden können.

## Dossier für den Treuhänder
Statt eines Downloads kann dem Treuhänder ein Link zum *Dossier* (= Journal + Belege) gesendet werden, über den er alles direkt herunterladen kann.

- Kein rein öffentlicher Link.
- Beispiel: Vor dem Download erhält der Treuhänder einen Code per Email, den er eingeben muss, um den Download freizuschalten.
