@description('The prefix used for naming resources to ensure unique names')
param namePrefix string

@description('The location where the resources will be deployed')
param location string

@description('Tags to apply to resources')
param tags object

// Network address ranges
var vnetAddressPrefix = '10.0.0.0/16'

// Subnet address prefixes
var defaultSubnetPrefix = '10.0.0.0/24'
var postgresqlSubnetPrefix = '10.0.1.0/24'
var containerAppEnvSubnetPrefix = '10.0.2.0/23'  // required size for the container app environment is /23
var serviceBusSubnetPrefix = '10.0.4.0/24'
var redisSubnetPrefix = '10.0.5.0/24'
var sshJumperSubnetPrefix = '10.0.6.0/24'

resource defaultNSG 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: '${namePrefix}-default-nsg'
  location: location
  properties: {
    securityRules: [
      // todo: restrict the ports further
      {
        name: 'AllowAnyCustomAnyInbound'
        type: 'Microsoft.Network/networkSecurityGroups/securityRules'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 100
          direction: 'Inbound'
        }
      }
      {
        name: 'AllowAnyCustomAnyOutbound'
        type: 'Microsoft.Network/networkSecurityGroups/securityRules'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 100
          direction: 'Outbound'
        }
      }
    ]
  }
  tags: tags
}

// https://learn.microsoft.com/en-us/azure/container-apps/firewall-integration?tabs=consumption-only
resource containerAppEnvironmentNSG 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: '${namePrefix}-container-app-environment-nsg'
  location: location
  properties: {
    securityRules: [
      {
        name: 'AllowAnyCustomAnyOutbound'
        type: 'Microsoft.Network/networkSecurityGroups/securityRules'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 110
          direction: 'Outbound'
          sourcePortRanges: []
          destinationPortRanges: []
          sourceAddressPrefixes: []
          destinationAddressPrefixes: []
        }
      }
      {
        name: 'container-app'
        type: 'Microsoft.Network/networkSecurityGroups/securityRules'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 120
          direction: 'Inbound'
          sourcePortRanges: []
          destinationPortRanges: ['80', '443']
          sourceAddressPrefixes: []
          destinationAddressPrefixes: []
        }
      }
      {
        name: 'container-app-environment'
        type: 'Microsoft.Network/networkSecurityGroups/securityRules'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '10.0.0.62'
          access: 'Allow'
          priority: 130
          direction: 'Inbound'
          sourcePortRanges: []
          destinationPortRanges: ['80', '443']
          sourceAddressPrefixes: []
          destinationAddressPrefixes: []
        }
      }
      {
        name: 'load-balancer'
        type: 'Microsoft.Network/networkSecurityGroups/securityRules'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '30000-32767'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 140
          direction: 'Inbound'
          sourcePortRanges: []
          destinationPortRanges: []
          sourceAddressPrefixes: []
          destinationAddressPrefixes: []
        }
      }
      // remove once we want a more fine grained control over the ports
      {
        name: 'AllowAnyCustomAnyInbound'
        type: 'Microsoft.Network/networkSecurityGroups/securityRules'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 150
          direction: 'Inbound'
          sourcePortRanges: []
          destinationPortRanges: []
          sourceAddressPrefixes: []
          destinationAddressPrefixes: []
        }
      }
    ]
  }
  tags: tags
}

resource postgresqlNSG 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: '${namePrefix}-postgresql-nsg'
  location: location
  properties: {
    securityRules: [
      // todo: restrict the ports furter: https://learn.microsoft.com/en-us/azure/postgresql/flexible-server/concepts-networking-private#virtual-network-concepts
      {
        name: 'AllowAnyCustomAnyInbound'
        type: 'Microsoft.Network/networkSecurityGroups/securityRules'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 100
          direction: 'Inbound'
        }
      }
      {
        name: 'AllowAnyCustomAnyOutbound'
        type: 'Microsoft.Network/networkSecurityGroups/securityRules'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 100
          direction: 'Outbound'
        }
      }
    ]
  }
  tags: tags
}

