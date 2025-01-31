param appConfigurationName string
param principalIds array

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
    name: appConfigurationName
}

@description('This is the built-in App Configuration Data Reader role. See https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#app-configuration-data-reader')
resource dataReaderRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
    scope: subscription()
    name: '516239f1-63e1-4d78-a4de-a74fb236a071'
}

resource roleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for principalId in principalIds: {
    scope: appConfig
    name: guid(subscription().id, principalId, dataReaderRoleDefinition.id)
    properties: {
        roleDefinitionId: dataReaderRoleDefinition.id
        principalId: principalId
        principalType: 'ServicePrincipal'
    }
}]
