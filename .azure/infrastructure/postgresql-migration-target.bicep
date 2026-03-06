/*
  This template exists only for the parallel PostgreSQL target that is needed during a storage/major-version migration.

  Why it is separate from main.bicep:
  - main.bicep currently represents the single canonical server per environment
  - the PostgreSQL module can publish the fixed Dialogporten DB connection secrets
  - changing those secrets is the live cutover, not ordinary infrastructure provisioning

  This template therefore provisions only the new server and reuses the existing shared infrastructure in the resource group:
  - existing VNet / delegated PostgreSQL subnet
  - existing private DNS zone name
  - existing Log Analytics workspace
  - existing source Key Vault secret for the admin password

  It intentionally does NOT publish the canonical Key Vault secrets or App Configuration references.
  Those changes are performed later, during the controlled cutover window, by the operational runbook and scripts.

  Expected lifecycle:
  1. Deploy this file to create the transient PG18 / SSDv2 target.
  2. Run pgcopydb-based migration and cutover operationally.
  3. After the environment is stable on the new server, copy the "param postgresConfiguration" from the 
     postgresql-migration-target.{env}.bicepparam files into the {env}.bicepparam to make the new server the 
     canonical one for the environment, and remove postgresql-migration-target.{env}.bicepparam.
  4. Remove the old canonical server after the rollback window has passed.

  Keeping this template separate makes the transient dual-server state explicit and avoids accidental cutovers
  from a normal infrastructure deployment.

  When the migration is complete and the old server is decommissioned, this file can be removed.
*/

targetScope = 'resourceGroup'

import { baseTags } from '../functions/baseTags.bicep'
import { Sku as PostgresSku } from '../modules/postgreSql/create.bicep'
import { StorageConfiguration as PostgresStorageConfig } from '../modules/postgreSql/create.bicep'
import { HighAvailabilityConfiguration as PostgresHighAvailabilityConfig } from '../modules/postgreSql/create.bicep'

@description('The environment whose existing shared infrastructure this transient target will attach to.')
param environment string

@description('The location where the resources will be deployed. Defaults to the current resource group location.')
param location string = resourceGroup().location

@description('Password for PostgreSQL admin. Use the same admin password as the current canonical server.')
@secure()
@minLength(3)
param dialogportenPgAdminPassword string

@description('Subscription ID for the source Key Vault that stores the PostgreSQL admin password secret.')
@secure()
@minLength(3)
param sourceKeyVaultSubscriptionId string

@description('Resource group for the source Key Vault that stores the PostgreSQL admin password secret.')
@secure()
@minLength(3)
param sourceKeyVaultResourceGroup string

@description('Name of the source Key Vault that stores the PostgreSQL admin password secret.')
@secure()
@minLength(3)
param sourceKeyVaultName string

@description('The name of the deployer principal used as the PostgreSQL administrator.')
param deployerPrincipalName string

@description('Configuration for the transient migration target PostgreSQL server.')
param postgresConfiguration {
  serverNameStem: string
  version: '16' | '17' | '18'
  sku: PostgresSku
  storage: PostgresStorageConfig
  enableIndexTuning: bool
  enableQueryPerformanceInsight: bool
  highAvailability: PostgresHighAvailabilityConfig?
  backupRetentionDays: int
  availabilityZone: string
  enableBackupVault: bool
}

var namePrefix = 'dp-be-${environment}'
var tags = baseTags({}, environment)
var srcKeyVault = {
  name: sourceKeyVaultName
  subscriptionId: sourceKeyVaultSubscriptionId
  resourceGroupName: sourceKeyVaultResourceGroup
}
var virtualNetworkName = '${namePrefix}-vnet'
var virtualNetworkId = resourceId('Microsoft.Network/virtualNetworks', virtualNetworkName)
var postgresqlSubnetId = resourceId('Microsoft.Network/virtualNetworks/subnets', virtualNetworkName, 'postgresqlSubnet')
var appInsightWorkspaceName = '${namePrefix}-insightsWorkspace'

module postgresql '../modules/postgreSql/create.bicep' = {
  name: 'postgresqlMigrationTarget'
  params: {
    namePrefix: namePrefix
    location: location
    serverNameStem: postgresConfiguration.serverNameStem
    postgresVersion: postgresConfiguration.version
    publishCanonicalConnectionSecrets: false
    srcKeyVault: srcKeyVault
    srcKeyVaultAdministratorLoginPasswordKey: 'dialogportenPgAdminPassword${environment}'
    administratorLoginPassword: dialogportenPgAdminPassword
    sku: postgresConfiguration.sku
    storage: postgresConfiguration.storage
    appInsightWorkspaceName: appInsightWorkspaceName
    enableIndexTuning: postgresConfiguration.enableIndexTuning
    enableQueryPerformanceInsight: postgresConfiguration.enableQueryPerformanceInsight
    subnetId: postgresqlSubnetId
    vnetId: virtualNetworkId
    highAvailability: postgresConfiguration.?highAvailability
    backupRetentionDays: postgresConfiguration.backupRetentionDays
    availabilityZone: postgresConfiguration.availabilityZone
    enableBackupVault: postgresConfiguration.enableBackupVault
    deployerPrincipalName: deployerPrincipalName
    tags: tags
  }
}

output serverName string = postgresql.outputs.serverName
output fullyQualifiedDomainName string = postgresql.outputs.fullyQualifiedDomainName
