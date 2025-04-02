param destKeyVaultName string
param secretName string
param tags object
@secure()
param secretValue string

resource secret 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  name: '${destKeyVaultName}/${secretName}'
  properties: {
    value: secretValue
  }
  tags: tags
}

output secretUri string = secret.properties.secretUri
