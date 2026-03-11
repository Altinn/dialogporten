# PostgreSQL Configuration

This document describes the current PostgreSQL configuration for Dialogporten across environments, derived from the Azure Bicep module and parameter files. The YAML snippets below use the DIS `Database` CRD directly and include notes for settings that are currently only available in Bicep.

## Test Environment

```yaml
apiVersion: storage.dis.altinn.cloud/v1alpha1
kind: Database
metadata:
  name: dialogporten-test
  namespace: dialogporten
spec:
  version: 16
  serverType: dev
  auth:
    # TODO: set to Entra principal object IDs
    adminAppIdentity: <entra-admin-object-id>
    userAppIdentity: <entra-user-object-id>
  storage:
    sizeGB: 32
    tier: P4
  # Configuration from bicep:
  # - Storage: Premium_LRS with P4 tier (32GB)
  # - Auto-grow: Enabled
  # - Index tuning: Disabled
  # - Query performance insight: Enabled
  # - Parameter logging: Disabled
  # - Backup retention: 7 days
  # - Geo-redundant backup: Disabled
  # - Backup vault storage for restores: Disabled
  # - Availability zone: 1
  # - Extensions: PG_TRGM, BTREE_GIN
  # - idle_in_transaction_session_timeout: 24 hours
```

## YT01 Environment (Development)

```yaml
apiVersion: storage.dis.altinn.cloud/v1alpha1
kind: Database
metadata:
  name: dialogporten-yt01
  namespace: dialogporten
spec:
  version: 16
  serverType: dev
  auth:
    # TODO: set to Entra principal object IDs
    adminAppIdentity: <entra-admin-object-id>
    userAppIdentity: <entra-user-object-id>
  storage:
    sizeGB: 4096
    tier: P50
  # Configuration from bicep:
  # - Storage: Premium_LRS with P50 tier (4096GB)
  # - Auto-grow: Enabled
  # - Index tuning: Enabled
  # - Query performance insight: Enabled
  # - Parameter logging: Enabled (log_min_duration_statement=5000ms)
  # - Backup retention: 7 days
  # - Geo-redundant backup: Disabled
  # - Backup vault storage for restores: Disabled
  # - Availability zone: 1
  # - Extensions: PG_TRGM, BTREE_GIN
  # - idle_in_transaction_session_timeout: 24 hours
```

## Staging Environment

```yaml
apiVersion: storage.dis.altinn.cloud/v1alpha1
kind: Database
metadata:
  name: dialogporten-staging
  namespace: dialogporten
spec:
  version: 16
  serverType: dev
  auth:
    # TODO: set to Entra principal object IDs
    adminAppIdentity: <entra-admin-object-id>
    userAppIdentity: <entra-user-object-id>
  storage:
    sizeGB: 256
    tier: P20
  # Configuration from bicep:
  # - Storage: Premium_LRS with P20 tier (256GB)
  # - Auto-grow: Enabled
  # - Index tuning: Enabled
  # - Query performance insight: Enabled
  # - Parameter logging: Disabled
  # - Backup retention: 7 days
  # - Geo-redundant backup: Disabled
  # - Backup vault storage for restores: Disabled
  # - Availability zone: 2
  # - High availability: Disabled (single zone)
  # - Extensions: PG_TRGM, BTREE_GIN
  # - idle_in_transaction_session_timeout: 24 hours
```

## Production Environment

```yaml
apiVersion: storage.dis.altinn.cloud/v1alpha1
kind: Database
metadata:
  name: dialogporten-prod
  namespace: dialogporten
spec:
  version: 16
  serverType: prod
  auth:
    # TODO: set to Entra principal object IDs
    adminAppIdentity: <entra-admin-object-id>
    userAppIdentity: <entra-user-object-id>
  storage:
    sizeGB: 4096
    tier: P50
  # Configuration from bicep:
  # - Storage: Premium_LRS with P50 tier (4096GB)
  # - Auto-grow: Enabled
  # - Index tuning: Enabled
  # - Query performance insight: Disabled
  # - Parameter logging: Disabled
  # - Backup retention: 32 days
  # - Geo-redundant backup: Disabled
  # - Backup vault storage for restores: Enabled
  # - High availability: Disabled (standby zone configured but not active)
  # - Availability zone: 3
  # - Extensions: PG_TRGM, BTREE_GIN
  # - idle_in_transaction_session_timeout: 24 hours
```

