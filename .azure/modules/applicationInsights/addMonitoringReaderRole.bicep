@description('The name of the Application Insights resource')
param appInsightsName string

@description('Array of principal IDs to assign the Monitoring Reader role to')
param principalIds array

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

@description('This is the built-in Monitoring Reader role. See https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#monitoring-reader')
resource monitoringReaderRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: '43d0d8ad-25c7-4714-9337-8ba259a9fe05'
}

resource roleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for principalId in principalIds: {
    scope: appInsights
    name: guid(subscription().id, principalId, monitoringReaderRole.id)
    properties: {
      roleDefinitionId: monitoringReaderRole.id
      principalId: principalId
      principalType: 'ServicePrincipal'
    }
  }
]
