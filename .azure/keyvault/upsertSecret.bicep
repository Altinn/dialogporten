param destKeyVaultName string
param secretName string
@secure()
param secretValue string

resource secret 'Microsoft.KeyVault/vaults/secrets@2022-11-01' = {
  name: '${destKeyVaultName}/${secretName}'
  properties: {
    value: secretValue
  }
}

output secretUri string = secret.properties.secretUri