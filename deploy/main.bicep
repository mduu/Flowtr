// Flowtr in Azure (story #011, ADR-010): Container Apps with scale to zero, Azure SQL free tier, Blob Storage, Key Vault.
// Deploy with deploy/deploy.sh. The pipeline only sets a new image on the app and the job.

targetScope = 'resourceGroup'

@description('Short name. Part of all resource names.')
param name string = 'flowtr'

param location string = resourceGroup().location

@description('Container image, for example ghcr.io/mduu/flowtr:latest.')
param image string

@description('Own domain, for example app.flowtr.ch. Empty: only the generated address.')
param domain string = ''

@description('ID of the managed certificate for the domain. deploy/bind-domain.sh makes it, deploy/deploy.sh finds it.')
param certificateId string = ''

@description('Always admin of Flowtr (ADR-004).')
param adminEmail string

@description('Receives alerts and budget warnings.')
param alertEmail string = adminEmail

@description('Object ID of the person who operates Flowtr: may set secrets in Key Vault and read the blobs.')
param operatorId string

@description('Entra group that administers SQL: the operator and the identity of Flowtr. No SQL password.')
param sqlAdminGroupName string
param sqlAdminGroupId string

@description('False for the first run: the secrets in Key Vault do not exist yet.')
param deployApps bool = true

@description('IMAP mailbox for the e-mail inbox. Empty: no inbox job.')
param mailboxHost string = ''
param mailboxUserName string = ''

@description('Address shown in Flowtr, if it is not the user name.')
param mailboxAddress string = ''

@description('Cron expression (UTC) for the inbox job.')
param inboxSchedule string = '7 * * * *'

@description('Budget per month in the currency of the billing account.')
param monthlyBudget int = 5

@description('First day of the month the budget starts, yyyy-MM-dd. It cannot change after the first deployment.')
param budgetStartDate string

var suffix = uniqueString(resourceGroup().id)
var tags = { application: name }

// Built-in roles
var roles = {
  storageBlobDataContributor: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
  keyVaultCryptoServiceEncryptionUser: 'e147488a-f6f5-4113-8e2d-b22465e65bf6'
  keyVaultSecretsUser: '4633458b-17de-408a-b874-0445c86b69e6'
  keyVaultSecretsOfficer: 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7'
}

// --- Logs: only what says something, with a daily cap (story #011, logs) ---

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${name}'
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
    workspaceCapping: { dailyQuotaGb: json('0.1') }
    features: { enableLogAccessUsingOnlyResourcePermissions: true }
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${name}'
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
    IngestionMode: 'LogAnalytics'
    RetentionInDays: 30
  }
}

// --- Identity of the app and the job ---

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${name}'
  location: location
  tags: tags
}

// --- Blob Storage: receipts, inbox, keys of Data Protection ---

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take('st${name}${suffix}', 24)
  location: location
  tags: tags
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    defaultToOAuthAuthentication: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
  properties: {
    isVersioningEnabled: true
    deleteRetentionPolicy: { enabled: true, days: 30 }
    containerDeleteRetentionPolicy: { enabled: true, days: 30 }
  }
}

resource containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = [for container in ['belege', 'eingang', 'dataprotection']: {
  parent: blobService
  name: container
  properties: { publicAccess: 'None' }
}]

// Receipts keep all versions (never lose data). Uploads and keys change often: old versions go.
resource lifecycle 'Microsoft.Storage/storageAccounts/managementPolicies@2023-05-01' = {
  parent: storage
  name: 'default'
  properties: {
    policy: {
      rules: [
        {
          name: 'old-versions'
          enabled: true
          type: 'Lifecycle'
          definition: {
            filters: { blobTypes: ['blockBlob'], prefixMatch: ['eingang/', 'dataprotection/'] }
            actions: { version: { delete: { daysAfterCreationGreaterThan: 30 } } }
          }
        }
      ]
    }
  }
}

resource storageLock 'Microsoft.Authorization/locks@2020-05-01' = {
  scope: storage
  name: 'keep-receipts'
  properties: {
    level: 'CanNotDelete'
    notes: 'The receipts of Flowtr. Never lose data.'
  }
}

// --- Key Vault: secrets and the key that protects the keys of Data Protection ---

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('kv-${name}-${suffix}', 24)
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
  }
}

resource dataProtectionKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
  parent: vault
  name: 'dataprotection'
  properties: {
    kty: 'RSA'
    keySize: 2048
    keyOps: ['wrapKey', 'unwrapKey']
  }
}

