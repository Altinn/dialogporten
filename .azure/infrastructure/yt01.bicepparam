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
  sku: {
    name: 'Standard_D16ads_v5'
    tier: 'GeneralPurpose'
  }
  storage: {
    storageSizeGB: 4096
    autoGrow: 'Enabled'
    type: 'Premium_LRS'
    tier: 'P50'
  }
  enableIndexTuning: true
  enableQueryPerformanceInsight: true
  backupRetentionDays: 7
  availabilityZone: '1'
}

param deployerPrincipalName = 'GitHub: altinn/dialogporten - Dev'

param redisSku = {
  name: 'Basic'
  family: 'C'
  capacity: 1
}

param serviceBusSku = {
  name: 'Premium'
  tier: 'Premium'
  capacity: 1
}
// Altinn Product Dialogporten: Developers Dev
param sshJumperAdminLoginGroupObjectId = 'c12e51e3-5cbd-4229-8a31-5394c423fb5f'

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
    minimumCount: 3
    maximumCount: 10
  }
]
