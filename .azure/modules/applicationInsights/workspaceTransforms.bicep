@export()
type WorkspaceTransformConfiguration = {
  name: string
  destinationName: string
  transformations: {
    table: string
    @description('KQL without line comments; line endings are replaced with spaces for deployment')
    transformKql: string
  }[]
}

param configuration WorkspaceTransformConfiguration
param workspaceName string
param workspaceResourceId string
param location string
param tags object

@description('The same managed workspace properties used when creating the workspace')
param workspaceProperties object

// WorkspaceTransforms applies inside the destination Log Analytics workspace.
// It uses no credentials or outbound authentication, so no managed identity is needed.
// https://learn.microsoft.com/en-us/azure/azure-monitor/data-collection/data-collection-transformations-create#create-workspace-transformation-dcr
resource dataCollectionRule 'Microsoft.Insights/dataCollectionRules@2024-03-11' = {
  name: configuration.name
  location: location
  kind: 'WorkspaceTransforms'
  tags: tags
  properties: {
    destinations: {
      logAnalytics: [
        {
          name: configuration.destinationName
          workspaceResourceId: workspaceResourceId
        }
      ]
    }
    dataFlows: [for transformation in configuration.transformations: {
      streams: [
        'Microsoft-Table-${transformation.table}'
      ]
      destinations: [
        configuration.destinationName
      ]
      transformKql: trim(replace(replace(transformation.transformKql, '\r\n', ' '), '\n', ' '))
    }]
  }
}

// Reapply all managed properties when linking the DCR so the workspace update
// preserves retention, SKU, ingestion quota and purge settings.
resource workspace 'Microsoft.OperationalInsights/workspaces@2025-07-01' = {
  name: workspaceName
  location: location
  tags: tags
  properties: union(workspaceProperties, {
    defaultDataCollectionRuleResourceId: dataCollectionRule.id
  })
}
