@description('The name of the storage account')
param storageAccountName string

@description('Array of principal IDs to assign the Storage Blob Data Contributor role to')
param principalIds array

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

@description('This is the built-in Storage Blob Data Contributor role. See https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#storage-blob-data-contributor')
resource storageBlobDataContributorRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for principalId in principalIds: {
    scope: storageAccount
    name: guid(storageAccount.id, principalId, storageBlobDataContributorRoleDefinition.id)
    properties: {
      roleDefinitionId: storageBlobDataContributorRoleDefinition.id
      principalId: principalId
      principalType: 'ServicePrincipal'
    }
  }
]