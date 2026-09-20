targetScope = 'resourceGroup'

import { baseTags } from '../../functions/baseTags.bicep'
import { dialogDbConnectionString } from '../../functions/dialogDbConnectionString.bicep'

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

@description('The connection string for Application Insights')
@minLength(3)
@secure()
param appInsightConnectionString string

@description('The replica timeout for the job in seconds')
param replicaTimeOutInSeconds int

@description('The workload profile name to use, defaults to "Consumption"')
param workloadProfileName string = 'Consumption'

@description('How the workload authenticates to PostgreSQL. EntraToken connects with the managed identity as the PostgreSQL role named after it.')
@allowed(['Password', 'EntraToken'])
param dbAuthMode string = 'Password'

@description('PostgreSQL server FQDN, required in EntraToken mode. No database password is read in this mode.')
param dbHost string = ''

var namePrefix = 'dp-be-${environment}'
var baseImageUrl = 'ghcr.io/altinn/dialogporten-'

var name = '${namePrefix}-reindex-search'
var additionalTags = {
  FullName: name
  Description: 'Manual janitor job to reindex dialog search'
  JobType: 'Manual'
}

var tags = baseTags(additionalTags, environment)

var baseContainerAppEnvVars = [
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
]

// Token mode receives only the server address and authenticates as this workload's identity.
// The administrator connection string remains available only in Password mode.
var entraTokenEnvVars = [
  {
    name: 'Infrastructure__DialogDbConnectionString'
    value: dbAuthMode == 'EntraToken' ? dialogDbConnectionString(dbHost) : ''
  }
  {
    name: 'Infrastructure__DialogDbAuth__Mode'
    value: 'EntraToken'
  }
  {
    name: 'Infrastructure__DialogDbAuth__Username'
    value: managedIdentity.name
  }
]

var containerAppEnvVars = concat(
  baseContainerAppEnvVars,
  dbAuthMode == 'EntraToken' ? entraTokenEnvVars : [
    {
      name: 'Infrastructure__DialogDbConnectionString'
      secretRef: 'dbconnectionstring'
    }
  ]
)

// Base URL for accessing secrets in the Key Vault
// https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/bicep-functions-deployment#example-1
var keyVaultBaseUrl = 'https://${environmentKeyVaultName}${az.environment().suffixes.keyvaultDns}/secrets'

var secrets = concat(dbAuthMode == 'Password' ? [
  {
    name: 'dbconnectionstring'
    keyVaultUrl: '${keyVaultBaseUrl}/dialogportenAdoConnectionString'
    identity: managedIdentity.id
  }
] : [], [
  {
    name: 'redisconnectionstring'
    keyVaultUrl: '${keyVaultBaseUrl}/dialogportenRedisConnectionString'
    identity: managedIdentity.id
  }
])

module runtimeSecretReaderAccessPolicy '../../modules/keyvault/addSecretReaderRoles.bicep' = if (dbAuthMode == 'EntraToken') {
  name: 'runtimeSecretReaderAccessPolicy-${name}'
  params: {
    keyvaultName: environmentKeyVaultName
    principalId: managedIdentity.properties.principalId
    secretNames: ['dialogportenRedisConnectionString']
  }
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2025-10-02-preview' existing = {
  name: containerAppEnvironmentName
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: '${name}-identity'
  location: location
  tags: tags
}

module keyVaultReaderAccessPolicy '../../modules/keyvault/addReaderRoles.bicep' = if (dbAuthMode == 'Password') {
  name: 'keyVaultReaderAccessPolicy-${name}'
  params: {
    keyvaultName: environmentKeyVaultName
    principalIds: [
      managedIdentity.properties.principalId
    ]
  }
}

module dialogsearchReindexJob '../../modules/containerAppJob/main.bicep' = {
  name: name
  params: {
    name: name
    location: location
    image: '${baseImageUrl}janitor:${imageTag}'
    containerAppEnvId: containerAppEnvironment.id
    environmentVariables: containerAppEnvVars
    secrets: secrets
    tags: tags
    // We need a beefy container to run multiple reindexing workers
    resources: {
        cpu: 4
        memory: '8Gi'
    }
    args: [
      'reindex-dialogsearch'
    ]
    userAssignedIdentityId: managedIdentity.id
    replicaTimeOutInSeconds: replicaTimeOutInSeconds
    workloadProfileName: workloadProfileName
  }
  dependsOn: [
    runtimeSecretReaderAccessPolicy
    keyVaultReaderAccessPolicy
  ]
}

output identityPrincipalId string = managedIdentity.properties.principalId
output name string = dialogsearchReindexJob.outputs.name
