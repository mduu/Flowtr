#!/usr/bin/env bash
# Deploys Flowtr to Azure (story #011, docs/betrieb.md). Run it again after each change to deploy/main.bicep:
# it changes only what is different. The pipeline sets new images on the app and the job.
#
# Needs: az (az login), gh (gh auth login), deploy/flowtr.env (see deploy/flowtr.env.example).
set -euo pipefail
cd "$(dirname "$0")"

# shellcheck source=/dev/null
source ./flowtr.env
: "${SUBSCRIPTION_ID:?Set SUBSCRIPTION_ID in deploy/flowtr.env}"
: "${ADMIN_EMAIL:?Set ADMIN_EMAIL in deploy/flowtr.env}"
NAME=${NAME:-flowtr}
RESOURCE_GROUP=${RESOURCE_GROUP:-rg-$NAME}
LOCATION=${LOCATION:-switzerlandnorth}
ALERT_EMAIL=${ALERT_EMAIL:-$ADMIN_EMAIL}
IMAGE=${IMAGE:-ghcr.io/mduu/flowtr:latest}
MONTHLY_BUDGET=${MONTHLY_BUDGET:-5}
ENVIRONMENT=cae-$NAME

step() { printf '\n\033[1m%s\033[0m\n' "$*"; }

step "Subscription"
az account set --subscription "$SUBSCRIPTION_ID"
TENANT_ID=$(az account show --query tenantId -o tsv)
OPERATOR_ID=$(az ad signed-in-user show --query id -o tsv)

step "Resource providers (a new subscription does not know them yet)"
for provider in Microsoft.App Microsoft.OperationalInsights Microsoft.Insights Microsoft.Sql Microsoft.Storage \
    Microsoft.KeyVault Microsoft.ManagedIdentity Microsoft.Consumption Microsoft.AlertsManagement; do
    az provider register --namespace "$provider" --wait -o none
done

step "Resource group $RESOURCE_GROUP"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --tags "application=$NAME" -o none

step "Entra group for the SQL admins (no SQL password)"
SQL_GROUP=$NAME-sql-admins
SQL_GROUP_ID=$(az ad group show --group "$SQL_GROUP" --query id -o tsv 2>/dev/null || true)
if [[ -z "$SQL_GROUP_ID" ]]; then
    SQL_GROUP_ID=$(az ad group create --display-name "$SQL_GROUP" --mail-nickname "$SQL_GROUP" --query id -o tsv)
fi
add_to_sql_group() {
    if [[ $(az ad group member check --group "$SQL_GROUP_ID" --member-id "$1" --query value -o tsv) != "true" ]]; then
        az ad group member add --group "$SQL_GROUP_ID" --member-id "$1"
    fi
}
add_to_sql_group "$OPERATOR_ID"

# The start of the budget cannot change: keep the start of an existing budget.
BUDGET_START=$(az rest --method get \
    --url "https://management.azure.com/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Consumption/budgets/budget-$NAME?api-version=2023-11-01" \
    --query properties.timePeriod.startDate -o tsv 2>/dev/null | cut -c1-10 || true)
BUDGET_START=${BUDGET_START:-$(date -u +%Y-%m-01)}

# After deploy/bind-domain.sh: without the certificate, a deployment would remove the domain from the app.
CERTIFICATE_ID=""
if [[ -n "${DOMAIN:-}" ]]; then
    CERTIFICATE_ID=$(az containerapp env certificate list --resource-group "$RESOURCE_GROUP" --name "$ENVIRONMENT" \
        --managed-certificates-only --query "[?properties.subjectName=='$DOMAIN'].id | [0]" -o tsv 2>/dev/null || true)
fi

deploy() {
    az deployment group create --resource-group "$RESOURCE_GROUP" --name "flowtr-$(date -u +%Y%m%d-%H%M%S)" \
        --template-file main.bicep \
        --parameters name="$NAME" image="$IMAGE" domain="${DOMAIN:-}" certificateId="$CERTIFICATE_ID" \
            adminEmail="$ADMIN_EMAIL" alertEmail="$ALERT_EMAIL" operatorId="$OPERATOR_ID" \
            sqlAdminGroupName="$SQL_GROUP" sqlAdminGroupId="$SQL_GROUP_ID" \
            mailboxHost="${MAILBOX_HOST:-}" mailboxUserName="${MAILBOX_USER:-}" mailboxAddress="${MAILBOX_ADDRESS:-}" \
            monthlyBudget="$MONTHLY_BUDGET" budgetStartDate="$BUDGET_START" deployApps="$1" \
        --query properties.outputs -o json
}

step "Infrastructure (without app and job)"
OUTPUTS=$(deploy false)
output() { jq -r ".$1.value" <<<"$OUTPUTS"; }
VAULT=$(output vaultName)
add_to_sql_group "$(output identityPrincipalId)"

