#!/usr/bin/env bash
# Sets up Flowtr in Azure (story #011, docs/betrieb.md). Run it once, and again after a change to deploy/flowtr.env:
# it changes only what is different. The pipeline sets new images on the app and the job. The workflow
# "Infrastructure" deploys changes to deploy/main.bicep.
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
CULTURE=${CULTURE:-de-CH}

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

# shellcheck source=deploy/bicep.sh
source ./bicep.sh
deploy() { deploy_bicep "$1"; }

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
    step "GitHub Actions may deploy (OIDC, no secret in GitHub)"
    # Prints the client ID of the app registration $1. Makes it, if it does not exist.
    github_app() {
        local client_id
        client_id=$(az ad app list --display-name "$1" --query "[0].appId" -o tsv)
        if [[ -z "$client_id" ]]; then
            client_id=$(az ad app create --display-name "$1" --query appId -o tsv)
            az ad sp create --id "$client_id" -o none
        fi
        echo "$client_id"
    }
    # GitHub sends the subject with names only, or (newer repositories) with names and IDs. Trust both.
    REPO_WITH_IDS=$(gh api "repos/$GITHUB_REPO" --jq '"\(.owner.login)@\(.owner.id)/\(.name)@\(.id)"')
    trust_github() {
        local client_id=$1 name=$2 claim=$3
        trust_subject "$client_id" "$name" "repo:$GITHUB_REPO:$claim"
        trust_subject "$client_id" "$name-ids" "repo:$REPO_WITH_IDS:$claim"
    }
    trust_subject() {
        if [[ -z $(az ad app federated-credential list --id "$1" --query "[?name=='$2'].name" -o tsv) ]]; then
            az ad app federated-credential create --id "$1" --parameters "{
                \"name\": \"$2\",
                \"issuer\": \"https://token.actions.githubusercontent.com\",
                \"subject\": \"$3\",
                \"audiences\": [\"api://AzureADTokenExchange\"]
            }" -o none
        fi
    }

    # Pipeline: only the image of the app and the job, from main.
    CLIENT_ID=$(github_app "$NAME-github")
    trust_github "$CLIENT_ID" main ref:refs/heads/main
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

    # Workflow "Infrastructure": deploys deploy/main.bicep. It sets roles and locks, so it needs Owner on the
    # resource group. Only the environment $INFRA_ENV gets a token, and only from main.
    INFRA_ENV=azure-infra
    gh api --method PUT "repos/$GITHUB_REPO/environments/$INFRA_ENV" --input - <<<'{
        "deployment_branch_policy": { "protected_branches": false, "custom_branch_policies": true }
    }' >/dev/null
    if [[ -z $(gh api "repos/$GITHUB_REPO/environments/$INFRA_ENV/deployment-branch-policies" \
        --jq '.branch_policies[] | select(.name == "main") | .name') ]]; then
        gh api --method POST "repos/$GITHUB_REPO/environments/$INFRA_ENV/deployment-branch-policies" \
            -f name=main -f type=branch >/dev/null
    fi
    INFRA_CLIENT_ID=$(github_app "$NAME-github-infra")
    trust_github "$INFRA_CLIENT_ID" infra "environment:$INFRA_ENV"
    az role assignment create --assignee "$INFRA_CLIENT_ID" --role Owner \
        --scope "$(az group show --name "$RESOURCE_GROUP" --query id -o tsv)" -o none
    set_infra_variable() {
        if [[ -n "$2" ]]; then
            gh variable set "$1" --repo "$GITHUB_REPO" --env "$INFRA_ENV" --body "$2"
        else
            gh variable delete "$1" --repo "$GITHUB_REPO" --env "$INFRA_ENV" 2>/dev/null || true
        fi
    }
    set_infra_variable AZURE_INFRA_CLIENT_ID "$INFRA_CLIENT_ID"
    set_infra_variable OPERATOR_ID "$OPERATOR_ID"
    set_infra_variable SQL_GROUP "$SQL_GROUP"
    set_infra_variable SQL_GROUP_ID "$SQL_GROUP_ID"
    set_infra_variable NAME "$NAME"
    set_infra_variable ADMIN_EMAIL "$ADMIN_EMAIL"
    set_infra_variable ALERT_EMAIL "$ALERT_EMAIL"
    set_infra_variable DOMAIN "${DOMAIN:-}"
    set_infra_variable MAILBOX_HOST "${MAILBOX_HOST:-}"
    set_infra_variable MAILBOX_USER "${MAILBOX_USER:-}"
    set_infra_variable MAILBOX_ADDRESS "${MAILBOX_ADDRESS:-}"
    set_infra_variable MONTHLY_BUDGET "$MONTHLY_BUDGET"
    set_infra_variable CULTURE "$CULTURE"
fi

if [[ -n "${DOMAIN:-}" && -z "$CERTIFICATE_ID" ]]; then
    step "Domain $DOMAIN: enter these DNS records, then run deploy/bind-domain.sh"
    echo "CNAME  ${DOMAIN%%.*}         $ADDRESS"
    echo "TXT    asuid.${DOMAIN%%.*}   $(output domainVerificationId)"
fi

step "Done"
