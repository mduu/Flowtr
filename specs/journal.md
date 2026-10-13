# Journal & Export

## Inhalt
Das Journal (aka Milchbüchlein) enthält alle Angaben für die Schweizer Steuererklärung (z.B. Aargau). Auch ein Treuhänder muss es klar verstehen.

- Jede Buchung referenziert ihren nummerierten Beleg (siehe [Konzept](concept.md#ablage-der-belege)).
- Abfragen und Analysen über das Journal müssen möglich sein. Darum liegt es in einer echten Datenbank.

## Felder einer Buchung
| Feld | Bemerkung |
| :-- | :-- |
| Datum | Belegdatum |
| Art | Einnahme oder Ausgabe |
| Betrag & Währung (Original) | Optional, nur bei Fremdwährung: wie auf dem Beleg, z.B. USD 20.00. Reine Information, Flowtr rechnet nur mit dem Betrag CHF |
| Betrag CHF | Immer positiv, die Art gibt die Richtung. Höchstens zwei Nachkommastellen. Vorschlag per Tageskurs folgt mit der KI-Extraktion. Manuell korrigierbar (z.B. gemäss Kreditkartenabrechnung) |
| Beschreibung | |
| Kategorie | siehe [Kategorien](#kategorien) |
| Gegenpartei | Lieferant oder Kunde (z.B. Microsoft, Stripe) |
| Zahlungsart/Konto | z.B. Kreditkarte, Bankkonto, bar |
| Belegnummer | Referenz auf den abgelegten Beleg |
| Notiz | Freitext |

**MWST:** Aktuell nicht nötig. Das Datenmodell muss MWST-Satz und -Betrag aber später ohne Umbau aufnehmen können.

## Kategorien
- Vordefinierte Startliste, angelehnt an das Formular «Selbständige Erwerbstätigkeit» der Steuererklärung:
  - **Einnahmen:** Umsatz und Honorare, Übrige Erträge
  - **Ausgaben:** Material und Waren, Fremdleistungen, Software und Abos, Hardware und Geräte, Raumkosten, Fahrzeug und Reisen, Telefon und Internet, Werbung und Marketing, Weiterbildung und Fachliteratur, Versicherungen und Gebühren, Bank- und Kreditkartenspesen, Übriger Aufwand
- Jede Kategorie gehört zu Einnahmen oder Ausgaben. Eine Buchung kann nur eine passende Kategorie haben.
- Admins können Kategorien ergänzen, umbenennen und deaktivieren («Verwaltung → Kategorien»). Eine Kategorie mit Buchungen lässt sich nicht löschen, nur deaktivieren. Bestehende Buchungen behalten sie.
- Zahlungsarten starten mit Bankkonto, Kreditkarte, Bar. Neue Gegenparteien und Zahlungsarten entstehen beim Erfassen.

## Bearbeitung & Abschluss
- Der Abschluss gilt pro **Kalenderjahr** (= Steuerperiode).
- Bis zum Abschluss kann der User das Journal des Jahres bearbeiten.
- Danach ist das Jahr read-only (= Archiv). Das gilt auch für seine Belege (siehe [Sicherheit](security.md)).

## Export
- Auf Knopfdruck: Journal und alle zugehörigen Belege, digital (Excel und Belegverzeichnisse).
- Export des Journals als PDF und Excel.
- Der User kann bei jedem Export den Datumsbereich wählen.

## Dossier für den Treuhänder
Statt eines Downloads kann der User dem Treuhänder einen Link zum *Dossier* (= Journal und Belege) senden. Über den Link lädt der Treuhänder alles direkt herunter.

- Kein rein öffentlicher Link.
- Vor jedem Download erhält der Treuhänder einen Code per Email. Erst mit diesem Code ist der Download frei.
- Ein Link ist **14 Tage** gültig. Der User kann ihn jederzeit widerrufen.
