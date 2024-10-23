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

var namePrefix = 'dp-be-${environment}'
var baseImageUrl = 'ghcr.io/digdir/dialogporten-'
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
  name: '${namePrefix}-service-identity'
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
    name: 'Infrastructure__MassTransit__Host'
    value: 'sb://${serviceBusNamespaceName}.servicebus.windows.net/'
  }
]

resource environmentKeyVaultResource 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: environmentKeyVaultName
}

var serviceName = 'service'

var containerAppName = '${namePrefix}-${serviceName}'

var port = 8080

var probes = [
  {
    periodSeconds: 5
    failureThreshold: 10
    initialDelaySeconds: 20
    timeoutSeconds: 10
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
    probes: probes
    port: port
    revisionSuffix: revisionSuffix
    userAssignedIdentityId: managedIdentity.id
    scale: scale
    // TODO: Once all container apps use user-assigned identities, remove this comment and ensure userAssignedIdentityId is always provided
  }
  dependsOn: [
    keyVaultReaderAccessPolicy
    appConfigReaderAccessPolicy
    serviceBusOwnerAccessPolicy
  ]
}

output name string = containerApp.outputs.name
output revisionName string = containerApp.outputs.revisionName
