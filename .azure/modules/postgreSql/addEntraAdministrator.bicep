// Registers a principal as a Microsoft Entra administrator of a PostgreSQL flexible server.
//
// This exists as its own module because an administrator record is named after the principal's
// object id, and a resource name must be resolvable at the start of the deployment it belongs to.
// The object id of an identity created in the same deployment is not, so it is passed in here as a
// parameter, which the nested deployment does have from the start.

@description('The name of the PostgreSQL flexible server to register the administrator on')
@minLength(3)
param serverName string

@description('The object (principal) id of the principal to register')
@minLength(1)
param principalObjectId string

@description('The principal name recorded for the administrator')
@minLength(1)
param principalName string

@description('The type of the principal being registered')
@allowed(['ServicePrincipal', 'User', 'Group'])
param principalType string = 'ServicePrincipal'

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2025-08-01' existing = {
  name: serverName
}

resource administrator 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2025-08-01' = {
  parent: postgres
  name: principalObjectId
  properties: {
    principalName: principalName
    principalType: principalType
    tenantId: tenant().tenantId
  }
}
