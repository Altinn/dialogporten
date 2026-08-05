param destKeyVaultName string
param secretName string
param tags object
@secure()
param secretValue string

resource secret 'Microsoft.KeyVault/vaults/secrets@2026-02-01' = {
  name: '${destKeyVaultName}/${secretName}'
  properties: {
    value: secretValue
  }
  tags: tags
}

output secretUri string = secret.properties.secretUri
