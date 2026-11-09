# Betrieb in Azure

*Flowtr* läuft in Azure Container Apps mit Scale to Zero und Azure SQL Free Tier ([ADR-010](../specs/adrs/010-hosting-auf-container-apps.md)). Ohne Nutzung kostet es praktisch nichts. Nach einer Pause dauert der Start bis zu einer Minute.

## Übersicht

| Ressource | Name | Zweck |
|---|---|---|
| Container App | `flowtr` | Die Web-App. 0 bis 1 Instanz. |
| Container Apps Job | `flowtr-inbox` | Holt einmal pro Nacht die Mails ab (nur mit Mailbox). Jede Mail weckt die Datenbank, darum nicht öfter. |
| Azure SQL | `sql-flowtr-…/flowtr` | Journal, Benutzer, Audit-Log. Free Tier, pausiert nach 15 Minuten. |
| Storage Account | `stflowtr…` | Container `belege`, `eingang`, `dataprotection`. Versionierung und Soft-Delete (30 Tage). |
| Key Vault | `kv-flowtr-…` | Geheimnisse (`anthropic-api-key`, `mailbox-password`) und der Key für Data Protection. |
| Managed Identity | `id-flowtr` | Zugriff von App und Job auf SQL, Storage und Key Vault. Kein Passwort. |
| Log Analytics, Application Insights | `log-flowtr`, `appi-flowtr` | Logs, 30 Tage, Tageslimit 100 MB. |
| Alerts, Budget | `ag-flowtr`, `budget-flowtr` | E-Mail bei Fehlern, wenig Gratis-Kontingent der Datenbank und Kosten. |

Locks verhindern das Löschen von SQL-Server und Storage Account.

## Kosten

