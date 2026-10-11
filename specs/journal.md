# Journal & Export

## Inhalt
Das Journal (aka Milchbüchlein) muss alle Angaben enthalten, damit die Schweizer Steuererklärung (z.B. Aargau) damit gemacht werden kann. Wenn ein Treuhänder die Steuererklärung macht, muss es auch für diesen klar sein.

- Jede Buchung referenziert ihren nummerierten Beleg (siehe [Konzept](concept.md#ablage-der-belege)).
- Über das Journal sollen Abfragen und Analysen möglich sein → echte Datenbank.

## Felder einer Buchung
| Feld | Bemerkung |
| :-- | :-- |
| Datum | Belegdatum |
| Art | Einnahme oder Ausgabe |
| Betrag & Währung (Original) | wie auf dem Beleg, z.B. USD 20.00 |
| Betrag CHF | per Tageskurs vorgeschlagen, manuell korrigierbar (z.B. gemäss Kreditkartenabrechnung) |
| Beschreibung | |
| Kategorie | siehe [Kategorien](#kategorien) |
| Gegenpartei | Lieferant bzw. Kunde (z.B. Microsoft, Stripe) |
| Zahlungsart/Konto | z.B. Kreditkarte, Bankkonto, bar |
| Belegnummer | Referenz auf den abgelegten Beleg |
| Notiz | Freitext |

**MWST:** Wird aktuell nicht benötigt. Das Datenmodell muss MWST-Satz und -Betrag aber später ohne Umbau aufnehmen können.

## Kategorien
- Vordefinierte Startliste, angelehnt an das Formular «Selbständige Erwerbstätigkeit» der Steuererklärung (z.B. Material, Software/Abos, Hardware, Raumkosten, Fahrzeug, Honorare).
- Admins können Kategorien ergänzen.

## Bearbeitung & Abschluss
- Abgeschlossen wird pro **Kalenderjahr** (= Steuerperiode).
- Das Journal kann nachbearbeitet werden, solange das Jahr noch nicht abgeschlossen ist.
- Danach ist das Jahr read-only (= Archiv), inkl. der zugehörigen Belege (siehe [Sicherheit](security.md)).

## Export
- Auf Knopfdruck: Journal sowie alle dazugehörigen Belege – digital (Excel + Belegverzeichnisse).
- Journal als PDF und Excel exportierbar.
- Der Datumsbereich muss jeweils gewählt werden können.

## Dossier für den Treuhänder
Statt eines Downloads kann dem Treuhänder ein Link zum *Dossier* (= Journal + Belege) gesendet werden, über den er alles direkt herunterladen kann.

- Kein rein öffentlicher Link.
- Vor jedem Download erhält der Treuhänder einen Code per Email, den er eingeben muss, um den Download freizuschalten.
- Ein Link ist **14 Tage** gültig und kann jederzeit manuell widerrufen werden.
