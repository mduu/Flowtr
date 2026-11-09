# Story: #011, Deployment nach Azure

**Status:** Open

## Beschreibung

*Flowtr* läuft in Azure, so günstig wie möglich: Im Ruhezustand fallen praktisch keine Kosten an. Ein Push auf `main` baut, testet und deployt *Flowtr* ohne Handarbeit. Security-Header und ein Minimum an Backups sind ab dem ersten Tag aktiv.

## Hintergrund

- **Scope:** Eigengebrauch, eine Installation, ein bis zwei User ([Architektur](../architecture.md#prinzipien)). Andere forken das Repo und betreiben eine eigene Installation. Domain, Region und Namen sind deshalb Parameter, keine festen Werte.
- **Nutzung:** sporadisch, wenn Belege eintreffen oder wenn der User abrechnet. Eine Startzeit nach einer Pause ist in Ordnung (auch eine Minute).
- **Kosten:** absolutes Minimum. Ziel: unter CHF 2 pro Monat ohne die Kosten für Claude.
- **Eigene Subscription «Flowtr»** (angelegt am 01.11.2026), getrennt von den Nebenverdiensten (Feldnah, Solar Lens, Bau-Ticker) und den privaten Diensten. Grund: Das Gratis-Kontingent von Container Apps gilt pro Subscription. Andere Container Apps würden es mitbrauchen. Dazu: Kosten, Budget, Rechte und Locks nur für *Flowtr*. Die ID der Subscription steht nicht im Repository: Sie kommt als Variable in GitHub (`AZURE_SUBSCRIPTION_ID`) bzw. als Parameter für das Setup-Skript. Forks legen ihre eigene Subscription an.
- **Daten in der Schweiz:** Region Switzerland North. Belege gehen nur zur Analyse an Claude ([ADR-008](../adrs/008-ki-extraktion-mit-claude.md)).

### Hosting: Optionen

| | App Service F1 (Free) | App Service B1 | **Container Apps (Consumption)** |
|---|---|---|---|
| Kosten pro Monat | 0 | ca. CHF 11.60 (wie beim Bau-Ticker) | **0** (im monatlichen Gratis-Kontingent) |
| Eigene Domain mit Zertifikat | nein (nur `*.azurewebsites.net`) | ja, gratis | **ja, gratis** |
| Limiten | 60 CPU-Minuten pro Tag, 165 MB Datentransfer pro Tag, 32 Bit | keine | 180'000 vCPU-Sekunden, 360'000 GiB-Sekunden, 2 Mio. Requests pro Monat gratis |
| Ruhezustand | entlädt die App nach ca. 20 Minuten | läuft immer (Always On) | **skaliert auf null** |
| Mailbox abholen | nur während die App läuft | Hintergrund-Job wie heute | **geplanter Job (Cron)** |
| Startzeit nach Pause | einige Sekunden | keine | ca. 10 bis 30 Sekunden |
| Deployment | Code | Code | Container-Image |

- **F1** scheidet aus: Das Limit von 165 MB pro Tag reicht knapp für den Download der Blazor-App nach einem Update und einige Belege. Eine eigene Domain gibt es nicht.
- **B1** ist am einfachsten (der Hintergrund-Job läuft wie heute), kostet aber ca. CHF 140 im Jahr für eine App, die meistens nichts tut.
- **Container Apps im Vergleich zu App Service:**
  - Vorteile: Ohne Nutzung läuft nichts und es kostet nichts. Geplante Jobs (Cron) sind eingebaut. Das Zertifikat für die eigene Domain ist gratis. Jede Version ist eine Revision: Zurück auf die vorherige Version geht mit einem Klick. Dasselbe Image läuft lokal und in Azure.
  - Nachteile: Startzeit nach einer Pause. Es braucht ein Container-Image und eine Registry. Das Dateisystem ist flüchtig (Data-Protection-Keys, siehe unten). Weniger Komfort beim Fehlersuchen als mit Kudu bzw. Log Stream von App Service (es gibt `az containerapp logs` und eine Konsole). Neu für uns: Feldnah und Bau-Ticker laufen auf App Service B1.
- **Gratis-Kontingent** pro Subscription und Monat: 180'000 vCPU-Sekunden, 360'000 GiB-Sekunden, 2 Mio. HTTP-Requests. Health Probes zählen nicht als Requests. Mit 0.5 vCPU und 1 GiB sind das **100 Stunden Laufzeit der App pro Monat**. Eine Sitzung kostet ihre Dauer plus ca. 5 Minuten, bis die App wieder auf null geht. Darüber hinaus ca. USD 0.000024 pro vCPU-Sekunde: Eine App, die rund um die Uhr läuft, käme auf ca. USD 25 pro Monat. Scale to Zero ist also die Bedingung.
- **Entschieden: Container Apps** mit Scale to Zero. Die App läuft nur, wenn jemand sie nutzt. Das Abholen der Mails übernimmt ein **geplanter Job** (Container Apps Job, einmal pro Nacht) mit demselben Image: Er startet, holt die Mails ab, lässt neue Belege von der KI lesen und beendet sich. Die Rechnungen liegen also schon fertig im Eingang, wenn der User *Flowtr* öffnet. Der Job läuft in einem eigenen Container: Er weckt die App nicht und die Datenbank nur, wenn Mails da sind. Schätzung: 720 Läufe pro Monat × 60 s × 0.25 vCPU = ca. 11'000 vCPU-Sekunden (6 % des Kontingents). Mit 30 Stunden Nutzung der App pro Monat (54'000 vCPU-Sekunden) bleibt mehr als die Hälfte frei.
- **Image:** `dotnet publish /t:PublishContainer` (ohne Dockerfile) in die GitHub Container Registry. Das Repo ist öffentlich, deshalb ist das Image öffentlich. Speicher und Datentransfer für öffentliche Images sind bei GitHub gratis, ohne Limite (GitHub kündigt Änderungen einen Monat vorher an). Private Images wären auf 500 MB begrenzt, zusammen mit den Artefakten von Actions. Es enthält keine Geheimnisse, und Forks können es direkt nutzen. Azure Container Registry (ab ca. USD 5 pro Monat) ist nicht nötig.

### Datenbank: Azure SQL Free Tier

- Serverless General Purpose mit Free-Angebot: pro Monat 100'000 vCore-Sekunden und 32 GB gratis, unbefristet.
- Pausiert nach **60 Minuten** ohne Verbindung (beim Free Offer fest, Azure lehnt andere Werte ab). Der erste Zugriff danach weckt sie (bis ca. eine Minute). Die Retry-Logik von EF Core fängt das ab.
- Beim Erreichen des Limits: **Pause bis zum Monatsende** (keine Kosten). Ein Alert warnt bei 10 % Rest. Bei Bedarf lässt sich auf «weiter mit Kosten» umstellen. Das lässt sich nicht rückgängig machen.
- Einschränkungen: kein SLA, Point-in-Time-Restore nur 7 Tage, Backups nur lokal redundant. Für den Eigengebrauch genügt das. Das Offsite-Backup ([Backup](../backup.md)) wird dadurch wichtiger (eigene Story).
- **Wichtig:** Der heutige `InboxWorker` fragt die Datenbank alle 5 Minuten ab (`AnalyzeInbox`, `ExpireInboxItems`). So würde sie nie pausieren und das Gratis-Kontingent wäre nach wenigen Tagen aufgebraucht. **Regel: Die Datenbank nur wecken, wenn es etwas zu tun gibt**, also wenn Mails in der Mailbox liegen oder ein User *Flowtr* nutzt.

### Domain

- *Flowtr* braucht keine eigene Domain. Die generierte Adresse (`flowtr.<zufall>.switzerlandnorth.azurecontainerapps.io`) funktioniert auch.
- **Aber:** Ein Passkey gilt nur für die Domain, auf der er eingerichtet ist ([ADR-002](../adrs/002-passkeys-und-passwort.md)). Ändert sich die Adresse (neue Umgebung, andere Region, Wechsel des Hostings), sind alle Passkeys ungültig.
- **Empfehlung:** eine Subdomain von `flowtr.ch`, z.B. `app.flowtr.ch`. Sie kostet nichts zusätzlich (Zertifikat gratis) und bleibt stabil, auch wenn das Hosting ändert. `flowtr.ch` selbst bleibt frei, z.B. für eine Projektseite mit Link zum Repo. Die Adresse ist kein Geheimnis: Zugang gibt es nur auf Einladung ([ADR-004](../adrs/004-zugang-nur-auf-einladung.md)).
- Liegt die Domain bei Hostpoint, passt auch die Mailbox dazu, z.B. `belege@flowtr.ch`.
- Für Forks ist die Domain ein optionaler Parameter.
- **Entschieden:** `app.flowtr.ch`. Das DNS liegt bei Hostpoint: ein CNAME auf die Adresse der App und ein TXT-Eintrag `asuid.app` zur Prüfung. Das Zertifikat prüft per CNAME, die App muss dafür nicht laufen.

### Aufbau in Container Apps

- Ein **Container Apps Environment** (Consumption, ohne eigenes VNet, keine fixen Kosten) enthält zwei Ressourcen mit **demselben Image**:
  - **Container App** `flowtr`: die Web-App mit Ingress, `minReplicas: 0`, `maxReplicas: 1`.
  - **Container Apps Job** `flowtr-inbox`: geplant (Cron, einmal pro Nacht: die Mails eines Tages wecken die Datenbank nur einmal), ohne Ingress, mit eigenem Startbefehl. Jeder Lauf startet einen Container, der sich am Ende beendet.
- Beide nutzen dieselbe Managed Identity und dieselben Geheimnisse. Das Gratis-Kontingent gilt pro Subscription, App und Job teilen es.

### Start nach einer Pause

Bis zu drei Dinge müssen aufwachen: der Container (ca. 10 bis 30 Sekunden), die Datenbank (bis ca. eine Minute) und die Blazor-App im Browser (aus dem Cache des Browsers, schnell). Der User soll erkennen, dass *Flowtr* startet und nicht hängt.

- **Container:** Solange er startet, hält Container Apps den Request an. In dieser Zeit läuft noch kein Code von *Flowtr*. Der Browser zeigt nur, dass er lädt. Ziel: diese Zeit kurz halten (kleines Image, z.B. `aspnet:10.0-noble-chiseled`, ReadyToRun prüfen).
- **Datenbank aufwecken:** Beim Start der App öffnet ein Hintergrund-Dienst sofort eine Verbindung zur Datenbank, mit Retry bis ca. 2 Minuten. Ein Status im Speicher sagt, ob sie bereit ist.
- **Startseite:** Solange die Datenbank nicht bereit ist, beantwortet die App Aufrufe von Seiten mit einer leichten Seite «*Flowtr* startet …» (Logo, Fortschrittsbalken, «Die Datenbank wacht auf. Das dauert bis zu einer Minute.»). Die Seite fragt einen Status-Endpunkt ohne Datenbank ab und lädt neu, sobald *Flowtr* bereit ist. So hängt das Vorrendern nicht eine Minute ohne Rückmeldung.
- **API und Formulare:** Solange die Datenbank nicht bereit ist, wartet der Aufruf (höchstens 2 Minuten). So braucht der Client kein eigenes Retry, und kein Upload geht verloren.
- **Hinweis im Client:** Dauert ein Aufruf länger als 2 Sekunden, fragt der Client `GET /status`. Meldet der Server, dass die Datenbank aufwacht, zeigt der Client unten «Die Datenbank wacht auf …». Ein langsamer Aufruf aus einem anderen Grund (z.B. die KI) zeigt nichts. `/status` antwortet ohne Datenbank, vor der Authentifizierung (sie prüft das Login-Cookie gegen die Datenbank).
- **Pause während der Nutzung:** Die Datenbank pausiert nach 60 Minuten ohne Verbindung, auch wenn der Browser noch offen ist. Der nächste Aufruf wartet, und der Client zeigt denselben Hinweis. Die Retry-Logik von EF Core wartet (Fehler 40613 «Database not currently available» ist transient). Das Timeout ist so lang, dass ein Aufwachen hineinpasst.
- **Login** (statisches SSR, ohne Blazor-Interaktivität): Der Knopf zeigt nach dem Klick einen Spinner (kleines Skript wie bei den Passkeys). Ist die Datenbank nicht bereit, zeigt die Seite einen Hinweis statt eines Fehlers.
- **Job:** wartet auf die Datenbank nur, wenn Mails da sind, höchstens ca. 2 Minuten. Sonst bleiben die Mails in der Mailbox bis zum nächsten Lauf.

### Weitere Ressourcen (Kosten nahe null)

- **Storage Account** (Standard LRS): Belege, Eingang, Data-Protection-Keys. Versionierung und Soft-Delete für Blobs und Container.
- **Key Vault** (Standard): Anthropic-API-Key, Passwort der Mailbox, Key für Data Protection. Den API-Key und das Passwort trägt der User selbst ein, nicht die Pipeline.
- **Managed Identity** (user-assigned) für App und Job: Zugriff auf SQL (Entra-Authentifizierung, kein SQL-Passwort), Storage und Key Vault.
- **Log Analytics / Application Insights:** siehe [Logs](#logs).
- **Budget** in Cost Management (z.B. CHF 5 pro Monat) mit E-Mail-Alert.
- **Resource Locks** (`CanNotDelete`) auf SQL-Server und Storage Account: Kein versehentliches Löschen von Datenbank oder Belegen. Nicht auf der ganzen Ressourcengruppe: Änderungen an der übrigen Infrastruktur bleiben möglich.

### Was sich im Code ändert

- **Data Protection:** Die Keys liegen heute im Dateisystem. Ein Container verliert sie bei jedem Neustart, also nach jeder Pause. Danach sind alle Logins, Einladungslinks und Formulare ungültig. → Keys in den Blob Storage, geschützt mit einem Key aus dem Key Vault.
- **Mails abholen:** ein Einstiegspunkt für den Job (z.B. `Flowtr.Server --job inbox`): einmal abholen, neue Belege lesen lassen, beenden. Die Datenbank nur öffnen, wenn Mails da sind. Das Aufräumen (`ExpireInboxItems`) und das Lesen hängengebliebener Belege laufen, wenn die App läuft.
- **Migrationen** auch in Produktion beim Start (EF Core sperrt gleichzeitige Migrationen). Nur eine Instanz der App (`maxReplicas: 1`).
- **Forwarded Headers:** Container Apps beendet TLS vor der App. Ohne `X-Forwarded-Proto` stimmen Schema und Origin nicht, und Passkeys scheitern.
- **Health Checks:** Die Probes dürfen die Datenbank nicht wecken (nur Liveness, ohne DB).
- **Security-Header** ([Sicherheit](../security.md#web-sicherheit)):
  - Content-Security-Policy für Blazor WebAssembly und MudBlazor: `script-src 'self' 'wasm-unsafe-eval'`, `style-src 'self' 'unsafe-inline'`, `frame-ancestors 'none'` und weitere. Die Mail-Ansicht behält ihre eigene, strengere Policy.
  - HSTS ein Jahr, `X-Content-Type-Options: nosniff`, `Referrer-Policy`, `Permissions-Policy` (Kamera nur für «Foto aufnehmen»).

### Logs

Nur Fehler und Ereignisse, die etwas aussagen. Kein Rauschen, keine Daten aus der Buchhaltung. Übernommen aus Feldnah, Bau-Ticker und Solar Lens:

- **Log-Levels** in Produktion: `Default` Information für *Flowtr* selbst. `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore` und `System` auf Warning. `Microsoft.Hosting.Lifetime` auf Warning. `Azure.Core` auf Error, `Azure.Identity` auf Warning. `Polly` auf Warning.
- **`System.Net.Http.HttpClient` auf Warning:** `IHttpClientFactory` schreibt jeden Request in vier Zeilen, mit der URI. Bei Solar Lens war das vier Fünftel des Logs.
- **Keine Requests für statische Dateien:** `/_framework`, `/_content`, `/css`, `/js`, `/images`, `favicon`, `manifest`. Blazor lädt beim ersten Aufruf über hundert Dateien.
- **Keine Health Checks** in Traces und Logs.
- **Sampling:** Exceptions nie. Bei ein bis zwei Usern braucht es kein weiteres Sampling.
- **Keine System-Logs von Container Apps** (Revisionen, Skalierung). Konsolen-Logs nur für Fehler beim Start.
- **Keine Daten aus Belegen** in Logs: keine Beträge, kein Inhalt von Mails, keine Antwort der KI. Absender nur bei abgelehnten Mails.
- **Laufzeit-Metriken** (GC, Threads) nur in Development.
- **Log Analytics:** Tageslimit 100 MB, Aufbewahrung 30 Tage (gratis). Die ersten 5 GB pro Monat sind gratis.
- **Alerts** (per E-Mail): Fehler 5xx (keine 4xx: Scans von Bots), Job scheitert (z.B. Mailbox nicht erreichbar), wenig Gratis-Kontingent der Datenbank, Budget.

### Pipeline (GitHub Actions)

- Anmeldung an Azure per OIDC (Federated Credential): kein Geheimnis in GitHub.
- Ablauf auf `main`: Build, Tests, Image in die GitHub Container Registry, App und Job auf das neue Image setzen, warten bis *Flowtr* bereit ist. Das Image ist mit dem Commit (SHA) markiert. Kein Artefakt (AGENTS.md).
- Die Pipeline deployt kein Bicep: GitHub braucht in Azure nur Rechte auf App und Job (Contributor), keine Rechte für Rollen. Die Infrastruktur ändert selten: `deploy/deploy.sh` von Hand.
- Alte Image-Versionen regelmässig löschen (z.B. die letzten 10 behalten).
- Infrastruktur als Bicep in `deploy/`. `deploy/deploy.sh` richtet alles ein und darf wieder laufen: Ressourcengruppe, Federated Credential, Geheimnisse im Key Vault, DNS-Hinweise. `deploy/bind-domain.sh` bindet die Domain mit Zertifikat.

## Entscheide (vor der Umsetzung)

- [x] Hosting: Container Apps mit geplantem Job (01.11.2026). Auch, um Erfahrung damit zu sammeln.
- [x] Domain: `app.flowtr.ch` (01.11.2026)
- [x] DNS von `flowtr.ch` liegt bei Hostpoint (01.11.2026)
- [x] Mails abholen: stündlich, beim Öffnen von *Flowtr* und mit «Jetzt abholen» (01.11.2026)
- [x] Deployment automatisch bei jedem Push auf `main`, wenn die Tests grün sind (01.11.2026)

## Akzeptanzkriterien

- [ ] *Flowtr* läuft in Azure (Switzerland North) unter `app.flowtr.ch` mit gültigem Zertifikat.
- [ ] Ohne Nutzung laufen weder App noch Datenbank. Die Kosten bleiben unter CHF 2 pro Monat (ohne Claude). Das Budget mit Alert ist eingerichtet.
- [ ] Nach einer Pause lädt *Flowtr* ohne Fehler, auch wenn die Datenbank erst aufwachen muss. Solange sie aufwacht, sieht der User «*Flowtr* startet …» mit Fortschritt, nie eine Fehlerseite.
- [ ] Pausiert die Datenbank während der Nutzung, wartet der nächste Aufruf und zeigt einen Hinweis. Keine Daten gehen verloren.
- [ ] Eine weitergeleitete Rechnung liegt spätestens nach dem nächsten Lauf des Jobs mit KI-Vorschlag im Eingang. Ist die Mailbox leer, bleibt die Datenbank in Pause.
- [ ] Login mit Passwort und mit Passkey funktioniert. Nach einem Neustart bleibt der User angemeldet, und Einladungslinks bleiben gültig.
- [ ] Ein Push auf `main` deployt automatisch, nur wenn die Tests grün sind. In GitHub liegt kein Geheimnis.
- [ ] Zugangsdaten (Claude, Mailbox) liegen nur im Key Vault. Weder im Repository noch in der Pipeline.
- [ ] Die Security-Header sind gesetzt (geprüft z.B. mit securityheaders.com). Die App funktioniert damit vollständig.
- [ ] Belege haben Versionierung und Soft-Delete. Die Datenbank hat Point-in-Time-Restore. Locks verhindern das Löschen von Datenbank und Belegen.
- [ ] Die Logs enthalten keine Requests für statische Dateien, keine Health Checks und keine Daten aus Belegen. Fehler lösen einen Alert aus.
- [ ] Ein Fork kann *Flowtr* mit eigenen Parametern (Name, Region, Domain) deployen. Die Anleitung steht in `docs/`.

## Aufgaben

- [x] ADR: Hosting auf Container Apps mit Scale to Zero und geplantem Job (Kosten, Datenbank nur bei Bedarf wecken)
- [x] Data Protection: Keys in Blob Storage, geschützt mit Key Vault
- [x] Job-Einstiegspunkt für das Abholen der Mails. `InboxWorker` so ändern, dass er die Datenbank nur bei Bedarf öffnet. Tests
- [x] Produktion: Migrationen beim Start, Forwarded Headers, Health Checks ohne DB
- [x] Start nach einer Pause: Datenbank beim Start aufwecken, Startseite «*Flowtr* startet …», API und Formulare warten, Hinweis im Client bei langsamen Aufrufen, Spinner beim Login, Retry von EF Core (ca. 75 Sekunden). Tests mit einer Datenbank, die nicht erreichbar ist
- [x] Logs filtern (Levels, statische Dateien, Health Checks, keine Daten aus Belegen), Application Insights per OpenTelemetry, Alerts
- [x] Security-Header (Middleware). Tests für die Header. Prüfung im Browser, dass Blazor und MudBlazor damit laufen
- [x] Bicep in `deploy/`: Container Apps Environment, App, Job, SQL Free Tier, Storage, Key Vault, Managed Identity, Log Analytics, Budget, Locks
- [x] Setup-Skript `deploy/deploy.sh` (Resource Provider registrieren, z.B. `Microsoft.App`; Ressourcengruppe; Federated Credential; Geheimnisse; DNS-Hinweise)
- [x] GitHub Actions: Image bauen und pushen, deployen, alte Images löschen
- [x] Alert bei wenig restlichem Gratis-Kontingent der Datenbank
- [x] Dokumentation in `docs/`: Betrieb, Setup für Forks, Kosten. `README.md`, [Architektur](../architecture.md) und [Sicherheit](../security.md) nachführen. Im Kontext von [ADR-009](../adrs/009-email-eingang-mail-als-beleg.md) einen Nachtrag zum Hosting ergänzen
- [ ] Erstes Deployment. Passkeys auf der neuen Adresse einrichten. Test mit der echten Mailbox bei Hostpoint

## Umsetzung (Stand 01.11.2026, vor dem ersten Deployment)

- Entscheid: [ADR-010](../adrs/010-hosting-auf-container-apps.md). Betrieb: [docs/betrieb.md](../../docs/betrieb.md).
- **Start nach einer Pause:** `DatabaseStartup` verbindet und migriert im Hintergrund, aber erst wenn ein Request die Datenbank braucht. Eine Seite ohne Login, eine Datei und `/status` wecken sie nicht. Anfragen von Bots und Scannern (zum Beispiel `/wp-admin`, `/.env`, `*.php`) bekommen sofort 404. `robots.txt` sperrt alle Bots, jede Antwort hat `X-Robots-Tag: noindex`. Jedes Aufwachen kostet mindestens 60 Minuten × 0.5 vCore = 1'800 vCore-Sekunden. `DatabaseReadiness` kennt den Stand. `StartupGate` zeigt bis dahin «*Flowtr* startet …», lässt API und Formulare warten und beantwortet `/status` vor der Authentifizierung. `DatabaseWakeInterceptor` meldet langsame und gescheiterte Verbindungen. Im Client fragt `SlowCallHandler` nach 2 Sekunden `/status` und zeigt `ServerStatusBanner`. EF Core wiederholt ca. 75 Sekunden lang.
- **Job:** `InboxJob` (`--job inbox`) zählt zuerst die Mails, ohne Datenbank. Nur mit Mail: Datenbank vorbereiten (höchstens 3 Minuten), abholen, lesen lassen. Exit-Code 1 bei Fehlern. `InboxWorker` in der App analysiert nur beim Start, nach neuen Belegen und nach «Jetzt abholen», räumt einmal am Tag auf.
- **Data Protection:** Keys im Container `dataprotection`, mit `DataProtection:KeyVaultKey` geschützt.
- **Security-Header:** `SecurityHeaders` mit Nonce pro Request für die Import Map.
- **Logs:** Levels in `appsettings.json`, Filter in den Service Defaults, Azure Monitor über OpenTelemetry. Log-Meldungen ohne Betreff, Dateiname und Ablageort von Belegen.
- **Image:** `noble-chiseled-extra` (ICU und Zeitzonen für Schweizer Formate), ca. 276 MB. Lokal im Modus Production geprüft: App startet, Job ohne Mail fasst die Datenbank nicht an, Job mit Mail importiert.
- **Geprüft im Browser:** Blazor und MudBlazor laufen mit der Content-Security-Policy, Hinweis «Die Datenbank wacht auf …» bei gestoppter Datenbank, Login bleibt über Neustarts gültig.
- **Offen:** erstes Deployment, Domain, Test in Azure (Aufwachen der Datenbank, Kosten, Alerts, Passkeys, Mailbox bei Hostpoint).

## Nicht im Scope

- Offsite-Backup auf das Synology NAS und Restore-Test ([Backup](../backup.md)): eigene Story, bald danach
- E-Mail-Versand (Einladungen, Passwort-Reset)
- Eine zweite Umgebung (Staging): lokal mit Aspire und der Test-Mailbox testen

## Definition of Done

- [ ] Applikation buildet, automatische Tests laufen grün
- [ ] Bei UI-Änderungen: im Browser auf Mobile- und Desktop-Breite geprüft
- [ ] `README.md` und `/specs` aktualisiert, falls nötig
- [ ] Architekturentscheide als ADR in `/specs/adrs` erfasst
- [ ] Status auf "Done (DD.MM.YYYY)" gesetzt und Datei nach `/specs/stories/done/` verschoben
- [ ] Story aus `/specs/backlog.md` entfernt