| Posten | Pro Monat |
|---|---|
| Container Apps (App und Job) | 0, im Gratis-Kontingent (180'000 vCPU-Sekunden pro Subscription, ca. 100 Stunden App) |
| Azure SQL Free Tier | 0 (100'000 vCore-Sekunden, 32 GB) |
| Storage, Key Vault | wenige Rappen |
| Log Analytics | 0 (5 GB pro Monat gratis, Tageslimit 100 MB) |
| Alerts | ca. CHF 0.50 (eine Log-Regel, eine Metrik-Regel) |
| GitHub Container Registry | 0 (öffentliches Image) |
| Claude (Anthropic) | nach Anzahl Belege, separat bei Anthropic |

Das Budget warnt bei 80 % und bei einer Prognose über 100 % (Standard: 5 pro Monat).

## Einrichten (einmal)

Voraussetzungen: `az` (Azure CLI), `gh` (GitHub CLI), `jq`. Eine eigene Subscription nur für *Flowtr*: Das Gratis-Kontingent von Container Apps gilt pro Subscription.

1. **Image:** Ein Push auf `main` baut das Image `ghcr.io/<owner>/flowtr`. Bei einem öffentlichen Repository ist es öffentlich. Prüfen: `docker manifest inspect ghcr.io/<owner>/flowtr:latest` ohne Anmeldung. Ist es privat, in GitHub unter «Packages → flowtr → Package settings» auf **Public** stellen. Sonst kann Azure es nicht laden.
2. **Einstellungen:** `deploy/flowtr.env.example` nach `deploy/flowtr.env` kopieren und ausfüllen (Subscription, Admin, Domain, Mailbox). Die Datei kommt nicht ins Repository.
3. **Anmelden:** `az login` und `gh auth login`.
4. **Deployen:** `deploy/deploy.sh`. Das Skript
   - registriert die Resource Provider und legt die Ressourcengruppe an,
   - legt die Entra-Gruppe `flowtr-sql-admins` an (du und die Identity von *Flowtr*, kein SQL-Passwort),
   - deployt `deploy/main.bicep`,
   - fragt nach dem Anthropic-API-Key und dem Passwort der Mailbox und legt sie in den Key Vault,
   - erlaubt GitHub Actions per OIDC, neue Images auf App und Job zu setzen, und setzt die Variablen im Repository,
   - legt für den Workflow «Infrastructure» eine eigene Identity (Owner auf der Ressourcengruppe) und das Environment `azure-infra` mit seinen Variablen an. Nur `main` darf dieses Environment brauchen.
5. **Domain** (optional): Das Skript zeigt am Ende einen CNAME- und einen TXT-Eintrag. Beide beim DNS-Provider eintragen (bei Hostpoint: «Domains → DNS-Einträge»), dann `deploy/bind-domain.sh`. Das Zertifikat ist gratis und erneuert sich selbst.
6. **Erster Admin:** *Flowtr* öffnen. Beim ersten Start schreibt *Flowtr* für jede Adresse in `ADMIN_EMAIL` einen Einladungslink ins Log (Application Insights, «Logs»: `traces | where message has "Invitation link"`). Link öffnen, Passwort setzen, Passkey einrichten. Passkeys gelten nur für die Domain, auf der sie eingerichtet sind.

`deploy/deploy.sh` darf jederzeit wieder laufen, zum Beispiel nach einer Änderung an `deploy/flowtr.env`. Es ändert nur, was anders ist. Geheimnisse, die es schon gibt, fragt es nicht neu ab.

## Infrastruktur ändern

Der Workflow «Infrastructure» (`.github/workflows/infra.yml`) deployt `deploy/main.bicep`. Er läuft bei jedem Push auf `main`, der `deploy/main.bicep` ändert. Von Hand: «Actions → Infrastructure → Run workflow». Mit «what-if» zeigt er nur die Änderungen. Er behält das Image, das gerade läuft. Pull Requests prüfen nur die Bicep-Datei und die Skripte.

Die Einstellungen (Admin, Domain, Mailbox, Budget, Kultur) liegen als Variablen im Environment `azure-infra`. Nach einer Änderung an `deploy/flowtr.env` daher `deploy/deploy.sh` ausführen. Das Skript setzt die Variablen neu.

Optional: Unter «Settings → Environments → azure-infra» einen «Required reviewer» eintragen. Dann wartet jedes Deployment der Infrastruktur auf eine Freigabe.

## Updates

Jeder Push auf `main`, der die App ändert, baut nach grünen Tests ein neues Image. Änderungen nur an `specs/`, `docs/`, `deploy/`, `assets/` oder an `*.md` starten die Pipeline nicht. Das Image hat die Tags Commit und `latest`. Die Pipeline hat zwei Jobs: «Build and test» baut, testet und veröffentlicht das Image. «Deploy to Azure» setzt es auf App und Job. Danach wartet dieser Job, bis *Flowtr* bereit ist (`/status`). Dabei wacht die Datenbank auf, und *Flowtr* wendet die Migrationen an. GitHub behält die letzten 10 Images.

**Zurück auf eine frühere Version:** im Portal unter «Container App → Revisions» die frühere Revision aktivieren, oder `az containerapp update --image ghcr.io/<owner>/flowtr:<commit>`.

## Kultur

*Flowtr* formatiert und liest Zahlen und Datum nach `Culture` (Standard `de-CH`), unabhängig von der Spracheinstellung des Browsers. Ein Fork setzt bei Bedarf `CULTURE` in `deploy/flowtr.env` (z.B. `de-DE`) und führt `deploy/deploy.sh` aus. Ein unbekannter Wert stoppt den Start mit einer Fehlermeldung.

## Geheimnisse ändern

Container Apps liest Geheimnisse aus dem Key Vault nur etwa alle 30 Minuten neu. Ein Neustart allein genügt nicht. Nach dem Ändern den Verweis neu setzen und die App neu starten:

```bash
VAULT=<kv-flowtr-…>; SECRET=anthropic-api-key   # oder mailbox-password
read -rs VALUE && az keyvault secret set --vault-name "$VAULT" --name "$SECRET" --value "$VALUE" -o none && unset VALUE
REF="$SECRET=keyvaultref:https://$VAULT.vault.azure.net/secrets/$SECRET,identityref:$(az identity show -n id-flowtr -g rg-flowtr --query id -o tsv)"
az containerapp secret set -n flowtr -g rg-flowtr --secrets "$REF"
az containerapp job secret set -n flowtr-inbox -g rg-flowtr --secrets "$REF"
az containerapp revision restart -n flowtr -g rg-flowtr --revision "$(az containerapp show -n flowtr -g rg-flowtr --query properties.latestRevisionName -o tsv)"
```

Ein neuer API-Key für Claude gehört in einen eigenen Key für den Betrieb (Anthropic-Konsole), getrennt vom Key für die Entwicklung.

## Logs und Fehler

- **Application Insights** (`appi-flowtr`): Logs, Requests und Fehler von App (`flowtr`) und Job (`flowtr-inbox`). Nur Warnungen und Fehler der Frameworks, keine statischen Dateien, keine Daten aus Belegen.
- **Konsole** (Log Analytics, Tabelle `ContainerAppConsoleLogs`): nur Warnungen und Fehler, z.B. wenn ein Container nicht startet.
- **Job:** im Portal unter «Container Apps Job → Execution history». Ein Lauf ohne Mail dauert wenige Sekunden und weckt die Datenbank nicht.
- **Alerts** per E-Mail: Fehler in App oder Job (stündlich geprüft), weniger als 10 % Gratis-Kontingent der Datenbank, Budget.

## Datenbank

- Pausiert nach 15 Minuten ohne Verbindung. Der erste Zugriff weckt sie (bis ca. eine Minute). *Flowtr* zeigt solange «*Flowtr* startet …» bzw. «Die Datenbank wacht auf …».
- Das Gratis-Kontingent zeigt das Portal unter «SQL-Datenbank → Übersicht → Free monthly vCore amount». Ist es aufgebraucht, pausiert die Datenbank bis Ende Monat. Notfalls im Portal unter «Compute + storage» auf «Continue using database for additional charges» umstellen. Das lässt sich nicht rückgängig machen.
- **Zugriff von Hand:** Nur Mitglieder der Gruppe `flowtr-sql-admins` mit Entra-Anmeldung. Im Portal: «Query editor». Danach die Verbindung schliessen, sonst pausiert die Datenbank nicht.
- **Restore:** Point-in-Time-Restore bis 7 Tage zurück. Das Offsite-Backup folgt mit einer eigenen Story ([Backup](../specs/backup.md)).

## Für Forks

Eigene Subscription, eigene `deploy/flowtr.env` (Name, Region, Domain, Mailbox, `IMAGE` und `GITHUB_REPO` des Forks), dann wie oben. Das Image eines Forks baut dessen eigene Pipeline.
