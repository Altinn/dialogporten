@description('The prefix used for naming resources to ensure unique names')
param namePrefix string

@description('The location where the resources will be deployed')
param location string

@description('Tags to apply to resources')
param tags object

@export()
type Sku = {
  name: 'premium' | 'standard'
  family: 'A'
}

@description('The SKU of the Key Vault')
param sku Sku

var keyVaultName = take('${namePrefix}-kv-${uniqueString(resourceGroup().id)}', 24)

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: keyVaultName
  location: location
  properties: {
    enablePurgeProtection: true
    enabledForTemplateDeployment: false
    sku: sku
    tenantId: subscription().tenantId
    accessPolicies: []
    enableRbacAuthorization: true
  }
  tags: tags
}

output name string = keyVault.name
