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

var name = '${namePrefix}-aggregate-cost-metrics'

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-10-02-preview' existing = {
  name: containerAppEnvironmentName
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: '${namePrefix}-aggregate-cost-metrics-identity'
  location: location
  tags: tags
}

// Create storage account for cost metrics
module storageAccount '../../modules/storageAccount/main.bicep' = {
  name: 'storageAccount-${name}'
  params: {
    namePrefix: namePrefix
    location: location
    tags: tags
    accessTier: 'Cool'
  }
}

// Create blob container for cost metrics
resource storageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  name: '${storageAccount.name}/default/${storageContainerName}'
  properties: {
    publicAccess: 'None'
  }
}

module storageBlobDataContributorRole '../../modules/storageAccount/addBlobDataContributorRole.bicep' = {
  name: 'storageBlobDataContributorRole-${name}'
  params: {
    storageAccountName: storageAccount.outputs.storageAccountName
    principalIds: [managedIdentity.properties.principalId]
  }
}

module appInsightsMonitoringReaderRole '../../modules/applicationInsights/addMonitoringReaderRole.bicep' = {
  name: 'appInsightsMonitoringReaderRole-${name}'
  params: {
    appInsightsName: appInsightsName
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
    value: storageAccount.outputs.storageAccountName
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

module costMetricsJob '../../modules/containerAppJob/main.bicep' = {
  name: name
  params: {
    name: name
    location: location
    image: '${baseImageUrl}janitor:${imageTag}'
    containerAppEnvId: containerAppEnvironment.id
    environmentVariables: containerAppEnvVars
    tags: tags
    cronExpression: jobSchedule
    args: 'aggregate-cost-metrics'
    userAssignedIdentityId: managedIdentity.id
    replicaTimeOutInSeconds: replicaTimeOutInSeconds
    workloadProfileName: workloadProfileName
  }
  dependsOn: [
    storageBlobDataContributorRole
    appInsightsMonitoringReaderRole
  ]
}
