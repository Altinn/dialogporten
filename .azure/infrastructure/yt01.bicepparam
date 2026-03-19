using './main.bicep'

param environment = 'yt01'
param location = 'norwayeast'
param keyVaultSourceKeys = json(readEnvironmentVariable('AZURE_KEY_VAULT_SOURCE_KEYS'))

param appInsightsPurgeDataOn30Days = true

param redisVersion = '6.0'

param containerAppEnvZoneRedundancyEnabled = false

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
    throughput: 1200
  }
  enableIndexTuning: true
  enableQueryPerformanceInsight: true
  backupRetentionDays: 7
  availabilityZone: '1'
  enableBackupVault: false
}

param deployerPrincipalName = 'GitHub: altinn/dialogporten - Dev'

param redisSku = {
  name: 'Basic'
  family: 'C'
  capacity: 1
}

param serviceBusSku = {
  name: 'Standard'
  tier: 'Standard'
  capacity: null
}

param serviceBusVnetEnabled = false

// Altinn Product Dialogporten: Developers Dev
param sshJumperConfig = {
  adminLoginGroupObjectId: 'c12e51e3-5cbd-4229-8a31-5394c423fb5f'
  vmSize: 'Standard_B2als_v2'
}

param apimUrl = 'https://platform.yt01.altinn.cloud/dialogporten'

// Workload profiles configuration
param workloadProfiles = [
  {
    name: 'Consumption'
    workloadProfileType: 'Consumption'
  }
  {
    name: 'Dedicated-D8'
    workloadProfileType: 'D8'
    minimumCount: 0
    maximumCount: 10
  }
]
