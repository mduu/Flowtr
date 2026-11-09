# ADR-010: Hosting auf Azure Container Apps mit Scale to Zero

## Status

**Akzeptiert** (01.11.2026)

## Kontext

*Flowtr* ist für den Eigengebrauch gebaut, mit ein bis zwei Usern ([Architektur](../architecture.md#prinzipien)). Der User braucht es sporadisch: wenn Belege eintreffen oder wenn er abrechnet ([Story #011](../stories/011-azure-deployment.md)).

- Die Kosten sollen auf einem absoluten Minimum bleiben. Eine Startzeit nach einer Pause ist in Ordnung, auch eine Minute.
- Der E-Mail-Eingang holt Mails regelmässig ab ([ADR-009](009-email-eingang-mail-als-beleg.md)). Die Rechnungen sollen fertig gelesen im Eingang liegen, wenn der User *Flowtr* öffnet.
- Die Daten sollen in der Schweiz liegen.
- Andere Betreiber forken das Repo: Die Installation muss mit Parametern funktionieren, ohne feste Werte.

## Entscheidung

Wir entscheiden uns für **Azure Container Apps (Consumption) mit Scale to Zero** und **Azure SQL Free Tier**, in einer eigenen Subscription.

- **App:** Container App mit 0 bis 1 Instanz. Ohne Nutzung läuft nichts.
- **E-Mail-Eingang:** Ein geplanter Container Apps Job (einmal pro Nacht) startet dasselbe Image mit `--job inbox`: einmal abholen, neue Belege lesen lassen, beenden. Dazu holt die App beim Start und mit «Jetzt abholen» ab.
- **Datenbank:** Azure SQL Free Tier (Serverless General Purpose, 100'000 vCore-Sekunden pro Monat). Sie pausiert nach 60 Minuten ohne Verbindung (beim Free Offer fest). Beim Erreichen des Limits pausiert sie bis Ende Monat, statt Kosten zu verursachen.
- **Regel: Die Datenbank nur wecken, wenn es etwas zu tun gibt.** Der Job öffnet sie nur, wenn Mails in der Mailbox liegen. Die Hintergrund-Jobs der App fragen sie nicht regelmässig ab.
- **Start nach einer Pause:** Die App weckt die Datenbank sofort und zeigt bis dahin «*Flowtr* startet …». API-Aufrufe warten. Der Client zeigt einen Hinweis, wenn ein Aufruf auf die Datenbank wartet.
- **Eigene Subscription:** Das Gratis-Kontingent von Container Apps gilt pro Subscription. Andere Container Apps würden es mitbrauchen.
- **Image:** `dotnet publish /t:PublishContainer` in die GitHub Container Registry (öffentlich, gratis).
- **Infrastruktur:** Bicep. `deploy/deploy.sh` richtet Azure einmal von Hand ein. Danach deployt der Workflow «Infrastructure» jede Änderung an `deploy/main.bicep`, mit einer eigenen Identity im Environment `azure-infra` (nur `main`). Die Pipeline setzt nur das neue Image auf App und Job: ihre Identity hat nur Rechte auf diese zwei Ressourcen.

## Optionen

### Option A: Container Apps mit Scale to Zero und geplantem Job (gewählt)

**Vorteile:**
- Ohne Nutzung keine Kosten: App und Job bleiben im Gratis-Kontingent (180'000 vCPU-Sekunden pro Monat, ca. 100 Stunden Laufzeit der App mit 0.5 vCPU)
- Geplante Jobs sind eingebaut
- Eigene Domain mit Zertifikat gratis
- Jede Version ist eine Revision: Zurück auf die vorherige Version mit einem Klick

**Nachteile:**
- Startzeit nach einer Pause (Container ca. 10 bis 30 Sekunden, Datenbank bis ca. eine Minute)
- Container-Image und Registry nötig
- Flüchtiges Dateisystem: Die Keys von Data Protection liegen im Blob Storage, geschützt mit Key Vault
- Weniger Komfort beim Fehlersuchen als App Service

### Option B: App Service B1

**Vorteile:**
- Einfachstes Deployment (Code), keine Startzeit, Hintergrund-Job wie lokal
- Bekannt aus anderen Projekten

**Nachteile:**
- Ca. CHF 11.60 pro Monat (CHF 140 im Jahr) für eine App, die meistens nichts tut

### Option C: App Service F1 (Free)

**Vorteile:**
- Gratis

**Nachteile:**
- Keine eigene Domain, 60 CPU-Minuten und 165 MB Datentransfer pro Tag, 32 Bit
- Entlädt die App nach ca. 20 Minuten: kein verlässliches Abholen der Mails

## Konsequenzen

### Positive Auswirkungen

- Kosten unter CHF 2 pro Monat ohne Claude. Budget mit Alert.
- Eine eigene Domain hält die Passkeys stabil, auch wenn das Hosting ändert.
- Forks deployen mit eigenen Parametern in ihre eigene Subscription.

### Negative Auswirkungen / Risiken

- Der Free Tier von Azure SQL hat kein SLA, Point-in-Time-Restore nur 7 Tage und lokal redundante Backups. Das Offsite-Backup ([Backup](../backup.md)) wird wichtiger.
- Ist das Gratis-Kontingent der Datenbank aufgebraucht, ist *Flowtr* bis Ende Monat nicht erreichbar. Ein Alert warnt bei 10 % Rest.
- Ändert sich das Gratis-Kontingent von Microsoft oder GitHub, steigen die Kosten.

### Aufwand

- Mittel: Startlogik, Job, Bicep, Skripte, Pipeline.

## Referenzen

- [Story #011: Deployment nach Azure](../stories/011-azure-deployment.md)
- [Betrieb](../../docs/betrieb.md)
- [Container Apps: Billing](https://learn.microsoft.com/en-us/azure/container-apps/billing)
- [Azure SQL Database free tier](https://learn.microsoft.com/en-us/azure/azure-sql/database/free-tier)
