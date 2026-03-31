using './postgresql-migration-target.bicep'

param environment = 'test'

param keyVaultSourceKeys = json(readEnvironmentVariable('AZURE_KEY_VAULT_SOURCE_KEYS'))
param dialogportenPgAdminPassword = readEnvironmentVariable('PG_ADMIN_PASSWORD')
param sourceKeyVaultSubscriptionId = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_SUBSCRIPTION_ID')
param sourceKeyVaultResourceGroup = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_RESOURCE_GROUP')
param sourceKeyVaultName = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_NAME')

// After migration is complete, copy this postgresConfiguration block into test.bicepparam
// to make postgres2 the canonical server, then remove this file.
// AT23 stays on Burstable / Premium_LRS (SSDv1) to keep costs down.
param postgresConfiguration = {
  serverNameStem: 'postgres2'
  version: '18'
  sku: {
    name: 'Standard_B2s'
    tier: 'Burstable'
  }
  storage: {
    storageSizeGB: 32
    autoGrow: 'Enabled'
    type: 'Premium_LRS'
    tier: 'P4'
  }
  enableIndexTuning: false
  enableQueryPerformanceInsight: false
  backupRetentionDays: 7
  availabilityZone: '1'
  enableBackupVault: false
}

param deployerPrincipalName = 'GitHub: altinn/dialogporten - Dev'
