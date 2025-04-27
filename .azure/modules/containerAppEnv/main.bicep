@description('The location where the resources will be deployed')
param location string

@description('The prefix used for naming resources to ensure unique names')
param namePrefix string

@description('The ID of the subnet to be used for the container app environment')
param subnetId string

@description('Tags to apply to resources')
param tags object

@description('The name of the Application Insights workspace')
param appInsightWorkspaceName string

@description('The Application Insights connection string')
param appInsightsConnectionString string

@description('The ID of the user-assigned managed identity')
param userAssignedIdentityId string

resource appInsightsWorkspace 'Microsoft.OperationalInsights/workspaces@2025-02-01' existing = {
  name: appInsightWorkspaceName
}

resource containerAppEnv 'Microsoft.App/managedEnvironments@2025-01-01' = {
  name: '${namePrefix}-cae'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: appInsightsWorkspace.properties.customerId
        sharedKey: appInsightsWorkspace.listKeys().primarySharedKey
      }
    }
    vnetConfiguration: {
      infrastructureSubnetId: subnetId
      internal: false
    }
    appInsightsConfiguration: {
      connectionString: appInsightsConnectionString
    }
    openTelemetryConfiguration: {
      tracesConfiguration: {
        destinations: ['appInsights']
      }
      logsConfiguration: {
        destinations: ['appInsights']
      }
    }
  }
  tags: tags
}

output containerAppEnvId string = containerAppEnv.id
