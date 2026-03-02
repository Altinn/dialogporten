using './postgresql-migration-target.bicep'

param environment = 'test'
param location = 'norwayeast'

param dialogportenPgAdminPassword = readEnvironmentVariable('PG_ADMIN_PASSWORD')
param sourceKeyVaultSubscriptionId = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_SUBSCRIPTION_ID')
param sourceKeyVaultResourceGroup = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_RESOURCE_GROUP')
param sourceKeyVaultName = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_NAME')

// After migrations are complete, remove this file and let the current (burstable, SSDv1-tier) server
// remain the canoncial "test"-server.
param postgresConfiguration = {
  serverNameStem: 'postgres2'
  version: '18'
  sku: {
    name: 'Standard_E4ads_v5'
    tier: 'MemoryOptimized'
  }
  storage: {
    storageSizeGB: 32
    type: 'PremiumV2_LRS'
    iops: 3000
    throughput: 125
  }
  enableIndexTuning: false
  enableQueryPerformanceInsight: false
  backupRetentionDays: 7
  availabilityZone: '1'
  enableBackupVault: false
}

param deployerPrincipalName = 'GitHub: altinn/dialogporten - Test (new postgresql)'