// --- Azure SQL, free tier: pauses after 15 minutes without a connection ---

resource sql 'Microsoft.Sql/servers@2023-08-01' = {
  name: 'sql-${name}-${suffix}'
  location: location
  tags: tags
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      principalType: 'Group'
      login: sqlAdminGroupName
      sid: sqlAdminGroupId
      tenantId: subscription().tenantId
    }
  }
}

// Container Apps on the Consumption plan have no fixed outbound address. Entra authentication protects the server.
resource sqlAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  parent: sql
  name: 'AllowAllWindowsAzureIps'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sql
  name: 'flowtr'
  location: location
  tags: tags
  sku: { name: 'GP_S_Gen5', tier: 'GeneralPurpose', family: 'Gen5', capacity: 2 }
  properties: {
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
    minCapacity: json('0.5')
    maxSizeBytes: 34359738368
    requestedBackupStorageRedundancy: 'Local'
    zoneRedundant: false
  }
}

resource sqlLock 'Microsoft.Authorization/locks@2020-05-01' = {
  scope: sql
  name: 'keep-journal'
  properties: {
    level: 'CanNotDelete'
    notes: 'The journal of Flowtr. Never lose data.'
  }
}

// --- Roles ---

resource identityBlobs 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storage
  name: guid(storage.id, identity.id, roles.storageBlobDataContributor)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.storageBlobDataContributor)
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource operatorBlobs 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storage
  name: guid(storage.id, operatorId, roles.storageBlobDataContributor)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.storageBlobDataContributor)
    principalId: operatorId
    principalType: 'User'
  }
}

resource identityKey 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: vault
  name: guid(vault.id, identity.id, roles.keyVaultCryptoServiceEncryptionUser)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.keyVaultCryptoServiceEncryptionUser)
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource identitySecrets 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: vault
  name: guid(vault.id, identity.id, roles.keyVaultSecretsUser)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.keyVaultSecretsUser)
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource operatorSecrets 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: vault
  name: guid(vault.id, operatorId, roles.keyVaultSecretsOfficer)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.keyVaultSecretsOfficer)
    principalId: operatorId
    principalType: 'User'
  }
}

// --- Container Apps: the app scales to zero, the job fetches mail every hour ---

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-${name}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: { destination: 'azure-monitor' }
    workloadProfiles: [{ name: 'Consumption', workloadProfileType: 'Consumption' }]
  }
}

// Only the console of the containers, and only for errors at the start. No system logs (revisions, scaling).
resource environmentLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: environment
  name: 'console'
  properties: {
    workspaceId: logs.id
    logs: [{ category: 'ContainerAppConsoleLogs', enabled: true }]
  }
}

var publicUrl = empty(domain) ? 'https://${name}.${environment.properties.defaultDomain}/' : 'https://${domain}/'

var secrets = [
  {
    name: 'anthropic-api-key'
    keyVaultUrl: '${vault.properties.vaultUri}secrets/anthropic-api-key'
    identity: identity.id
  }
  {
    name: 'mailbox-password'
    keyVaultUrl: '${vault.properties.vaultUri}secrets/mailbox-password'
    identity: identity.id
  }
]

var sharedEnv = [
  { name: 'AZURE_CLIENT_ID', value: identity.properties.clientId }
  {
    name: 'ConnectionStrings__flowtr'
    value: 'Server=tcp:${sql.properties.fullyQualifiedDomainName},1433;Database=${database.name};Authentication=Active Directory Managed Identity;User Id=${identity.properties.clientId};Encrypt=True;Connect Timeout=60'
  }
  { name: 'ConnectionStrings__blobs', value: storage.properties.primaryEndpoints.blob }
  { name: 'DataProtection__KeyVaultKey', value: dataProtectionKey.properties.keyUri }
  { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
  { name: 'PublicUrl', value: publicUrl }
  { name: 'Admins__0', value: adminEmail }
  { name: 'Anthropic__ApiKey', secretRef: 'anthropic-api-key' }
  { name: 'Mailbox__Host', value: mailboxHost }
  { name: 'Mailbox__UserName', value: mailboxUserName }
  { name: 'Mailbox__Address', value: mailboxAddress }
  { name: 'Mailbox__Password', secretRef: 'mailbox-password' }
]

resource app 'Microsoft.App/containerApps@2024-03-01' = if (deployApps) {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      maxInactiveRevisions: 3
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
        customDomains: empty(certificateId) ? [] : [
          { name: domain, certificateId: certificateId, bindingType: 'SniEnabled' }
        ]
      }
      secrets: secrets
    }
    template: {
      containers: [
        {
          name: 'flowtr'
          image: image
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: concat(sharedEnv, [
            { name: 'OTEL_SERVICE_NAME', value: 'flowtr' }
            // TLS ends in front of the container (story #011).
            { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
            { name: 'ASPNETCORE_HTTPS_PORT', value: '443' }
          ])
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
        rules: [{ name: 'http', http: { metadata: { concurrentRequests: '50' } } }]
      }
    }
  }
  dependsOn: [identityBlobs, identityKey, identitySecrets, containers]
}

