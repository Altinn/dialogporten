// This Bicep module provisions a Service Bus namespace in Azure with support for all SKU tiers.
// For Premium tier, it configures a private endpoint and private DNS zone for secure VNET connectivity.
// For Standard/Basic tiers, it uses public access (SAS and Entra ID authentication are both enabled).
import { uniqueResourceName } from '../../functions/resourceName.bicep'

@description('The prefix used for naming resources to ensure unique names')
param namePrefix string

@description('The location where the resources will be deployed')
param location string

@description('The ID of the subnet where the Service Bus will be deployed (Premium tier only)')
param subnetId string?

@description('The ID of the virtual network for the private DNS zone (Premium tier only)')
param vnetId string?

@description('Tags to apply to resources')
param tags object

@export()
type Sku = {
  name: 'Basic' | 'Standard' | 'Premium'
  tier: 'Basic' | 'Standard' | 'Premium'
  @minValue(1)
  capacity: int?
}

@description('The SKU of the Service Bus')
param sku Sku

var serviceBusNameMaxLength = 50
var serviceBusName = uniqueResourceName('${namePrefix}-service-bus', serviceBusNameMaxLength)
var premiumNetworkValidation = sku.name == 'Premium' && (empty(subnetId) || empty(vnetId))
  ? fail('Premium SKU requires both subnetId and vnetId to be provided')
  : true
var isPremium = sku.name == 'Premium' && premiumNetworkValidation

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: serviceBusName
  location: location
  sku: sku
  identity: {
    type: 'SystemAssigned'
  }
  properties: isPremium ? {
    publicNetworkAccess: 'Disabled'
  } : {}
  tags: tags
}

// private endpoint name max characters is 80
var serviceBusPrivateEndpointName = uniqueResourceName('${namePrefix}-service-bus-pe', 80)

resource serviceBusPrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = if (isPremium) {
  name: serviceBusPrivateEndpointName
  location: location
  properties: {
    privateLinkServiceConnections: [
      {
        name: serviceBusPrivateEndpointName
        properties: {
          privateLinkServiceId: serviceBusNamespace.id
          groupIds: [
            'namespace'
          ]
        }
      }
    ]
    customNetworkInterfaceName: uniqueResourceName('${namePrefix}-service-bus-pe-nic', 80)
    subnet: {
      id: subnetId!
    }
  }
  tags: tags
}

module privateDnsZone '../privateDnsZone/main.bicep' = if (isPremium) {
  name: '${namePrefix}-service-bus-pdz'
  params: {
    namePrefix: namePrefix
    defaultDomain: 'privatelink.servicebus.windows.net'
    vnetId: vnetId!
    tags: tags
  }
}

module privateDnsZoneGroup '../privateDnsZoneGroup/main.bicep' = if (isPremium) {
  name: '${namePrefix}-service-bus-privateDnsZoneGroup'
  params: {
    name: 'default'
    dnsZoneGroupName: 'privatelink-servicebus-windows-net'
    dnsZoneId: privateDnsZone.outputs.id
    privateEndpointName: serviceBusPrivateEndpoint.name
  }
}
