# Story: #015, Stripe-Abrechnung automatisch abholen

**Status:** Open

## Beschreibung

*Flowtr* holt nach jedem Monat die Abrechnung von Stripe selbst ab. Im Eingang stehen dann zwei fertige Vorschläge, eine Einnahme (brutto) und eine Ausgabe für die Gebühren, mit dem Bericht von Stripe als Beleg. Der User muss nur noch bestätigen.

## Hintergrund

- **Heute:** Der User öffnet in Stripe jeden Monat *Berichte → Saldenübersicht*, exportiert sie als PDF und erfasst damit zwei Buchungen. Er lädt das PDF zweimal hoch und korrigiert jedes Mal die Vorschläge der KI.
- **Buchung nach dem Bruttoprinzip** (Beispiel September 2026: 2 Zahlungen 198.00, 1 Rückerstattung −99.00, Gebühren 7.84):
  - **Einnahme:** Zahlungen minus Rückerstattungen (99.00). Kategorie «Umsatz und Honorare», Gegenpartei Stripe, Datum Monatsende.
  - **Ausgabe:** alle Gebühren (7.84). Kategorie «Bank- und Kreditkartenspesen», Gegenpartei Stripe, Datum Monatsende.
  - **Keine Buchung für Auszahlungen:** Eine Auszahlung ist eine Umbuchung von Stripe auf die Bank, keine Einnahme.
  - Hat ein Monat keine Zahlungen, gibt es nur die Ausgabe (Beispiel August 2026: Gebühren 0.63). Ist ein Betrag 0, gibt es keinen Vorschlag dafür.
- **Stripe-API:** Ein *Restricted Key* nur mit Leserechten genügt. Zwei Wege:
  - **Reporting API** (bevorzugt): Report Runs liefern dieselben Berichte wie das Dashboard als CSV, z.B. `balance.summary.1` und `balance_change_from_activity.itemized.3`. Ein Report Run läuft asynchron (Sekunden bis Minuten). Die Datei kommt von Stripe selbst: ein echter Fremdbeleg.
  - **Balance Transactions** (`GET /v1/balance_transactions`, gefiltert nach Monat): sofort verfügbar, aber JSON. *Flowtr* müsste den Beleg selbst darstellen.