resource redisNSG 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: '${namePrefix}-redis-nsg'
  location: location
  properties: {
    securityRules: [
      // todo: restrict the ports furter: https://learn.microsoft.com/en-us/azure/postgresql/flexible-server/concepts-networking-private#virtual-network-concepts
      {
        name: 'AllowAnyCustomAnyInbound'
        type: 'Microsoft.Network/networkSecurityGroups/securityRules'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 100
          direction: 'Inbound'
        }
      }
      {
        name: 'AllowAnyCustomAnyOutbound'
        type: 'Microsoft.Network/networkSecurityGroups/securityRules'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 100
          direction: 'Outbound'
        }
      }
    ]
  }
  tags: tags
}

resource serviceBusNSG 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: '${namePrefix}-service-bus-nsg'
  location: location
  properties: {
    securityRules: [
      // todo: make more restrictive
      {
        name: 'AllowAnyCustomAnyInbound'
        type: 'Microsoft.Network/networkSecurityGroups/securityRules'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 100
          direction: 'Inbound'
        }
      }
      {
        name: 'AllowAnyCustomAnyOutbound'
        type: 'Microsoft.Network/networkSecurityGroups/securityRules'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 100
          direction: 'Outbound'
        }
      }
    ]
  }
  tags: tags
}

resource sshJumperNSG 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: '${namePrefix}-ssh-jumper-nsg'
  location: location
  properties: {
    securityRules: [
      {
        name: 'AllowAnyCustomAnyOutbound'
        type: 'Microsoft.Network/networkSecurityGroups/securityRules'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 100
          direction: 'Outbound'
        }
      }
    ]
  }
  tags: tags
}

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: '${namePrefix}-vnet'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        vnetAddressPrefix
      ]
    }
    subnets: [
      {
        name: 'default'
        properties: {
          addressPrefix: defaultSubnetPrefix
          networkSecurityGroup: {
            id: defaultNSG.id
          }
        }
      }
      {
        name: 'postgresqlSubnet'
        properties: {
          addressPrefix: postgresqlSubnetPrefix
          networkSecurityGroup: {
            id: postgresqlNSG.id
          }
          delegations: [
            {
              name: 'postgresql'
              properties: {
                serviceName: 'Microsoft.DBforPostgreSQL/flexibleServers'
              }
            }
          ]
          serviceEndpoints: [
            {
              service: 'Microsoft.Storage'
              locations: [location]
            }
          ]
        }
      }
      {
        name: 'containerAppEnvSubnet'
        properties: {
          addressPrefix: containerAppEnvSubnetPrefix
          networkSecurityGroup: {
            id: containerAppEnvironmentNSG.id
          }
          privateLinkServiceNetworkPolicies: 'Disabled'
          delegations: [
            {
              name: 'containerAppEnvironment'
              properties: {
                serviceName: 'Microsoft.App/environments'
              }
            }
          ]
        }
      }
      {
        name: 'serviceBusSubnet'
        properties: {
          addressPrefix: serviceBusSubnetPrefix
          networkSecurityGroup: {
            id: serviceBusNSG.id
          }
        }
      }
      {
        name: 'redisSubnet'
        properties: {
          addressPrefix: redisSubnetPrefix
          networkSecurityGroup: {
            id: redisNSG.id
          }
        }
      }
      {
        name: 'sshJumperSubnet'
        properties: {
          addressPrefix: sshJumperSubnetPrefix
          networkSecurityGroup: {
            id: sshJumperNSG.id
          }
        }
      }
    ]
  }
  tags: tags
}

output virtualNetworkName string = virtualNetwork.name
output virtualNetworkId string = virtualNetwork.id
output defaultSubnetId string = resourceId('Microsoft.Network/virtualNetworks/subnets', virtualNetwork.name, 'default')
output postgresqlSubnetId string = resourceId(
  'Microsoft.Network/virtualNetworks/subnets',
  virtualNetwork.name,
  'postgresqlSubnet'
  )
output containerAppEnvironmentSubnetId string = resourceId(
  'Microsoft.Network/virtualNetworks/subnets',
  virtualNetwork.name,
  'containerAppEnvSubnet'
  )
output serviceBusSubnetId string = resourceId(
  'Microsoft.Network/virtualNetworks/subnets',
  virtualNetwork.name,
  'serviceBusSubnet'
  )
output redisSubnetId string = resourceId(
  'Microsoft.Network/virtualNetworks/subnets',
  virtualNetwork.name,
  'redisSubnet'
  )
output sshJumperSubnetId string = resourceId('Microsoft.Network/virtualNetworks/subnets', virtualNetwork.name, 'sshJumperSubnet')
