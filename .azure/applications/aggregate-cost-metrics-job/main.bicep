targetScope = 'resourceGroup'

@description('The tag of the image to be used')
@minLength(3)
param imageTag string

@description('The environment for the deployment')
@minLength(3)
param environment string

@description('The location where the resources will be deployed')
@minLength(3)
param location string

@description('The name of the container app environment')
@minLength(3)
@secure()
param containerAppEnvironmentName string

@description('The name of the Key Vault for the environment')
@minLength(3)
@secure()
param environmentKeyVaultName string

@description('The cron expression for the job schedule')
@minLength(9)
param jobSchedule string

@description('The connection string for Application Insights')
@minLength(3)
@secure()
param appInsightConnectionString string

@description('The replica timeout for the job in seconds')
param replicaTimeOutInSeconds int

@description('The workload profile name to use, defaults to "Consumption"')
param workloadProfileName string = 'Consumption'

@description('The name of the storage container for cost metrics')
param storageContainerName string = 'costmetrics'

@description('Environments to aggregate metrics from')
param environments array = [
  'staging'
  'prod'
]

var namePrefix = 'dp-be-${environment}'
var baseImageUrl = 'ghcr.io/altinn/dialogporten-'
var tags = {
  FullName: '${namePrefix}-aggregate-cost-metrics'
  Environment: environment
  Product: 'Dialogporten'
  Description: 'Aggregates cost metrics from Application Insights across environments'
  JobType: 'Scheduled'
}
var name = '${namePrefix}-aggregate-cost-metrics'

// Compute a valid storage account name (<=24, lowercase, alphanumeric)
var saPrefixBase = replace(toLower(namePrefix), '-', '')
var saShortPrefix = take(saPrefixBase, 8)
var saStatic = 'costmetrics' // 11 chars
var saUnique = take(uniqueString(resourceGroup().id), 5)
var storageAccountSafeName = '${saShortPrefix}${saStatic}${saUnique}'

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-10-02-preview' existing = {
  name: containerAppEnvironmentName
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: '${namePrefix}-aggregate-cost-metrics-identity'
  location: location
  tags: tags
}

// Create storage account for cost metrics
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountSafeName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    encryption: {
      services: {
        blob: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
  tags: tags
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  name: 'default'
  parent: storageAccount
}

// Create blob container for cost metrics
resource storageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: storageContainerName
  properties: {
    publicAccess: 'None'
  }
}

module keyVaultReaderAccessPolicy '../../modules/keyvault/addReaderRoles.bicep' = {
  name: 'keyVaultReaderAccessPolicy-${name}'
  params: {
    keyvaultName: environmentKeyVaultName
    principalIds: [managedIdentity.properties.principalId]
  }
}

var containerAppEnvVars = [
  {
    name: 'DOTNET_ENVIRONMENT'
    value: environment
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: appInsightConnectionString
  }
  {
    name: 'AZURE_CLIENT_ID'
    value: managedIdentity.properties.clientId
  }
  {
    name: 'MetricsAggregation__StorageAccountName'
    value: storageAccount.name
  }
  {
    name: 'MetricsAggregation__StorageContainerName'
    value: storageContainerName
  }
]

// Base URL for accessing secrets in the Key Vault
// https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/bicep-functions-deployment#example-1
var keyVaultBaseUrl = 'https://${environmentKeyVaultName}${az.environment().suffixes.keyvaultDns}/secrets'

var secrets = [
  {
    name: 'stagingSubscriptionId'
    keyVaultUrl: '${keyVaultBaseUrl}/aggregateCostMetricsStagingSubscriptionId'
    identity: managedIdentity.id
  }
  {
    name: 'prodSubscriptionId'
    keyVaultUrl: '${keyVaultBaseUrl}/aggregateCostMetricsProdSubscriptionId'
    identity: managedIdentity.id
  }
  {
    name: 'testSubscriptionId'
    keyVaultUrl: '${keyVaultBaseUrl}/aggregateCostMetricsTestSubscriptionId'
    identity: managedIdentity.id
  }
  {
    name: 'yt01SubscriptionId'
    keyVaultUrl: '${keyVaultBaseUrl}/aggregateCostMetricsYt01SubscriptionId'
    identity: managedIdentity.id
  }
]

module costMetricsJob '../../modules/containerAppJob/main.bicep' = {
  name: name
  params: {
    name: name
    location: location
    image: '${baseImageUrl}janitor:${imageTag}'
    containerAppEnvId: containerAppEnvironment.id
    environmentVariables: containerAppEnvVars
    secrets: secrets
    tags: tags
    cronExpression: jobSchedule
    args: 'aggregate-cost-metrics -e ${join(environments, ' -e ')}'
    userAssignedIdentityId: managedIdentity.id
    replicaTimeOutInSeconds: replicaTimeOutInSeconds
    workloadProfileName: workloadProfileName
  }
  dependsOn: [
    keyVaultReaderAccessPolicy
  ]
}

output identityPrincipalId string = managedIdentity.properties.principalId
output name string = costMetricsJob.outputs.name
output storageAccountName string = storageAccount.name
output storageContainerName string = storageContainerName