## Configuration Comparison

| Configuration | Test | YT01 | Staging | Production |
|--------------|------|------|---------|------------|
| **PostgreSQL Version** | 16 | 16 | 16 | 16 |
| **DIS serverType (assumed)** | dev | dev | dev | prod |
| **Azure SKU** | Standard_B2s | Standard_D16ads_v5 | Standard_D8ads_v5 | Standard_E16ads_v5 |
| **Tier** | Burstable | GeneralPurpose | GeneralPurpose | MemoryOptimized |
| **Storage Size** | 32 GB (P4) | 4096 GB (P50) | 256 GB (P20) | 4096 GB (P50) |
| **Storage Type** | Premium_LRS | Premium_LRS | Premium_LRS | Premium_LRS |
| **Auto-grow** | Enabled | Enabled | Enabled | Enabled |
| **Index Tuning** | Disabled | Enabled | Enabled | Enabled |
| **Query Performance** | Enabled | Enabled | Enabled | Disabled |
| **Parameter Logging** | Disabled | Enabled (5000ms) | Disabled | Disabled |
| **Backup Retention** | 7 days | 7 days | 7 days | 32 days |
| **Backup Vault Storage (restore)** | Disabled | Disabled | Disabled | Enabled |
| **High Availability** | Disabled | Disabled | Disabled | Disabled |
| **Primary Zone** | 1 | 1 | 2 | 3 |
| **Standby Zone** | - | - | - | - |
| **Extensions** | PG_TRGM, BTREE_GIN | PG_TRGM, BTREE_GIN | PG_TRGM, BTREE_GIN | PG_TRGM, BTREE_GIN |
| **Idle TX Timeout** | 24 hours | 24 hours | 24 hours | 24 hours |

## Database Configuration Details

### Database Settings
- **Database Name**: `dialogporten`
- **Character Set**: `UTF8`
- **Collation**: `en_US.utf8`
- **Administrator Login**: `dialogportenPgAdmin`
- **Entra ID Administrator**: `deployerPrincipalName` (service principal) is added as a PostgreSQL admin during provisioning

### PostgreSQL Extensions
- **PG_TRGM**: Enabled for trigram matching and similarity operations
- **BTREE_GIN**: Enabled for GIN index optimization

### Connection Settings
- **Port**: 5432
- **SSL Mode**: Required
- **Trust Server Certificate**: true

### Secrets and App Configuration
- **Admin password**: stored (upserted) in the source Key Vault as `dialogportenPgAdminPassword{environment}`.
- **Connection strings**: stored in the environment Key Vault as `dialogportenAdoConnectionString` and `dialogportenPsqlConnectionString`.
- **App Configuration**: `Infrastructure:DialogDbConnectionString` is set as a Key Vault reference to `dialogportenAdoConnectionString`.

### Performance Settings
- **idle_in_transaction_session_timeout**: 86400000ms (24 hours)
- **Query Store** is enabled when **Index Tuning** or **Query Performance Insight** is enabled:
  - **track_io_timing**: on
  - **pg_qs.query_capture_mode**: all
  - **pgms_wait_sampling.query_capture_mode**: all (only when Query Performance Insight is enabled)
  - **index_tuning.mode**: report (only when Index Tuning is enabled)
- **Parameter logging** (only when enabled):
  - **log_min_duration_statement**: environment-specific (YT01 uses 5000ms)
  - **log_statement**: none
  - **log_duration**: on

### Network Configuration
- Delegated subnet for the Flexible Server
- Private DNS zone integration (`${namePrefix}.postgres.database.azure.com`) with VNet link
- Deployed within Azure VNet

## Notes

1. **CPU and Memory Mapping**: The CPU and memory values in the Kubernetes YAML are approximate mappings from the Azure SKUs. Adjust based on actual NAIS/Kubernetes cluster capabilities and workload requirements.

