using './postgresql-migration-target.bicep'

param environment = 'yt01'
param location = 'norwayeast'

param dialogportenPgAdminPassword = readEnvironmentVariable('PG_ADMIN_PASSWORD')
param sourceKeyVaultSubscriptionId = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_SUBSCRIPTION_ID')
param sourceKeyVaultResourceGroup = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_RESOURCE_GROUP')
param sourceKeyVaultName = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_NAME')

// Copy this into yt01.bicepparam replacing the existing postgresConfiguration when migration is complete
// and remove this file
param postgresConfiguration = {
  serverNameStem: 'postgres2'
  version: '18'
  sku: {
    name: 'Standard_E48ads_v5'
    tier: 'MemoryOptimized'
  }
  storage: {
    storageSizeGB: 4096
    type: 'PremiumV2_LRS'
    iops: 24000
    throughput: 12000
  }
  enableIndexTuning: false
  enableQueryPerformanceInsight: false
  backupRetentionDays: 1
  availabilityZone: '1'
  enableBackupVault: false
}

param deployerPrincipalName = 'GitHub: altinn/dialogporten - YT01 (new postgresql)'
