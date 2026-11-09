# Deploys deploy/main.bicep. deploy/deploy.sh and the workflow "Infrastructure" use it: source this file, then call
# deploy_bicep <deployApps: true|false> [what-if].
#
# Needs these variables: SUBSCRIPTION_ID, RESOURCE_GROUP, NAME, IMAGE, ADMIN_EMAIL, ALERT_EMAIL, OPERATOR_ID,
# SQL_GROUP, SQL_GROUP_ID, MONTHLY_BUDGET, CULTURE. Optional: DOMAIN, MAILBOX_HOST, MAILBOX_USER, MAILBOX_ADDRESS.
# shellcheck shell=bash

deploy_bicep() {
    local apps=$1 mode=${2:-create}

    # The start of the budget cannot change: keep the start of an existing budget.
    local budget_start
    budget_start=$(az rest --method get \
        --url "https://management.azure.com/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Consumption/budgets/budget-$NAME?api-version=2023-11-01" \
        --query properties.timePeriod.startDate -o tsv 2>/dev/null | cut -c1-10 || true)
    budget_start=${budget_start:-$(date -u +%Y-%m-01)}

    # After deploy/bind-domain.sh: without the certificate, a deployment would remove the domain from the app.
    # Sets CERTIFICATE_ID: deploy/deploy.sh shows the DNS records while it is empty.
    CERTIFICATE_ID=""
    if [[ -n "${DOMAIN:-}" ]]; then
        CERTIFICATE_ID=$(az containerapp env certificate list --resource-group "$RESOURCE_GROUP" --name "cae-$NAME" \
            --managed-certificates-only --query "[?properties.subjectName=='$DOMAIN'].id | [0]" -o tsv 2>/dev/null || true)
    fi

    local args=(--resource-group "$RESOURCE_GROUP" --template-file "$(dirname "${BASH_SOURCE[0]}")/main.bicep"
        --parameters name="$NAME" image="$IMAGE" domain="${DOMAIN:-}" certificateId="$CERTIFICATE_ID"
            adminEmail="$ADMIN_EMAIL" alertEmail="$ALERT_EMAIL" operatorId="$OPERATOR_ID"
            sqlAdminGroupName="$SQL_GROUP" sqlAdminGroupId="$SQL_GROUP_ID"
            mailboxHost="${MAILBOX_HOST:-}" mailboxUserName="${MAILBOX_USER:-}" mailboxAddress="${MAILBOX_ADDRESS:-}"
            monthlyBudget="$MONTHLY_BUDGET" budgetStartDate="$budget_start" culture="$CULTURE" deployApps="$apps")

    if [[ $mode == what-if ]]; then
        az deployment group what-if "${args[@]}"
    else
        az deployment group create --name "flowtr-$(date -u +%Y%m%d-%H%M%S)" "${args[@]}" \
            --query properties.outputs -o json
    fi
}
