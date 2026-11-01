#!/usr/bin/env bash
# Binds the own domain (DOMAIN in deploy/flowtr.env) with a free managed certificate (story #011).
# Before: deploy/deploy.sh, then the CNAME and TXT records at the DNS provider. Once per domain.
set -euo pipefail
cd "$(dirname "$0")"

# shellcheck source=/dev/null
source ./flowtr.env
: "${DOMAIN:?Set DOMAIN in deploy/flowtr.env}"
NAME=${NAME:-flowtr}
RESOURCE_GROUP=${RESOURCE_GROUP:-rg-$NAME}

az account set --subscription "$SUBSCRIPTION_ID"
az containerapp hostname add --resource-group "$RESOURCE_GROUP" --name "$NAME" --hostname "$DOMAIN" -o none
# The certificate takes some minutes. The CNAME validation does not need the app to run.
az containerapp hostname bind --resource-group "$RESOURCE_GROUP" --name "$NAME" --hostname "$DOMAIN" \
    --environment "cae-$NAME" --validation-method CNAME -o none
echo "https://$DOMAIN is bound. deploy/deploy.sh keeps the binding."
