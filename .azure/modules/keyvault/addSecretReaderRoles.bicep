@description('The name of the Key Vault')
param keyvaultName string

@description('The workload managed identity principal ID')
param principalId string

@description('The runtime secrets this workload needs. Database administrator credentials must not be included.')
@minLength(1)
param secretNames string[]

var databaseCredentials = [
  'dialogportenadoconnectionstring'
  'dialogportenpsqlconnectionstring'
  'dialogportenpgadminpassword'
]
var allowedSecretNames = empty(filter(secretNames, name => contains(databaseCredentials, toLower(name))))
  ? secretNames
  : fail('Token-authenticated workloads must not receive access to database administrator credentials.')

resource keyvault 'Microsoft.KeyVault/vaults@2026-02-01' existing = {
  name: keyvaultName
}

resource secrets 'Microsoft.KeyVault/vaults/secrets@2026-02-01' existing = [for name in allowedSecretNames: {
  parent: keyvault
  name: name
}]

resource secretsUserRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: '4633458b-17de-408a-b874-0445c86b69e6'
}

// Incremental deployments do not delete the former vault-wide assignment. Revoke it explicitly
// after the workload has switched to token authentication; see the provisioner rollout guide.
resource roleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for (name, i) in allowedSecretNames: {
  scope: secrets[i]
  name: guid(secrets[i].id, principalId, secretsUserRole.id)
  properties: {
    roleDefinitionId: secretsUserRole.id
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}]
