using './main.bicep'

param environment = 'prod'
param location = 'norwayeast'
param keyVaultSourceKeys = json(readEnvironmentVariable('AZURE_KEY_VAULT_SOURCE_KEYS'))

param redisVersion = '6.0'

param containerAppEnvZoneRedundancyEnabled = true

// Workload profiles configuration
param workloadProfiles = [
  {
    name: 'Consumption'
    workloadProfileType: 'Consumption'
  }
  {
    name: 'Dedicated-D8'
    workloadProfileType: 'D8'
    minimumCount: 3
    maximumCount: 10
  }
]

// secrets
param dialogportenPgAdminPassword = readEnvironmentVariable('PG_ADMIN_PASSWORD')
param sourceKeyVaultSubscriptionId = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_SUBSCRIPTION_ID')
param sourceKeyVaultResourceGroup = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_RESOURCE_GROUP')
param sourceKeyVaultName = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_NAME')
param sourceKeyVaultSshJumperSshPublicKey = readEnvironmentVariable('AZURE_SOURCE_KEY_VAULT_SSH_JUMPER_SSH_PUBLIC_KEY')

// SKUs
param keyVaultSku = {
  name: 'standard'
  family: 'A'
}
param appConfigurationSku = {
  name: 'standard'
}
param appInsightsSku = {
  name: 'PerGB2018'
}
param postgresConfiguration = {
  sku: {
    name: 'Standard_E16ads_v5'
    tier: 'MemoryOptimized'
  }
  storage: {
    storageSizeGB: 4096
    autoGrow: 'Enabled'
    type: 'Premium_LRS'
    tier: 'P50'
  }
  // Enabling index tuning will practically also enable query performance insight
  enableIndexTuning: true
  enableQueryPerformanceInsight: false
  parameterLogging: {
    enabled: false
  }
  highAvailability: {
    mode: 'Disabled'
    standbyAvailabilityZone: '2'
  }
  backupRetentionDays: 32
  availabilityZone: '3'
  enableBackupVault: true
}

param deployerPrincipalName = 'GitHub: altinn/dialogporten - Prod'

param redisSku = {
  name: 'Standard'
  family: 'C'
  capacity: 1
}

param serviceBusSku = {
  name: 'Premium'
  tier: 'Premium'
  capacity: 1
}

// Altinn Product Dialogporten: Developers Prod
param sshJumperAdminLoginGroupObjectId = 'a94de4bf-0a83-4d30-baba-0c6a7365571c'

param apimUrl = 'https://platform.altinn.no/dialogporten'
