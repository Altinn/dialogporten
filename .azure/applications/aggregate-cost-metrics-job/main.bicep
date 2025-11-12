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

@description('The cron expression for the job schedule')
@minLength(9)
param jobSchedule string

@description('The connection string for Application Insights')
@minLength(3)
@secure()
param appInsightConnectionString string

@description('The replica timeout for the job in seconds')
param replicaTimeOutInSeconds int

@description('Azure Subscription Id')
@secure()
param azureSubscriptionId string

@description('The workload profile name to use, defaults to "Consumption"')
param workloadProfileName string = 'Consumption'

@description('The name of the Key Vault for the environment')
@minLength(3)
@secure()
param environmentKeyVaultName string

@description('The name of the storage container for cost metrics')
param storageContainerName string = 'costmetrics'

var namePrefix = 'dp-be-${environment}'
var baseImageUrl = 'ghcr.io/altinn/dialogporten-'

// Use naming convention for Application Insights resource
// Pattern: dp-be-{environment}-applicationInsights
var appInsightsName = 'dp-be-${environment}-applicationInsights'

var tags = {
  FullName: '${namePrefix}-aggregate-cost-metrics'
  Environment: environment
  Product: 'Dialogporten'
  Description: 'Aggregates cost metrics from Application Insights across environments'
  JobType: 'Scheduled'
}

var name = '${namePrefix}-cost-metrics'

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-10-02-preview' existing = {
  name: containerAppEnvironmentName
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: '${namePrefix}-cost-metrics-identity'
  location: location
  tags: tags
}

module keyVaultReaderAccessPolicy '../../modules/keyvault/addReaderRoles.bicep' = {
  name: 'keyVaultReaderAccessPolicy-${name}'
  params: {
    keyvaultName: environmentKeyVaultName
    principalIds: [managedIdentity.properties.principalId]
  }
}

var storageAccountName = take('${toLower(replace(namePrefix, '-', ''))}storage${uniqueString(resourceGroup().id)}', 24)

resource storageAccount 'Microsoft.Storage/storageAccounts@2025-01-01' = {
  name: storageAccountName 
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Cool'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    isHnsEnabled: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        blob: {
          enabled: true
        }
        file: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
      requireInfrastructureEncryption: true
    }
  }
  tags: tags
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2025-01-01' = {
  name: 'default'
  parent: storageAccount
}

resource storageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2025-01-01' = {
  name: storageContainerName
  parent: blobService
  properties: {
    publicAccess: 'None'
  }
}

module storageBlobDataContributorRole '../../modules/storageAccount/addBlobDataContributorRole.bicep' = {
  name: 'storageContributorRole-${name}'
  params: {
    storageAccountName: storageAccountName
    principalIds: [managedIdentity.properties.principalId]
  }
}

module appInsightsMonitoringReaderRole '../../modules/applicationInsights/addMonitoringReaderRole.bicep' = {
  name: 'appInsightsReaderRole-${name}'
  params: {
    appInsightsName: appInsightsName
    principalIds: [managedIdentity.properties.principalId]
  }
}

var containerAppEnvVars = [
  {
    name: 'Infrastructure__DialogDbConnectionString'
    secretRef: 'dbconnectionstring'
  }
  {
    name: 'Infrastructure__Redis__ConnectionString'
    secretRef: 'redisconnectionstring'
  }
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
    value: storageAccountName
  }
  {
    name: 'MetricsAggregation__StorageContainerName'
    value: storageContainerName
  }
  {
    name: 'MetricsAggregation__SubscriptionId'
    value: azureSubscriptionId
  }
]

// Base URL for accessing secrets in the Key Vault
// https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/bicep-functions-deployment#example-1
var keyVaultBaseUrl = 'https://${environmentKeyVaultName}${az.environment().suffixes.keyvaultDns}/secrets'

var secrets = [
  {
    name: 'dbconnectionstring'
    keyVaultUrl: '${keyVaultBaseUrl}/dialogportenAdoConnectionString'
    identity: managedIdentity.id
  }
  {
    name: 'redisconnectionstring'
    keyVaultUrl: '${keyVaultBaseUrl}/dialogportenRedisConnectionString'
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
    args: [
      'aggregate-cost-metrics'
    ]
    userAssignedIdentityId: managedIdentity.id
    replicaTimeOutInSeconds: replicaTimeOutInSeconds
    workloadProfileName: workloadProfileName
  }
  dependsOn: [
    storageBlobDataContributorRole
    appInsightsMonitoringReaderRole
    keyVaultReaderAccessPolicy
  ]
}