step "Secrets in Key Vault $VAULT"
ask_secret() {
    local secret=$1 prompt=$2 default=${3:-}
    # The role for the secrets can take a minute to reach Key Vault.
    for _ in $(seq 1 12); do
        if az keyvault secret list --vault-name "$VAULT" -o none 2>/dev/null; then break; fi
        sleep 10
    done
    if az keyvault secret show --vault-name "$VAULT" --name "$secret" -o none 2>/dev/null; then
        echo "$secret: exists"
        return
    fi
    local value=$default
    if [[ -z "$value" && ! -t 0 ]]; then
        # No keyboard (for example started from another tool): the operator sets the secret, then runs the script again.
        echo "Missing secret $secret ($prompt). Set it, then run deploy/deploy.sh again:" >&2
        echo "  read -rs VALUE && az keyvault secret set --vault-name $VAULT --name $secret --value \"\$VALUE\" -o none" >&2
        exit 1
    fi
    if [[ -z "$value" ]]; then
        read -rsp "$prompt: " value
        echo
    fi
    az keyvault secret set --vault-name "$VAULT" --name "$secret" --value "$value" -o none
}
ask_secret anthropic-api-key "Anthropic API key"
if [[ -n "${MAILBOX_HOST:-}" ]]; then
    ask_secret mailbox-password "Password of the mailbox ${MAILBOX_USER:-}"
else
    ask_secret mailbox-password "" "none"
fi

step "App and inbox job ($IMAGE)"
OUTPUTS=$(deploy true)
APP=$(output appName)
JOB=$(output jobName)
ADDRESS=$(output appAddress)
echo "Flowtr: https://$ADDRESS"

if [[ -n "${GITHUB_REPO:-}" ]]; then
    step "GitHub Actions may set new images (OIDC, no secret in GitHub)"
    GITHUB_APP=$NAME-github
    CLIENT_ID=$(az ad app list --display-name "$GITHUB_APP" --query "[0].appId" -o tsv)
    if [[ -z "$CLIENT_ID" ]]; then
        CLIENT_ID=$(az ad app create --display-name "$GITHUB_APP" --query appId -o tsv)
        az ad sp create --id "$CLIENT_ID" -o none
    fi
    # GitHub sends the subject with names only, or (newer repositories) with names and IDs. Trust both.
    trust_github() {
        if [[ -z $(az ad app federated-credential list --id "$CLIENT_ID" --query "[?name=='$1'].name" -o tsv) ]]; then
            az ad app federated-credential create --id "$CLIENT_ID" --parameters "{
                \"name\": \"$1\",
                \"issuer\": \"https://token.actions.githubusercontent.com\",
                \"subject\": \"$2\",
                \"audiences\": [\"api://AzureADTokenExchange\"]
            }" -o none
        fi
    }
    trust_github main "repo:$GITHUB_REPO:ref:refs/heads/main"
    trust_github main-ids "$(gh api "repos/$GITHUB_REPO" \
        --jq '"repo:\(.owner.login)@\(.owner.id)/\(.name)@\(.id):ref:refs/heads/main"')"
    # Only the app and the job: GitHub sets the image, nothing else.
    for resource in "$(az containerapp show -g "$RESOURCE_GROUP" -n "$APP" --query id -o tsv)" \
        ${JOB:+"$(az containerapp job show -g "$RESOURCE_GROUP" -n "$JOB" --query id -o tsv)"}; do
        az role assignment create --assignee "$CLIENT_ID" --role Contributor --scope "$resource" -o none
    done
    gh variable set AZURE_CLIENT_ID --repo "$GITHUB_REPO" --body "$CLIENT_ID"
    gh variable set AZURE_TENANT_ID --repo "$GITHUB_REPO" --body "$TENANT_ID"
    gh variable set AZURE_SUBSCRIPTION_ID --repo "$GITHUB_REPO" --body "$SUBSCRIPTION_ID"
    gh variable set AZURE_RESOURCE_GROUP --repo "$GITHUB_REPO" --body "$RESOURCE_GROUP"
    gh variable set AZURE_CONTAINER_APP --repo "$GITHUB_REPO" --body "$APP"
    gh variable set AZURE_INBOX_JOB --repo "$GITHUB_REPO" --body "$JOB"
    gh variable set APP_URL --repo "$GITHUB_REPO" --body "https://${DOMAIN:-$ADDRESS}"
fi

if [[ -n "${DOMAIN:-}" && -z "$CERTIFICATE_ID" ]]; then
    step "Domain $DOMAIN: enter these DNS records, then run deploy/bind-domain.sh"
    echo "CNAME  ${DOMAIN%%.*}         $ADDRESS"
    echo "TXT    asuid.${DOMAIN%%.*}   $(output domainVerificationId)"
fi

step "Done"
