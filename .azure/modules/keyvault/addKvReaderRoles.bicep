@description('The name of the Key Vault')
param keyvaultName string

@description('Principal ID to assign the Key Vault Reader role to')
param principalId string

resource keyvault 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: keyvaultName
}

@description('Built-in Key Vault Reader role. Grants management-plane read on the vault including Microsoft.KeyVault/vaults/secrets/readMetadata/action (list secret names + attributes without values), needed by the keyvault-expiry check workflow. See https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles/security#key-vault-reader')
resource keyVaultReaderRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: '21090545-7ca7-4776-b22c-e363652d74d2'
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyvault
  name: guid(keyvault.id, principalId, keyVaultReaderRoleDefinition.id)
  properties: {
    roleDefinitionId: keyVaultReaderRoleDefinition.id
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
