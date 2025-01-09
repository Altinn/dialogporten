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

var namePrefix = 'dp-be-${environment}'
var baseImageUrl = 'ghcr.io/altinn/dialogporten-'
var tags = {
  Environment: environment
  Product: 'Dialogporten'
}

resource appConfiguration 'Microsoft.AppConfiguration/configurationStores@2023-03-01' existing = {
  name: appConfigurationName
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: containerAppEnvironmentName
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
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
]

@description('The scaling configuration for the container app')
param scale Scale = {
  minReplicas: 2
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

resource environmentKeyVaultResource 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: environmentKeyVaultName
}

var containerAppName = '${namePrefix}-webapi-eu-ca'

var port = 8080

var probes = [
  {
    periodSeconds: 5
    initialDelaySeconds: 2
    type: 'Liveness'
    httpGet: {
      path: '/health/liveness'
      port: port
    }
  }
  {
    periodSeconds: 5
    initialDelaySeconds: 2
    type: 'Readiness'
    httpGet: {
      path: '/health/readiness'
      port: port
    }
  }
  {
    periodSeconds: 5
    initialDelaySeconds: 2
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
