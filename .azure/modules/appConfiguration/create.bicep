import { uniqueResourceName } from '../../functions/resourceName.bicep'

@description('The prefix used for naming resources to ensure unique names')
param namePrefix string

@description('The location where the resources will be deployed')
param location string

@description('Tags to apply to resources')
param tags object

@description('The name of the Log Analytics workspace')
param logAnalyticsWorkspaceName string

@description('The ID of the subnet where the App Configuration private endpoint will be deployed')
param subnetId string

@description('The ID of the virtual network for the private DNS zone')
param vnetId string

@export()
type Sku = {
  name: 'standard'
}

@description('The SKU of the App Configuration')
param sku Sku

var appConfigNameMaxLength = 63
var appConfigName = uniqueResourceName('${namePrefix}-appConfiguration', appConfigNameMaxLength)

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' = {
  name: appConfigName
  location: location
  sku: sku
  properties: {
    // TODO: Remove
    enablePurgeProtection: false
    publicNetworkAccess: 'Disabled'
  }
  resource configStoreKeyValue 'keyValues' = {
    name: 'Sentinel'
    properties: {
      value: '1'
    }
  }
  tags: tags
}

// private endpoint name max characters is 80
var appConfigPrivateEndpointName = uniqueResourceName('${namePrefix}-app-config-pe', 80)

resource appConfigPrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: appConfigPrivateEndpointName
  location: location
  properties: {
    privateLinkServiceConnections: [
      {
        name: appConfigPrivateEndpointName
        properties: {
          privateLinkServiceId: appConfig.id
          groupIds: [
            'configurationStores'
          ]
        }
      }
    ]
    customNetworkInterfaceName: uniqueResourceName('${namePrefix}-app-config-pe-nic', 80)
    subnet: {
      id: subnetId
    }
  }
  tags: tags
}

module privateDnsZone '../privateDnsZone/main.bicep' = {
  name: '${namePrefix}-app-config-pdz'
  params: {
    namePrefix: namePrefix
    defaultDomain: 'privatelink.azconfig.io'
    vnetId: vnetId
    tags: tags
  }
}

module privateDnsZoneGroup '../privateDnsZoneGroup/main.bicep' = {
  name: '${namePrefix}-app-config-privateDnsZoneGroup'
  params: {
    name: 'default'
    dnsZoneGroupName: 'privatelink-azconfig-io'
    dnsZoneId: privateDnsZone.outputs.id
    privateEndpointName: appConfigPrivateEndpoint.name
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
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
        retentionPolicy: {
          days: 30
          enabled: true
        }
      }
    ]
    workspaceId: logAnalyticsWorkspace.id
  }
}

output endpoint string = appConfig.properties.endpoint
output name string = appConfig.name