2. **High Availability**: High availability is currently disabled in all environments. The production parameters include a standby zone value, but `highAvailability.mode` is set to `Disabled`.

3. **Monitoring**: Diagnostic logs are sent to Azure Log Analytics workspace only when Query Store is enabled (Index Tuning or Query Performance Insight), including:
   - PostgreSQL server logs
   - Session information
   - Query store runtime statistics
   - Query store wait statistics
   - Table statistics
   - Database transaction statistics

4. **Backup Vault Storage**: Production enables `enableBackupVault`, which provisions storage primitives for restores (not the Azure Backup vault itself).

5. **Source Files**: This configuration is derived from:
   - `.azure/modules/postgreSql/create.bicep`
   - `.azure/infrastructure/test.bicepparam`
   - `.azure/infrastructure/yt01.bicepparam`
   - `.azure/infrastructure/staging.bicepparam`
   - `.azure/infrastructure/prod.bicepparam`
6. **CRD Placeholders**: The DIS `Database` examples use placeholder Entra object IDs and a coarse `serverType`. These must be updated to real principal object IDs and an agreed profile mapping when moving to DIS.

## DIS Operator (dis-pgsql-operator) - Current Capabilities

The DIS PostgreSQL operator defines a `Database` CRD (`storage.dis.altinn.cloud/v1alpha1`) with the following scope:

- **Spec fields**: `version`, `serverType` (dev/prod), `auth` (`adminAppIdentity`, `userAppIdentity`), optional `storage` (`sizeGB`, `tier`).
- **serverType meaning**: A coarse sizing/profile selector used by the operator to pick a fixed SKU tier and size (today: `dev` -> `Standard_B1ms`, `prod` -> `Standard_D4s_v3`).
- **Creates**: Azure PostgreSQL Flexible Server, a per-database Private DNS zone (`<db>.private.postgres.database.azure.com`), and VNet links to both the DB VNet and AKS VNet.
- **Networking**: Allocates a subnet from the configured DB VNet and disables public network access.
- **Auth**: Enables Entra ID auth and disables password auth. `adminAppIdentity` must currently be an Entra principal object ID (not a display name) due to ASO limitations.
- **User identity**: `userAppIdentity` is accepted in the CRD but is not currently used to create roles or permissions.
- **Sizing**: `serverType` maps to fixed profiles (dev = `Standard_B1ms`, prod = `Standard_D4s_v3`). Storage defaults to 32GB/P10 with auto-grow enabled, unless overridden.
- **Location**: Hardcoded to `norwayeast`.
- **Not configured**: Database creation, extensions, Query Store/index tuning, parameter logging, backup retention, high availability, diagnostic settings, Key Vault secrets, or App Configuration wiring.

## Migration Considerations / Gaps vs. Current Bicep Setup

- **Sizing mismatch**: Current SKUs (`Standard_B2s`, `Standard_D8ads_v5`, `Standard_D16ads_v5`, `Standard_E16ads_v5`) are not supported by the operator's dev/prod profiles.
- **Authentication model**: Bicep uses a password-based admin login and writes connection strings to Key Vault/App Configuration; the operator disables password auth and relies on Entra ID principals.
- **Database creation & extensions**: Bicep creates the `dialogporten` database and enables `PG_TRGM`/`BTREE_GIN`; the operator does not.
- **Observability & tuning**: Query Store, index tuning, parameter logging, and diagnostics are Bicep-only today.
- **High availability & backups**: Bicep configures backup retention (and restore storage in prod); the operator does not support HA or backup configuration.
- **DNS & VNet layout**: The operator creates per-database private DNS zones and links them to both DB and AKS VNets, while Bicep uses an environment-scoped DNS zone. Aligning DNS names and VNet links will require design changes.
- **Subnet allocation**: The operator picks the first free subnet in the DB VNet; if that VNet contains non-DB subnets (as in the current Bicep VNet), it can select an incompatible subnet unless the VNet is DB-only or filtered.
- **Dependency wiring**: Current Bicep flow wires secrets and App Configuration values (connection strings); DIS would need equivalent wiring or new consumers for AAD-based connections.
