# Story: #016, Apple-Abrechnung automatisch abholen

**Status:** Open

## Beschreibung

*Flowtr* holt nach jedem Monat die Abrechnung von App Store Connect selbst ab. Im Eingang steht dann ein fertiger Vorschlag für die Einnahme aus dem App Store, mit dem Finanzbericht von Apple als Beleg. Der User muss nur noch bestätigen.

## Hintergrund

- **Heute:** Der User öffnet in App Store Connect «Payments and Financial Reports», macht einen Screenshot oder lädt die Datei herunter ([#014](done/014-csv-als-beleg.md)) und erfasst die Einnahme von Hand.
- **Buchung** (Beispiel September 2026: 3 Verkäufe in der Schweiz 5.19 CHF, 2 im Euro-Raum 2.34 EUR, ausbezahlt 7.35 CHF am 29.10.2026):
  - **Eine Einnahme**, Kategorie «Umsatz und Honorare», Gegenpartei Apple, Datum letzter Tag des Monats der Verkäufe (nicht der Tag der Auszahlung). So stimmt das Jahr: Die Verkäufe vom Dezember zahlt Apple erst Ende Januar aus.
  - **Netto:** Apple verkauft im eigenen Namen und zahlt den Erlös nach Abzug der Provision aus. Es gibt keine Gebührenrechnung. *Flowtr* bucht den Erlös («Proceeds») ohne Ausgabe für die Provision. Mit dem Treuhänder klären, bevor die Story umgesetzt wird.
- **App Store Connect API:** `GET /v1/financeReports` liefert die monatlichen Finanzberichte als TSV (gzip). Nötig: Vendor Number, ein API-Schlüssel (`.p8`) mit der Rolle *Finance*, Key ID und Issuer ID. Die Anmeldung ist ein JWT (ES256), das *Flowtr* selbst signiert.
- **Geschäftsmonate von Apple:** Apple rechnet nach eigenen Geschäftsmonaten ab, nicht nach Kalendermonaten. Der Monat «September 2026» endet etwa Anfang Oktober. Der Bericht ist erst einige Tage danach verfügbar.
- **Wechselkurse:** Der Finanzbericht nennt die Beträge pro Währung. Den Betrag in CHF und den Kurs zeigt die Seite «Payments» erst bei der Auszahlung. Ob die API den Betrag in CHF liefert, ist offen.
- Vorbedingungen: [#014](done/014-csv-als-beleg.md) (CSV als Beleg) und [#015](015-stripe-abrechnung-automatisch.md) (Abholen per API, Eingang, Verwaltung, Job).

## Entscheide

- **Beleg:** der Finanzbericht des Monats, entpackt und sonst unverändert, als CSV-Beleg (#014).
- **Betrag:**
  - Liefert die API den Betrag in CHF (z.B. im Bericht `FINANCE_DETAIL`), nimmt *Flowtr* diesen.
  - Sonst: CHF-Beträge direkt. Fremdwährungen rechnet *Flowtr* mit dem eigenen Kurs am Monatsende um ([Journal](../journal.md), `IExchangeRates`). Der Vorschlag sagt dann: «Kurs von Flowtr. Apple zahlt mit eigenem Kurs aus, bitte mit der Auszahlung abgleichen.»
- **Datum:** letzter Tag des Kalendermonats, nach dem Apple den Geschäftsmonat benennt (September 2026 → 30.09.2026).
- **Beschreibung:** «App Store September 2026». Notiz: Anzahl Verkäufe pro Region und «netto nach Provision Apple».
- **Wann:** wie bei Stripe im stündlichen Job, aber vom 5. bis 15. Tag des Folgemonats (Bericht erst nach dem Ende des Geschäftsmonats). «Jetzt abholen» in der Verwaltung für jeden Monat.
- **Ohne Verkäufe:** kein Vorschlag. Die Verwaltung zeigt den Monat als «keine Verkäufe».
- **Schlüssel:** `.p8` im Key Vault (`appstore-api-key`). Key ID, Issuer ID und Vendor Number sind keine Geheimnisse: Konfiguration. Nie im Log.

## Akzeptanzkriterien

- [ ] Mit einem API-Schlüssel (Rolle Finance) holt *Flowtr* den Finanzbericht für September 2026. Im Eingang steht eine Einnahme mit Datum 30.09.2026, Kategorie, Gegenpartei Apple und einem Betrag in CHF. Liefert Apple den Betrag in CHF, ist es 7.35.
- [ ] Der Beleg ist der Finanzbericht, lässt sich als Tabelle ansehen und kommt unverändert in den Export.
- [ ] Der Job holt den Bericht des Vormonats ohne Zutun, sobald Apple ihn bereitstellt. In den übrigen Stunden weckt er die Datenbank nicht.
- [ ] «Jetzt abholen» holt einen gewählten Monat. Ein zweites Abholen gibt keine Doppel.
- [ ] Ohne Schlüssel läuft *Flowtr* wie heute. Ein falscher Schlüssel gibt eine klare Meldung in der Verwaltung.

## Aufgaben

- [ ] Mit dem Treuhänder klären: Erlös netto buchen oder brutto mit Provision als Ausgabe
- [ ] API prüfen: Berichtstypen (`FINANCIAL`, `FINANCE_DETAIL`), Betrag in CHF, Verfügbarkeit nach dem Geschäftsmonat. Mit dem Bericht von September 2026 vergleichen (7.35 CHF)
- [ ] Client der App Store Connect API mit JWT (ES256) hinter einem Interface
- [ ] Bericht lesen: Summe pro Währung, Umrechnung, Notiz
- [ ] Quelle «Apple» im Job und in der Verwaltung (Grundlage aus #015)
- [ ] Deployment: Secret im Key Vault, Konfiguration, [Betrieb](../../docs/betrieb.md)
- [ ] Tests: Bericht mit CHF und EUR, ohne Verkäufe, Doppel, falscher Schlüssel
- [ ] Specs: [Konzept](../concept.md), [Journal](../journal.md) (App Store netto), ADR aus #015 (Nachtrag)

## Definition of Done

- [ ] Applikation buildet, automatische Tests laufen grün
- [ ] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [ ] `README.md` und `/specs` aktualisiert, falls nötig
- [ ] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [ ] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [ ] Story aus `/specs/backlog.md` entfernt
