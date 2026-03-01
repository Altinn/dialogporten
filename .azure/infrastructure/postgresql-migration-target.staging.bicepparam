using './postgresql-migration-target.bicep'

param environment = 'staging'
param location = 'norwayeast'

param dialogportenPgAdminPassword = readEnvironmentVariable('PG_ADMIN_PASSWORD')
param sourceKeyVaultSubscriptionId = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_SUBSCRIPTION_ID')
param sourceKeyVaultResourceGroup = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_RESOURCE_GROUP')
param sourceKeyVaultName = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_NAME')

// Copy this into staging.bicepparam replacing the existing postgresConfiguration when migration is complete
// and remove this file
param postgresConfiguration = {
  serverNameStem: 'postgres2'
  version: '18'
  sku: {
  sku: {
    name: 'Standard_E8ads_v5'
    tier: 'MemoryOptimized'
  }
  storage: {
    storageSizeGB: 256
    type: 'PremiumV2_LRS'
    iops: 3000
    throughput: 125
  }
  enableIndexTuning: false
  enableQueryPerformanceInsight: false
  backupRetentionDays: 7
  availabilityZone: '2'
  enableBackupVault: false
}

param deployerPrincipalName = 'GitHub: altinn/dialogporten - Staging (new postgresql)'