resource inboxJob 'Microsoft.App/jobs@2024-03-01' = if (deployApps && !empty(mailboxHost)) {
  name: '${name}-inbox'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      triggerType: 'Schedule'
      scheduleTriggerConfig: { cronExpression: inboxSchedule, parallelism: 1, replicaCompletionCount: 1 }
      replicaTimeout: 600
      replicaRetryLimit: 0
      secrets: secrets
    }
    template: {
      containers: [
        {
          name: 'inbox'
          image: image
          args: ['--job', 'inbox']
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: concat(sharedEnv, [{ name: 'OTEL_SERVICE_NAME', value: 'flowtr-inbox' }])
        }
      ]
    }
  }
  dependsOn: [identityBlobs, identityKey, identitySecrets, containers]
}

// --- Alerts and budget ---

resource alerts 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: 'ag-${name}'
  location: 'global'
  tags: tags
  properties: {
    groupShortName: take(name, 12)
    enabled: true
    emailReceivers: [{ name: 'operator', emailAddress: alertEmail, useCommonAlertSchema: true }]
  }
}

// Errors of the app and the job (for example the mailbox is not reachable). 4xx from bot scans are no errors.
resource errorAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'alert-${name}-errors'
  location: location
  tags: tags
  properties: {
    displayName: 'Flowtr: Fehler'
    description: 'Flowtr oder der Job für den E-Mail-Eingang hat einen Fehler gemeldet. Details in Application Insights.'
    severity: 2
    enabled: true
    scopes: [insights.id]
    evaluationFrequency: 'PT1H'
    windowSize: 'PT1H'
    autoMitigate: true
    criteria: {
      allOf: [
        {
          query: 'union traces, exceptions | where severityLevel >= 3 or itemType == "exception"'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: { numberOfEvaluationPeriods: 1, minFailingPeriodsToAlert: 1 }
        }
      ]
    }
    actions: { actionGroups: [alerts.id] }
  }
}

// Less than 10 % of the free vCore seconds left: the database pauses until the end of the month.
resource freeLimitAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: 'alert-${name}-free-limit'
  location: 'global'
  tags: tags
  properties: {
    description: 'Azure SQL: weniger als 10 % des Gratis-Kontingents übrig. Bei 0 pausiert die Datenbank bis Ende Monat.'
    severity: 2
    enabled: true
    scopes: [database.id]
    evaluationFrequency: 'PT1H'
    windowSize: 'PT1H'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          criterionType: 'StaticThresholdCriterion'
          name: 'FreeAmountRemaining'
          metricName: 'free_amount_remaining'
          operator: 'LessThan'
          threshold: 10000
          timeAggregation: 'Minimum'
        }
      ]
    }
    actions: [{ actionGroupId: alerts.id }]
  }
}

resource budget 'Microsoft.Consumption/budgets@2023-11-01' = {
  name: 'budget-${name}'
  properties: {
    category: 'Cost'
    amount: monthlyBudget
    timeGrain: 'Monthly'
    timePeriod: { startDate: budgetStartDate }
    notifications: {
      actual80: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: 80
        thresholdType: 'Actual'
        contactEmails: [alertEmail]
      }
      forecast100: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: 100
        thresholdType: 'Forecasted'
        contactEmails: [alertEmail]
      }
    }
  }
}

output appName string = deployApps ? app.name : ''
output jobName string = deployApps && !empty(mailboxHost) ? inboxJob.name : ''
output appAddress string = deployApps ? app!.properties.configuration.ingress.fqdn : ''
output domainVerificationId string = deployApps ? app!.properties.customDomainVerificationId : ''
output environmentName string = environment.name
output vaultName string = vault.name
output identityPrincipalId string = identity.properties.principalId
output sqlServer string = sql.properties.fullyQualifiedDomainName
output storageAccount string = storage.name
