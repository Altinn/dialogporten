@description('The prefix used for naming resources to ensure unique names')
param namePrefix string

@description('The location where the resources will be deployed')
param location string

@description('Tags to apply to resources')
param tags object

@description('Whether to purge data immediately after 30 days in Application Insights')
param immediatePurgeDataOn30Days bool

@export()
type Sku = {
  name:
    | 'PerGB2018'
    | 'CapacityReservation'
    | 'Free'
    | 'LACluster'
    | 'PerGB2018'
    | 'PerNode'
    | 'Premium'
    | 'Standalone'
    | 'Standard'
}

@description('The SKU of the Application Insights')
param sku Sku

resource appInsightsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${namePrefix}-insightsWorkspace'
  location: location
  properties: {
    features: {
        immediatePurgeDataOn30Days: immediatePurgeDataOn30Days
    }
    retentionInDays: 30
    sku: sku
    workspaceCapping: {
      dailyQuotaGb: -1
    }
  }
  tags: tags
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${namePrefix}-applicationInsights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: appInsightsWorkspace.id
    Flow_Type: 'Bluefield'
    Request_Source: 'rest'
  }
  tags: tags
}

output connectionString string = appInsights.properties.ConnectionString
output appInsightsWorkspaceName string = appInsightsWorkspace.name
output appInsightsName string = appInsights.name
output appInsightsId string = appInsights.id
