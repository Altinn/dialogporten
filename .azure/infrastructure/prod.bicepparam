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
  serverNameStem: 'postgres2'
  version: '18'
  sku: {
    name: 'Standard_E48ads_v5'
    tier: 'MemoryOptimized'
  }
  storage: {
    storageSizeGB: 10000
    type: 'PremiumV2_LRS'
    iops: 24000
    throughput: 1200
  }
  enableIndexTuning: false
  enableQueryPerformanceInsight: false
  enableTrackIoTiming: true
  additionalServerConfigurations: [
    {
      name: 'autovacuum_max_workers'
      value: '12'
    }
    {
      name: 'autovacuum_naptime'
      value: '15'
    }
    {
      name: 'autovacuum_vacuum_cost_delay'
      value: '1'
    }
    {
      name: 'autovacuum_vacuum_cost_limit'
      value: '10000'
    }
    {
      name: 'effective_io_concurrency'
      value: '24'
    }
    {
      name: 'maintenance_work_mem'
      value: '2097151'
    }
    {
      name: 'track_cost_delay_timing'
      value: 'on'
    }
    {
      name: 'vacuum_buffer_usage_limit'
      value: '8192'
    }
    {
      name: 'vacuum_cost_limit'
      value: '10000'
    }
  ]
  applyStaticServerConfigurations: false
  staticServerConfigurations: [
    {
      name: 'commit_timestamp_buffers'
      value: '1024'
    }
    {
      name: 'io_max_concurrency'
      value: '64'
    }
    {
      name: 'subtransaction_buffers'
      value: '1024'
    }
    {
      name: 'transaction_buffers'
      value: '1024'
    }
  ]
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

param serviceBusVnetEnabled = true

// Altinn Product Dialogporten: Developers Prod
param sshJumperConfig = {
  adminLoginGroupObjectId: 'a94de4bf-0a83-4d30-baba-0c6a7365571c'
  vmSize: 'Standard_D2as_v5'
}

param apimUrl = 'https://platform.altinn.no/dialogporten'
