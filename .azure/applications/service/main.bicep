targetScope = 'resourceGroup'

import { baseTags } from '../../functions/baseTags.bicep'
import { dialogDbConnectionString } from '../../functions/dialogDbConnectionString.bicep'

import { Scale } from '../../modules/containerApp/main.bicep'

@description('The tag of the image to be used')
@minLength(3)
param imageTag string

@description('The environment for the deployment')
@minLength(3)
param environment string

@description('The location where the resources will be deployed')
@minLength(3)
param location string

@description('The suffix for the revision of the container app')
@minLength(3)
param revisionSuffix string

@description('CPU and memory resources for the container app')
param resources object?

@description('The name of the container app environment')
@minLength(3)
param containerAppEnvironmentName string

@description('The name of the Service Bus namespace')
@minLength(3)
param serviceBusNamespaceName string

@description('The connection string for Application Insights')
@minLength(3)
@secure()
param appInsightConnectionString string

@description('The name of the App Configuration store')
@minLength(5)
param appConfigurationName string

@description('The name of the Key Vault for the environment')
@minLength(3)
param environmentKeyVaultName string

@description('The ratio of traces to sample (between 0.0 and 1.0). Lower values reduce logging volume.')
@minLength(1)
param otelTraceSamplerRatio string

@description('The workload profile name to use, defaults to "Consumption"')
param workloadProfileName string = 'Consumption'

@description('Minimum number of replicas')
@minValue(0)
param minReplicas int = 1

@description('The scaling configuration for the container app')
param scale Scale = {
  minReplicas: minReplicas
  maxReplicas: 10
  rules: [
    {
      name: 'cpu'
      custom: {
        type: 'cpu'
        metadata: {
          type: 'Utilization'
          value: '70'
        }
      }
    }
    {
      name: 'memory'
      custom: {
        type: 'memory'
        metadata: {
          type: 'Utilization'
          value: '70'
        }
      }
    }
  ]
}

@description('How the workload authenticates to PostgreSQL. EntraToken connects with the managed identity as the PostgreSQL role named after it.')
@allowed(['Password', 'EntraToken'])
param dbAuthMode string = 'Password'

@description('PostgreSQL server FQDN, required in EntraToken mode. No database password is read in this mode.')
param dbHost string = ''

@description('Explicit list of non-database Key Vault secrets referenced by App Configuration, required in EntraToken mode. Include Redis and the other runtime secrets it resolves.')
param runtimeSecretNames string[] = []

var namePrefix = 'dp-be-${environment}'
var baseImageUrl = 'ghcr.io/altinn/dialogporten-'

var additionalTags = {}

var tags = baseTags(additionalTags, environment)

var baseContainerAppEnvVars = [
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: environment
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: appInsightConnectionString
  }
  {
    name: 'AZURE_APPCONFIG_URI'
    value: appConfiguration.properties.endpoint
  }
  {
    name: 'ASPNETCORE_URLS'
    value: 'http://+:8080'
  }
  {
    name: 'AZURE_CLIENT_ID'
    value: managedIdentity.properties.clientId
  }
  {
    name: 'Infrastructure__MassTransit__Host'
    value: 'sb://${serviceBusNamespaceName}.servicebus.windows.net/'
  }
  {
    name: 'OTEL_TRACES_SAMPLER'
    value: 'parentbased_traceidratio'
  }
  {
    name: 'OTEL_TRACES_SAMPLER_ARG'
    value: otelTraceSamplerRatio
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
  dbAuthMode == 'EntraToken' ? entraTokenEnvVars : []
)

var serviceName = 'service'

var containerAppName = '${namePrefix}-${serviceName}'

resource appConfiguration 'Microsoft.AppConfiguration/configurationStores@2024-06-01' existing = {
  name: appConfigurationName
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2025-10-02-preview' existing = {
  name: containerAppEnvironmentName
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: '${namePrefix}-service-identity'
  location: location
  tags: tags
}

resource environmentKeyVaultResource 'Microsoft.KeyVault/vaults@2026-02-01' existing = {
  name: environmentKeyVaultName
}

module keyVaultReaderAccessPolicy '../../modules/keyvault/addReaderRoles.bicep' = if (dbAuthMode == 'Password') {
  name: 'keyVaultReaderAccessPolicy-${containerAppName}'
  params: {
    keyvaultName: environmentKeyVaultResource.name
    principalIds: [managedIdentity.properties.principalId]
  }
}

module runtimeSecretReaderAccessPolicy '../../modules/keyvault/addSecretReaderRoles.bicep' = if (dbAuthMode == 'EntraToken') {
  name: 'runtimeSecretReaderAccessPolicy-${containerAppName}'
  params: {
    keyvaultName: environmentKeyVaultName
    principalId: managedIdentity.properties.principalId
    secretNames: empty(runtimeSecretNames) ? fail('EntraToken requires an explicit runtimeSecretNames allowlist.') : runtimeSecretNames
  }
}

module appConfigReaderAccessPolicy '../../modules/appConfiguration/addReaderRoles.bicep' = {
  name: 'appConfigReaderAccessPolicy-${containerAppName}'
  params: {
    appConfigurationName: appConfigurationName
    principalIds: [managedIdentity.properties.principalId]
  }
}

module serviceBusOwnerAccessPolicy '../../modules/serviceBus/addDataOwnerRoles.bicep' = {
  name: 'serviceBusOwnerAccessPolicy-${containerAppName}'
  params: {
    serviceBusNamespaceName: serviceBusNamespaceName
    principalIds: [managedIdentity.properties.principalId]
  }
}

module containerApp '../../modules/containerApp/main.bicep' = {
  name: containerAppName
  params: {
    name: containerAppName
    image: '${baseImageUrl}${serviceName}:${imageTag}'
    location: location
    envVariables: containerAppEnvVars
    containerAppEnvId: containerAppEnvironment.id
    tags: tags
    resources: resources
    revisionSuffix: revisionSuffix
    userAssignedIdentityId: managedIdentity.id
    scale: scale
    workloadProfileName: workloadProfileName
  }
  dependsOn: [
    runtimeSecretReaderAccessPolicy
    keyVaultReaderAccessPolicy
    appConfigReaderAccessPolicy
    serviceBusOwnerAccessPolicy
  ]
}

output name string = containerApp.outputs.name
output revisionName string = containerApp.outputs.revisionName
