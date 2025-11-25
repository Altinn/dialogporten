import { uniqueResourceName } from '../../functions/resourceName.bicep'

@description('The prefix used for naming resources to ensure unique names')
param namePrefix string

@description('The location where the resources will be deployed')
param location string

@description('The name of the environment Key Vault')
param environmentKeyVaultName string

@description('The name of the secret name(key) in the source key vault to store the PostgreSQL administrator login password')
#disable-next-line secure-secrets-in-params
param srcKeyVaultAdministratorLoginPasswordKey string

@description('The ID of the subnet where the PostgreSQL server will be deployed')
param subnetId string

@description('The ID of the virtual network for the private DNS zone')
param vnetId string

@description('Tags to apply to resources')
param tags object

@export()
type Sku = {
  name: 'Standard_B1ms' | 'Standard_B2s' | 'Standard_B4ms' | 'Standard_B8ms' | 'Standard_B12ms' | 'Standard_B16ms' | 'Standard_B20ms' | 'Standard_D4ads_v5' | 'Standard_D8ads_v5' | 'Standard_D16ads_v5' | 'Standard_D32ads_v5' | 'Standard_D48ads_v5' | 'Standard_D64ads_v5'
  tier: 'Burstable' | 'GeneralPurpose' | 'MemoryOptimized'
}

@description('The SKU of the PostgreSQL server')
param sku Sku

@export()
type StorageConfiguration = {
  @minValue(32)
  storageSizeGB: int
  autoGrow: 'Enabled' | 'Disabled'
  @description('The type of storage account to use. Default is Premium_LRS.')
  type: 'Premium_LRS' | 'PremiumV2_LRS'
  @description('The performance tier of the storage.')
  tier: 'P1' | 'P2' | 'P4' | 'P6' | 'P10' | 'P15' | 'P20' | 'P30' | 'P40' | 'P50' | 'P60' | 'P70' | 'P80'
}

@description('The storage configuration for the PostgreSQL server')
param storage StorageConfiguration

@description('Enable query performance insight')
param enableQueryPerformanceInsight bool

@description('Enable index tuning')
param enableIndexTuning bool

@description('The name of the Application Insights workspace')
param appInsightWorkspaceName string

@export()
type HighAvailabilityConfiguration = {
  mode: 'ZoneRedundant' | 'SameZone' | 'Disabled'
  standbyAvailabilityZone: string
}

@description('High availability configuration for the PostgreSQL server')
param highAvailability HighAvailabilityConfiguration?

@description('The availability zone for the PostgreSQL primary server')
param availabilityZone string

@description('The number of days to retain backups.')
@minValue(7)
@maxValue(35)
param backupRetentionDays int

@description('The Key Vault to store the PostgreSQL administrator login password')
@secure()
param srcKeyVault object

@description('The password for the PostgreSQL administrator login')
@secure()
param administratorLoginPassword string

@description('The name of the deployer principal used as the PostgreSQL administrator')
@minLength(3)
param deployerPrincipalName string

var administratorLogin = 'dialogportenPgAdmin'
var databaseName = 'dialogporten'
var postgresServerNameMaxLength = 63
var postgresServerName = uniqueResourceName('${namePrefix}-postgres', postgresServerNameMaxLength)

module saveAdmPassword '../keyvault/upsertSecret.bicep' = {
  name: 'Save_${srcKeyVaultAdministratorLoginPasswordKey}'
  scope: resourceGroup(srcKeyVault.subscriptionId, srcKeyVault.resourceGroupName)
  params: {
    destKeyVaultName: srcKeyVault.name
    secretName: srcKeyVaultAdministratorLoginPasswordKey
    secretValue: administratorLoginPassword
    tags: tags
  }
}

module privateDnsZone '../privateDnsZone/main.bicep' = {
  name: 'postgresqlPrivateDnsZone'
  params: {
    namePrefix: namePrefix
    defaultDomain: '${namePrefix}.postgres.database.azure.com'
    vnetId: vnetId
    tags: tags
  }
}

resource postgresAdminIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: '${namePrefix}-postgres-admin-identity'
  location: location
  tags: tags
}

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: postgresServerName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${postgresAdminIdentity.id}': {}
    }
  }
  properties: {
    version: '16'
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    storage: {
      storageSizeGB: storage.storageSizeGB
      autoGrow: storage.autoGrow
      type: storage.type
      tier: storage.tier
    }
    backup: {
      backupRetentionDays: backupRetentionDays
      geoRedundantBackup: 'Disabled'
    }
    dataEncryption: {
      type: 'SystemManaged'
    }
    replicationRole: 'Primary'
    network: {
      delegatedSubnetResourceId: subnetId
      privateDnsZoneArmResourceId: privateDnsZone.outputs.id
    }
    availabilityZone: availabilityZone
    highAvailability: highAvailability
  }
  sku: sku
  resource database 'databases' = {
    name: databaseName
    properties: {
      charset: 'UTF8'
      collation: 'en_US.utf8'
    }
  }

  tags: tags
}

