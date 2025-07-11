import { uniqueResourceName } from '../../functions/resourceName.bicep'

@description('The prefix used for naming resources to ensure unique names')
param namePrefix string

@description('The location where the resources will be deployed')
param location string

@description('Tags to apply to resources')
param tags object

@description('The name of the Log Analytics workspace')
param logAnalyticsWorkspaceName string

@export()
type Sku = {
  name: 'standard'
}

@description('The SKU of the App Configuration')
param sku Sku

var appConfigNameMaxLength = 63
var appConfigName = uniqueResourceName('${namePrefix}-appConfiguration', appConfigNameMaxLength)

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-06-01' = {
  name: appConfigName
  location: location
  sku: sku
  properties: {
    // TODO: Remove
    enablePurgeProtection: false
  }
  resource configStoreKeyValue 'keyValues' = {
    name: 'Sentinel'
    properties: {
      value: '1'
    }
  }
  tags: tags
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2025-02-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${appConfigName}-diagnostics'
  scope: appConfig
  properties: {
    logs: [
      {
        category: 'Audit'
        enabled: true
      }
    ]
    workspaceId: logAnalyticsWorkspace.id
  }
}

output endpoint string = appConfig.properties.endpoint
output name string = appConfig.name
