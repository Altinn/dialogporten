import {
  uniqueResourceName
} from '../../functions/resourceName.bicep'

@description('The prefix used for naming resources to ensure unique names')
param namePrefix string

@description('The location where the resources will be deployed')
param location string

@description('The ID of the subnet for the Private Link')
param subnetId string

@description('Tags to apply to resources')
param tags object

@description('The ID of the virtual network for the private DNS zone')
param vnetId string

@description('The name of the environment Key Vault')
@minLength(1)
param environmentKeyVaultName string

@description('The version of the Redis instance')
@minLength(1)
param version string

@export()
type Sku = {
  name: 'Basic' | 'Standard' | 'Premium'
  family: 'C' | 'P'
  @minValue(1)
  capacity: int
}

@description('The SKU of the Redis instance')
param sku Sku

var redisNameMaxLength = 63
var redisName = uniqueResourceName('${namePrefix}-redis', redisNameMaxLength)

// https://learn.microsoft.com/en-us/azure/templates/microsoft.cache/redis?pivots=deployment-language-bicep
resource redis 'Microsoft.Cache/Redis@2024-11-01' = {
  name: redisName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    sku: sku
    enableNonSslPort: false
    redisConfiguration: {
      'aad-enabled': 'true'
      'maxmemory-policy': 'allkeys-lru'
    }
    redisVersion: version
    publicNetworkAccess: 'Disabled'
  }
  tags: tags
}

// private endpoint name max characters is 80
var redisPrivateEndpointName = uniqueResourceName('${namePrefix}-redis-pe', 80)

resource redisPrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: redisPrivateEndpointName
  location: location
  properties: {
    privateLinkServiceConnections: [
      {
        name: redisPrivateEndpointName
        properties: {
          privateLinkServiceId: redis.id
          groupIds: [
            'redisCache'
          ]
        }
      }
    ]
    customNetworkInterfaceName: uniqueResourceName('${namePrefix}-redis-pe-nic', 80)
    subnet: {
      id: subnetId
    }
  }
  tags: tags
}

module privateDnsZone '../privateDnsZone/main.bicep' = {
  name: '${namePrefix}-redis-pdz'
  params: {
    namePrefix: namePrefix
    defaultDomain: 'privatelink.redis.cache.windows.net'
    vnetId: vnetId
    tags: tags
  }
}

module privateDnsZoneGroup '../privateDnsZoneGroup/main.bicep' = {
  name: '${namePrefix}-redis-privateDnsZoneGroup'
  params: {
    name: 'default'
    dnsZoneGroupName: 'privatelink-redis-cache-windows-net'
    dnsZoneId: privateDnsZone.outputs.id
    privateEndpointName: redisPrivateEndpoint.name
  }
}

// Until managed identity is supported in the Redis for IDistributedCache, we need to use a connection string
// https://github.com/dotnet/aspnetcore/issues/54414
module redisConnectionString '../keyvault/upsertSecret.bicep' = {
  name: 'redisConnectionString'
  params: {
    destKeyVaultName: environmentKeyVaultName
    secretName: 'dialogportenRedisConnectionString'
    secretValue: '${redis.properties.hostName}:${redis.properties.sslPort},password=${redis.properties.accessKeys.primaryKey},ssl=True,abortConnect=False'
    tags: tags
  }
}

output connectionStringSecretUri string = redisConnectionString.outputs.secretUri