resource postgresAdministrators 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2024-08-01' = {
  name: deployer().objectId
  parent: postgres
  properties: {
    principalName: deployerPrincipalName
    principalType: 'ServicePrincipal'
    tenantId: deployer().tenantId
  }
}

resource enable_extensions 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2024-08-01' = {
    parent: postgres
    name: 'azure.extensions'
    properties: {
      value: 'PG_TRGM,BTREE_GIN'
      source: 'user-override'
    }
    dependsOn: [postgresAdministrators]
  }

resource idle_transactions_timeout 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2024-08-01' = {
  parent: postgres
  name: 'idle_in_transaction_session_timeout'
  properties: {
    value: '86400000' // 24 hours
    source: 'user-override'
  }
  dependsOn: [enable_extensions]
}

// Enable Query Store when either index tuning or query performance insight is enabled
var enableQueryStore = enableIndexTuning || enableQueryPerformanceInsight

resource track_io_timing 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2024-08-01' = if (enableQueryStore) {
  parent: postgres
  name: 'track_io_timing'
  properties: {
    value: 'on'
    source: 'user-override'
  }
  dependsOn: [idle_transactions_timeout]
}

resource pg_qs_query_capture_mode 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2024-08-01' = if (enableQueryStore) {
  parent: postgres
  name: 'pg_qs.query_capture_mode'
  properties: {
    value: 'all'
    source: 'user-override'
  }
  dependsOn: [track_io_timing]
}

resource pgms_wait_sampling_query_capture_mode 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2024-08-01' = if (enableQueryPerformanceInsight) {
  parent: postgres
  name: 'pgms_wait_sampling.query_capture_mode'
  properties: {
    value: 'all'
    source: 'user-override'
  }
  dependsOn: [pg_qs_query_capture_mode]
}

resource index_tuning_mode 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2024-08-01' = if (enableIndexTuning) {
  parent: postgres
  name: 'index_tuning.mode'
  properties: {
    value: 'report'
    source: 'user-override'
  }
  dependsOn: [pg_qs_query_capture_mode]
}

resource appInsightsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: appInsightWorkspaceName
}

// todo: setting as 0 for now. Will use the log analytics workspace policy instead. Consider setting explicitly in the future.
var diagnosticSettingRetentionPolicy = {
  days: 0
  enabled: false
}

var diagnosticLogCategories = [
  'PostgreSQLLogs'
  'PostgreSQLFlexSessions'
  'PostgreSQLFlexQueryStoreRuntime'
  'PostgreSQLFlexQueryStoreWaitStats'
  'PostgreSQLFlexTableStats'
  'PostgreSQLFlexDatabaseXacts'
]

resource diagnosticSetting 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (enableQueryStore) {
  name: 'PostgreSQLDiagnosticSetting'
  scope: postgres
  properties: {
    workspaceId: appInsightsWorkspace.id
    logs: [for category in diagnosticLogCategories: {
      category: category
      enabled: true
      retentionPolicy: diagnosticSettingRetentionPolicy
    }]
    metrics: [
      {
        timeGrain: null
        enabled: true
        retentionPolicy: diagnosticSettingRetentionPolicy
        category: 'AllMetrics'
      }
    ]
  }
  dependsOn: [pg_qs_query_capture_mode]
}

module adoConnectionString '../keyvault/upsertSecret.bicep' = {
  name: 'adoConnectionString'
  params: {
    destKeyVaultName: environmentKeyVaultName
    secretName: 'dialogportenAdoConnectionString'
    secretValue: 'Server=${postgres.properties.fullyQualifiedDomainName};Database=${databaseName};Port=5432;User Id=${administratorLogin};Password=${administratorLoginPassword};Ssl Mode=Require;Trust Server Certificate=true;Include Error Detail=True;'
    tags: tags
  }
}

module psqlConnectionString '../keyvault/upsertSecret.bicep' = {
  name: 'psqlConnectionString'
  params: {
    destKeyVaultName: environmentKeyVaultName
    secretName: 'dialogportenPsqlConnectionString'
    secretValue: 'psql \'host=${postgres.properties.fullyQualifiedDomainName} port=5432 dbname=${databaseName} user=${administratorLogin} password=${administratorLoginPassword} sslmode=require\''
    tags: tags
  }
}

output adoConnectionStringSecretUri string = adoConnectionString.outputs.secretUri
output psqlConnectionStringSecretUri string = psqlConnectionString.outputs.secretUri
