targetScope = 'resourceGroup'

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

@description('The IP address of the API Management instance')
@minLength(3)
param apimIp string

@description('The suffix for the revision of the container app')
@minLength(3)
param revisionSuffix string

@description('CPU and memory resources for the container app')
param resources object?

@description('The name of the container app environment')
@minLength(3)
@secure()
param containerAppEnvironmentName string

@description('The connection string for Application Insights')
@minLength(3)
@secure()
param appInsightConnectionString string

@description('The name of the App Configuration store')
@minLength(5)
@secure()
param appConfigurationName string

@description('The name of the Key Vault for the environment')
@minLength(3)
@secure()
param environmentKeyVaultName string

@description('The ratio of traces to sample (between 0.0 and 1.0). Lower values reduce logging volume.')
@minLength(1)
param otelTraceSamplerRatio string

var namePrefix = 'dp-be-${environment}'
var baseImageUrl = 'ghcr.io/altinn/dialogporten-'
var tags = {
  Environment: environment
  Product: 'Dialogporten'
}

resource appConfiguration 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
  name: appConfigurationName
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2025-01-01' existing = {
  name: containerAppEnvironmentName
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: '${namePrefix}-webapi-eu-identity'
  location: location
  tags: tags
}

var containerAppEnvVars = [
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
    name: 'OTEL_TRACES_SAMPLER'
    value: 'parentbased_traceidratio'
  }
  {
    name: 'OTEL_TRACES_SAMPLER_ARG'
    value: otelTraceSamplerRatio
  }
]

@description('The scaling configuration for the container app')
param scale Scale = {
  minReplicas: 2
  maxReplicas: 20
  rules: [
    {
      name: 'cpu'
      custom: {
        type: 'cpu'
        metadata: {
          type: 'Utilization'
          value: '50'
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

resource environmentKeyVaultResource 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: environmentKeyVaultName
}

var containerAppName = '${namePrefix}-webapi-eu-ca'

var port = 8080

var probes = [
  {
    periodSeconds: 5
    initialDelaySeconds: 2
    timeoutSeconds: 3
    type: 'Liveness'
    httpGet: {
      path: '/health/liveness'
      port: port
    }
  }
  {
    periodSeconds: 5
    initialDelaySeconds: 20
    timeoutSeconds: 3
    failureThreshold: 6
    type: 'Readiness'
    httpGet: {
      path: '/health/readiness'
      port: port
    }
  }
  {
    periodSeconds: 5
    initialDelaySeconds: 10
    timeoutSeconds: 3
    failureThreshold: 6
    type: 'Startup'
    httpGet: {
      path: '/health/startup'
      port: port
    }
  }
]

module containerApp '../../modules/containerApp/main.bicep' = {
  name: containerAppName
  params: {
    name: containerAppName
    image: '${baseImageUrl}webapi:${imageTag}'
    location: location
    envVariables: containerAppEnvVars
    containerAppEnvId: containerAppEnvironment.id
    apimIp: apimIp
    tags: tags
    resources: resources
    probes: probes
    revisionSuffix: revisionSuffix
    scale: scale
    userAssignedIdentityId: managedIdentity.id
  }
}

module keyVaultReaderAccessPolicy '../../modules/keyvault/addReaderRoles.bicep' = {
  name: 'keyVaultReaderAccessPolicy-${containerAppName}'
  params: {
    keyvaultName: environmentKeyVaultResource.name
    principalIds: [managedIdentity.properties.principalId]
  }
}

module appConfigReaderAccessPolicy '../../modules/appConfiguration/addReaderRoles.bicep' = {
  name: 'appConfigReaderAccessPolicy-${containerAppName}'
  params: {
    appConfigurationName: appConfigurationName
    principalIds: [managedIdentity.properties.principalId]
  }
}

output name string = containerApp.outputs.name
output revisionName string = containerApp.outputs.revisionName
