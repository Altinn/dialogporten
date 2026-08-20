@description('The prefix used for naming resources to ensure unique names')
param namePrefix string

@description('The location where the resources will be deployed')
param location string

@description('Tags to apply to resources')
param tags object

@export()
type Sku = {
  name: 'premium' | 'standard'
  family: 'A'
}

@description('The SKU of the Key Vault')
param sku Sku

@description('The name of the Application Insights workspace receiving the vault audit log')
param appInsightWorkspaceName string

var keyVaultName = take('${namePrefix}-kv-${uniqueString(resourceGroup().id)}', 24)

resource keyVault 'Microsoft.KeyVault/vaults@2026-02-01' = {
  name: keyVaultName
  location: location
  properties: {
    enablePurgeProtection: true
    enabledForTemplateDeployment: false
    sku: sku
    tenantId: subscription().tenantId
    accessPolicies: []
    enableRbacAuthorization: true
  }
  tags: tags
}

resource appInsightsWorkspace 'Microsoft.OperationalInsights/workspaces@2025-07-01' existing = {
  name: appInsightWorkspaceName
}

// Audit logging of vault access (SecretGet etc. with caller identity) to our own
// workspace, resource-specific table AZKVAuditLogs. The security team ships the same
// category to their Sentinel workspace through a separate diagnostic setting; Azure
// allows both as long as no two settings send one category to the same destination.
// Retention is governed by the workspace, not here.
resource diagnosticSetting 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'KeyVaultDiagnosticSetting'
  scope: keyVault
  properties: {
    workspaceId: appInsightsWorkspace.id
    logAnalyticsDestinationType: 'Dedicated'
    logs: [
      {
        category: 'AuditEvent'
        enabled: true
      }
    ]
  }
}

output name string = keyVault.name
