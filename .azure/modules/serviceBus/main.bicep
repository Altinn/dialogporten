// This Bicep module provisions a Service Bus namespace with a Premium SKU in Azure,
// assigns a system-managed identity, and sets up a private endpoint for secure connectivity.
// It also configures a private DNS zone for the Service Bus namespace to facilitate network resolution within the virtual network.
import { uniqueResourceName } from '../../functions/resourceName.bicep'

@description('The prefix used for naming resources to ensure unique names')
param namePrefix string

@description('The location where the resources will be deployed')
param location string

@description('The ID of the subnet where the Service Bus will be deployed')
param subnetId string

@description('The ID of the virtual network for the private DNS zone')
param vnetId string

@description('Tags to apply to resources')
param tags object

@export()
type Sku = {
  name: 'Premium'
  tier: 'Premium'
  @minValue(1)
  capacity: int
}

@description('The SKU of the Service Bus')
param sku Sku

var serviceBusNameMaxLength = 50
var serviceBusName = uniqueResourceName('${namePrefix}-service-bus', serviceBusNameMaxLength)

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: serviceBusName
  location: location
  sku: sku
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    publicNetworkAccess: 'Disabled'
  }
  tags: tags
}

// private endpoint name max characters is 80
var serviceBusPrivateEndpointName = uniqueResourceName('${namePrefix}-service-bus-pe', 80)

resource serviceBusPrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
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
      id: subnetId
    }
  }
  tags: tags
}

module privateDnsZone '../privateDnsZone/main.bicep' = {
  name: '${namePrefix}-service-bus-pdz'
  params: {
    namePrefix: namePrefix
    defaultDomain: 'privatelink.servicebus.windows.net'
    vnetId: vnetId
    tags: tags
  }
}

module privateDnsZoneGroup '../privateDnsZoneGroup/main.bicep' = {
  name: '${namePrefix}-service-bus-privateDnsZoneGroup'
  params: {
    name: 'default'
    dnsZoneGroupName: 'privatelink-servicebus-windows-net'
    dnsZoneId: privateDnsZone.outputs.id
    privateEndpointName: serviceBusPrivateEndpoint.name
  }
}
