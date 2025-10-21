@description('The prefix used for naming resources to ensure unique names')
param namePrefix string

@description('The location where the resources will be deployed')
param location string

@description('The tags to apply to the resources')
param tags object

@description('Whether to enable blob public access')
param allowBlobPublicAccess bool = false

@description('Whether to enable shared key access')
param allowSharedKeyAccess bool = false

@description('Whether to enable hierarchical namespace (Data Lake Storage Gen2)')
param enableHierarchicalNamespace bool = false

@description('The access tier for the storage account')
param accessTier 'Hot' | 'Cool' = 'Hot'

var storageAccountName = take('${namePrefix}storage${uniqueString(resourceGroup().id)}', 24)

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: accessTier
    allowBlobPublicAccess: allowBlobPublicAccess
    allowSharedKeyAccess: allowSharedKeyAccess
    isHnsEnabled: enableHierarchicalNamespace
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        blob: {
          enabled: true
        }
        file: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
      requireInfrastructureEncryption: true
    }
  }
  tags: tags
}

output storageAccountName string = storageAccount.name
output storageAccountId string = storageAccount.id
