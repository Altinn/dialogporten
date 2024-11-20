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

var namePrefix = 'dp-be-${environment}'
var baseImageUrl = 'ghcr.io/digdir/dialogporten-'
var tags = {
  FullName: '${namePrefix}-sync-resource-policy-information'
  Environment: environment
  Product: 'Dialogporten'
  Description: 'Synchronizes resource policy information'
  JobType: 'Scheduled'
}
var name = '${namePrefix}-sync-rp-info'

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: containerAppEnvironmentName
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
]

// Base URL for accessing secrets in the Key Vault
// https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/bicep-functions-deployment#example-1
var keyVaultBaseUrl = 'https://${environmentKeyVaultName}${az.environment().suffixes.keyvaultDns}/secrets'

var secrets = [
  {
    name: 'dbconnectionstring'
    keyVaultUrl: '${keyVaultBaseUrl}/dialogportenAdoConnectionString'
    identity: 'System'
  }
  {
    name: 'redisconnectionstring'
    keyVaultUrl: '${keyVaultBaseUrl}/dialogportenRedisConnectionString'
    identity: 'System'
  }
]

module migrationJob '../../modules/containerAppJob/main.bicep' = {
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
    args: 'sync-resource-policy-information'
  }
}

module keyVaultReaderAccessPolicy '../../modules/keyvault/addReaderRoles.bicep' = {
  name: 'keyVaultReaderAccessPolicy-${name}'
  params: {
    keyvaultName: environmentKeyVaultName
    principalIds: [migrationJob.outputs.identityPrincipalId]
  }
}

output identityPrincipalId string = migrationJob.outputs.identityPrincipalId
output name string = migrationJob.outputs.name
