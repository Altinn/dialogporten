using './main.bicep'

param environment = 'staging'
param location = 'norwayeast'
param keyVaultSourceKeys = json(readEnvironmentVariable('AZURE_KEY_VAULT_SOURCE_KEYS'))

param redisVersion = '6.0'

param containerAppEnvZoneRedundancyEnabled = true

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
    name: 'Standard_D8ads_v5'
    tier: 'GeneralPurpose'
  }
  storage: {
    storageSizeGB: 256
    autoGrow: 'Enabled'
    type: 'Premium_LRS'
    tier: 'P20'
  }
  // Enabling index tuning will practically also enable query performance insight
  enableIndexTuning: true
  enableQueryPerformanceInsight: true
  parameterLogging: {
    enabled: true
    logMinDurationStatementMs: 5000
  }
  backupRetentionDays: 7
  availabilityZone: '2'
}

param deployerPrincipalName = 'GitHub: altinn/dialogporten - Prod'

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
// Altinn Product Dialogporten: Developers Prod
param sshJumperAdminLoginGroupObjectId = 'a94de4bf-0a83-4d30-baba-0c6a7365571c'

param apimUrl = 'https://platform.tt02.altinn.no/dialogporten'
