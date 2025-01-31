/*
  This module copies secrets from a source Key Vault to a destination Key Vault and adds references to those secrets in App Configuration.
*/
// Source
@description('Array of keys from the source Key Vault')
param srcKeyVaultKeys array

@description('Name of the source Key Vault')
param srcKeyVaultName string

@description('Resource group name of the source Key Vault')
param srcKeyVaultRGNName string = resourceGroup().name

@description('Subscription ID of the source Key Vault')
param srcKeyVaultSubId string = subscription().subscriptionId

// Destination
@description('Name of the destination Key Vault')
param destKeyVaultName string

@description('Resource group name of the destination Key Vault')
param destKeyVaultRGName string = resourceGroup().name

@description('Subscription ID of the destination Key Vault')
param destKeyVaultSubId string = subscription().subscriptionId

// App configuration
@description('Name of the App Configuration to copy secret references to')
param appConfigurationName string

@description('Tags to apply to resources')
param tags object

// Secret
@description('Prefix for the secret names')
#disable-next-line secure-secrets-in-params
param secretPrefix string

var filteredKeysBySecretPrefix = filter(srcKeyVaultKeys, key => startsWith(key, secretPrefix))

var keys = map(filteredKeysBySecretPrefix, key => {
  secretNameWithoutPrefix: replace(key, secretPrefix, '')
  secretName: key
  appConfigKey: replace(replace(key, secretPrefix, ''), '--', ':')
})

resource srcKeyVaultResource 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: srcKeyVaultName
  scope: resourceGroup(srcKeyVaultSubId, srcKeyVaultRGNName)
}

resource appConfigurationResource 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
  name: appConfigurationName
}

module secrets 'upsertSecret.bicep' = [
  for key in keys: {
    name: '${take(key.secretName, 57)}-${take(uniqueString(key.secretName), 6)}'
    scope: resourceGroup(destKeyVaultSubId, destKeyVaultRGName)
    params: {
      destKeyVaultName: destKeyVaultName
      secretName: key.secretNameWithoutPrefix
      secretValue: srcKeyVaultResource.getSecret(key.secretName)
      tags: tags
    }
  }
]

module appConfiguration '../appConfiguration/upsertKeyValue.bicep' = [
  for key in keys: {
    name: '${take(key.secretNameWithoutPrefix, 57)}-${take(uniqueString(key.secretNameWithoutPrefix), 6)}'
    scope: resourceGroup(destKeyVaultSubId, destKeyVaultRGName)
    params: {
      configStoreName: appConfigurationResource.name
      key: key.appConfigKey
      value: 'https://${destKeyVaultName}${az.environment().suffixes.keyvaultDns}/secrets/${key.secretNameWithoutPrefix}'
      keyValueType: 'keyVaultReference'
      tags: tags
    }
  }
]
