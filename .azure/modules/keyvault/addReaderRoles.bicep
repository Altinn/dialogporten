@description('The name of the Key Vault')
param keyvaultName string

@description('Array of principal IDs to assign the Key Vault Secrets User role to')
param principalIds array

resource keyvault 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: keyvaultName
}

@description('This is the built-in Key Vault Secrets User role. See https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles/security#key-vault-secrets-user')
resource keyVaultSecretsUserRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: '4633458b-17de-408a-b874-0445c86b69e6'
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for principalId in principalIds: {
    scope: keyvault
    name: guid(keyvault.id, principalId, keyVaultSecretsUserRoleDefinition.id)
    properties: {
      roleDefinitionId: keyVaultSecretsUserRoleDefinition.id
      principalId: principalId
      principalType: 'ServicePrincipal'
    }
  }
]