- **Datenbank schlafen lassen** ([#011](011-azure-deployment.md)): Der Job darf die Datenbank nicht stündlich wecken, nur wenn es etwas zu tun gibt.
- Vorbedingung: [#014](done/014-csv-als-beleg.md) (CSV als Beleg). Grundlage für [#016](016-apple-abrechnung-automatisch.md) (Apple).

## Entscheide

- **Quelle des Belegs:** Reporting API. Beleg ist der Bericht «Itemized balance change from activity» des Monats als CSV, unverändert. Dazu die Saldenübersicht (`balance.summary.1`) als zweite Datei. Beide Dateien gehören zu beiden Buchungen. Bestätigt sich die Reporting API bei der Umsetzung nicht (Rechte, Verfügbarkeit), dann Balance Transactions: Die Antwort von Stripe als JSON ist das Original, ein PDF von *Flowtr* dient zum Ansehen (wie Mail und PDF, [ADR-009](../adrs/009-email-eingang-mail-als-beleg.md)).
- **Beträge:** *Flowtr* rechnet sie aus dem Bericht, ohne KI. Die Vorschläge sind fest: Art, Kategorie, Gegenpartei, Datum (letzter Tag des Monats), Beschreibung «Stripe Einnahmen September 2026» bzw. «Stripe Gebühren September 2026». Notiz bei der Einnahme: Anzahl und Summe der Zahlungen und Rückerstattungen.
- **Monat:** Kalendermonat in der Zeitzone Europe/Zurich, wie im Dashboard eingestellt. Nur CHF. Ein Saldo in einer anderen Währung erscheint als Warnung im Vorschlag.
- **Wann:** Der stündliche Job ([#011](011-azure-deployment.md)) prüft am 2. bis 7. Tag des Monats, je einmal am Tag um 06:00, ob die Abrechnung des Vormonats fehlt. Nur dann weckt er die Datenbank. Ein Report Run, der noch läuft, wird im nächsten Lauf abgeholt.
- **Von Hand:** In der Verwaltung kann der User einen Monat wählen und «Jetzt abholen» klicken, auch für vergangene Monate (z.B. Januar bis Oktober 2026 nachholen).
- **Eingang:** Herkunft «Stripe» statt Absender. Ein Monat gibt ein bis zwei Einträge. Diese Einträge verfallen nicht nach 30 Tagen: Es sind erwartete Buchungen. Der User kann sie verwerfen.
- **Doppel:** pro Quelle und Monat höchstens einmal. Ein neues Abholen ersetzt nur Einträge, die noch nicht gebucht sind. Gebuchte Monate zeigt die Verwaltung als «gebucht».
- **Schlüssel:** im Key Vault (`stripe-api-key`), lokal in den User Secrets. Nie in der Datenbank, nie im Log. Ohne Schlüssel ist die Funktion aus. Die Verwaltung zeigt nur «eingerichtet» oder «nicht eingerichtet».
- **Neuer ADR:** Abrechnungen per API abholen (Quellen, Belege aus Daten, Ablauf im Job).

## Akzeptanzkriterien

- [ ] Mit einem Restricted Key (nur lesen) holt *Flowtr* die Abrechnung für September 2026. Im Eingang stehen eine Einnahme 99.00 und eine Ausgabe 7.84, beide mit Datum 30.09.2026, Kategorie und Gegenpartei.
- [ ] Für August 2026 steht im Eingang nur die Ausgabe 0.63.
- [ ] Jeder Eintrag hat den Bericht von Stripe als Beleg. Der Beleg lässt sich ansehen, und er kommt unverändert in den Export.
- [ ] Der Job holt die Abrechnung des Vormonats ab dem 2. Tag des Monats ohne Zutun. In den übrigen Stunden weckt er die Datenbank nicht.
- [ ] «Jetzt abholen» in der Verwaltung holt einen gewählten Monat. Ein zweites Abholen gibt keine Doppel.
- [ ] Ohne Schlüssel läuft *Flowtr* wie heute. Ein falscher Schlüssel gibt eine klare Meldung in der Verwaltung und einen Eintrag im Log ohne Schlüssel.

## Aufgaben

- [ ] Stripe-API prüfen: Rechte des Restricted Key, Report Types, Dauer eines Report Run. Beträge gegen die Saldenübersicht von August und September 2026 prüfen
- [ ] ADR: Abrechnungen per API abholen
- [ ] Modell: Abholung pro Quelle und Monat (Stand, Zeitpunkt, Fehler). Eingang: Herkunft «Abrechnung» mit Quelle und Monat, kein Verfall
- [ ] Client der Stripe-API hinter einem Interface (Tests ohne Stripe)
- [ ] Beträge aus dem Bericht rechnen: Zahlungen, Rückerstattungen, Gebühren, Auszahlungen ignorieren
- [ ] Job: Prüfung am 2. bis 7. Tag, Datenbank nur bei Bedarf wecken, laufende Report Runs im nächsten Lauf abholen
- [ ] Verwaltung: Stand pro Monat, «Jetzt abholen»
- [ ] Deployment: Secret im Key Vault, `deploy.sh` fragt optional danach, [Betrieb](../../docs/betrieb.md)
- [ ] Tests: Beträge (Rückerstattung, nur Gebühren, leerer Monat), Doppel, Job ohne Arbeit weckt die Datenbank nicht, falscher Schlüssel
- [ ] Specs: [Konzept](../concept.md) (neuer Eingangskanal), [Journal](../journal.md) (Bruttoprinzip bei Zahlungsanbietern), [Security](../security.md) (Schlüssel)

## Definition of Done

- [ ] Applikation buildet, automatische Tests laufen grün
- [ ] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [ ] `README.md` und `/specs` aktualisiert, falls nötig
- [ ] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [ ] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [ ] Story aus `/specs/backlog.md` entfernt
