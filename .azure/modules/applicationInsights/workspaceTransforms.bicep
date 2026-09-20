@export()
type WorkspaceTransformConfiguration = {
  name: string
  destinationName: string
  transformations: {
    table: string
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
      transformKql: transformation.transformKql
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
