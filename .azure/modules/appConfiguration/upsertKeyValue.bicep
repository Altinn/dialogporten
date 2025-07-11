@description('Specifies the name of the App Configuration store.')
param configStoreName string

@description('Specifies the names of the key-value resources. The name is a combination of key and label with $ as delimiter. The label is optional.')
param key string

@description('Specifies the values of the key-value resources')
param value string

@description('Specifies the content type of the key-value resources. For feature flag set keyValueType=featureFlag. For Key Value reference set keyValueType=featureFlag. Otherwise, it\'s optional.')
param contentType string = ''

@description('Specifies the type of the key-value resources')
@allowed([
    'keyVaultReference'
    'featureFlag'
    'custom'
])
param keyValueType string = 'custom'

@description('Adds tags for the key-value resources')
param tags object = {}

var parsedContentType = keyValueType == 'keyVaultReference' ? 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8' : keyValueType == 'featureFlag' ? 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8' : contentType
var parsedValue = keyValueType == 'keyVaultReference' ? '{"uri":"${value}"}' : value

resource configStore 'Microsoft.AppConfiguration/configurationStores@2024-06-01' existing = {
    name: configStoreName
    resource configStoreKeyValue 'keyValues' = {
        name: key
        properties: {
            value: parsedValue
            contentType: parsedContentType
            tags: tags
        }
    }
}
