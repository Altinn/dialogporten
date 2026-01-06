@description('The name of the storage account to create the container in.')
param storageAccountName string

@description('The name of the blob container.')
param containerName string

@description('Public access level for the container.')
param publicAccess 'None' | 'Blob' | 'Container' = 'None'

@description('Whether to enable blob soft delete (delete retention policy) on the container.')
param enableBlobSoftDelete bool = false

@description('Number of days to retain soft-deleted blobs when blob soft delete is enabled.')
@minValue(1)
@maxValue(365)
param blobSoftDeleteRetentionDays int

resource storageAccount 'Microsoft.Storage/storageAccounts@2025-01-01' existing = {
  name: storageAccountName
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2025-01-01' = {
  name: 'default'
  parent: storageAccount
  properties: {
    deleteRetentionPolicy: {
      enabled: enableBlobSoftDelete
      days: blobSoftDeleteRetentionDays
    }
  }
}

resource storageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2025-01-01' = {
  name: containerName
  parent: blobService
  properties: {
    publicAccess: publicAccess
  }
}

output containerId string = storageContainer.id
output containerName string = storageContainer.name

