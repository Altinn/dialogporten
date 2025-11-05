# PostgreSQL Configuration

This document describes the PostgreSQL configuration for Dialogporten across different environments, expressed as NAIS Kubernetes YAML templates.

## Test Environment

```yaml
apiVersion: dis.io/v1alpha1
kind: Application
metadata:
  name: dialogporten-test
  namespace: dialogporten
spec:
  postgres:
    cluster:
      majorVersion: "16"
      resources:
        # Azure: Standard_B2s (2 vCPU, 4GB RAM) - Burstable tier
        cpu: 1000m  # Kubernetes equivalent for minimal performance
        diskSize: 32Gi  # 32GB storage (P4 tier)
        memory: 2Gi
      # Configuration from bicep:
      # - Storage: Premium_LRS with P4 tier (32GB)
      # - Auto-grow: Enabled
      # - Index tuning: Disabled
      # - Query performance insight: Enabled
      # - Backup retention: 7 days
      # - Availability zone: 1
      # - Extensions: PG_TRGM
      # - idle_in_transaction_session_timeout: 24 hours
```

## YT01 Environment (Development)

```yaml
apiVersion: dis.io/v1alpha1
kind: Application
metadata:
  name: dialogporten-yt01
  namespace: dialogporten
spec:
  postgres:
    cluster:
      majorVersion: "16"
      resources:
        # Azure: Standard_D16ads_v5 (16 vCPU, 64GB RAM)
        cpu: 8000m  # Kubernetes equivalent for good performance
        diskSize: 4096Gi  # 4TB storage (P50 tier)
        memory: 32Gi
      # Configuration from bicep:
      # - Storage: Premium_LRS with P50 tier (4096GB)
      # - Auto-grow: Enabled
      # - Index tuning: Enabled
      # - Query performance insight: Enabled
      # - Backup retention: 7 days
      # - Availability zone: 1
      # - Extensions: PG_TRGM
      # - idle_in_transaction_session_timeout: 24 hours
```

## Staging Environment

```yaml
apiVersion: dis.io/v1alpha1
kind: Application
metadata:
  name: dialogporten-staging
  namespace: dialogporten
spec:
  postgres:
    cluster:
      majorVersion: "16"
      resources:
        # Azure: Standard_D8ads_v5 (8 vCPU, 32GB RAM)
        cpu: 4000m  # Kubernetes equivalent for balanced performance
        diskSize: 256Gi  # 256GB storage (P20 tier)
        memory: 16Gi
      # Configuration from bicep:
      # - Storage: Premium_LRS with P20 tier (256GB)
      # - Auto-grow: Enabled
      # - Index tuning: Enabled
      # - Query performance insight: Enabled
      # - Backup retention: 7 days
      # - Availability zone: 2
      # - High availability: None (single zone)
      # - Extensions: PG_TRGM
      # - idle_in_transaction_session_timeout: 24 hours
```

## Production Environment

```yaml
apiVersion: dis.io/v1alpha1
kind: Application
metadata:
  name: dialogporten-prod
  namespace: dialogporten
spec:
  postgres:
    cluster:
      majorVersion: "16"
      resources:
        # Azure: Standard_D16ads_v5 (16 vCPU, 64GB RAM)
        cpu: 8000m  # Kubernetes equivalent for high performance
        diskSize: 4096Gi  # 4TB storage (P50 tier)
        memory: 32Gi
      # Configuration from bicep:
      # - Storage: Premium_LRS with P50 tier (4096GB)
      # - Auto-grow: Enabled
      # - Index tuning: Enabled
      # - Query performance insight: Disabled
      # - Backup retention: 32 days
      # - High availability: Zone redundant (zone 3 + standby in zone 2)
      # - Availability zones: 3 (primary), 2 (standby)
      # - Extensions: PG_TRGM
      # - idle_in_transaction_session_timeout: 24 hours
```

## Configuration Comparison

| Configuration | Test | YT01 | Staging | Production |
|--------------|------|------|---------|------------|
| **PostgreSQL Version** | 16 | 16 | 16 | 16 |
| **Azure SKU** | Standard_B2s | Standard_D16ads_v5 | Standard_D8ads_v5 | Standard_D16ads_v5 |
| **Tier** | Burstable | GeneralPurpose | GeneralPurpose | GeneralPurpose |
| **Storage Size** | 32 GB (P4) | 4096 GB (P50) | 256 GB (P20) | 4096 GB (P50) |
| **Storage Type** | Premium_LRS | Premium_LRS | Premium_LRS | Premium_LRS |
| **Auto-grow** | Enabled | Enabled | Enabled | Enabled |
| **Index Tuning** | Disabled | Enabled | Enabled | Enabled |
| **Query Performance** | Enabled | Enabled | Enabled | Disabled |
| **Backup Retention** | 7 days | 7 days | 7 days | 32 days |
| **High Availability** | None | None | None | Zone Redundant |
| **Primary Zone** | 1 | 1 | 2 | 3 |
| **Standby Zone** | - | - | - | 2 |
| **Extensions** | PG_TRGM | PG_TRGM | PG_TRGM | PG_TRGM |
| **Idle TX Timeout** | 24 hours | 24 hours | 24 hours | 24 hours |

## Database Configuration Details

### Database Settings
- **Database Name**: `dialogporten`
- **Character Set**: `UTF8`
- **Collation**: `en_US.utf8`
- **Administrator Login**: `dialogportenPgAdmin`

### PostgreSQL Extensions
- **PG_TRGM**: Enabled for trigram matching and similarity operations

### Connection Settings
- **Port**: 5432
- **SSL Mode**: Required
- **Trust Server Certificate**: true

### Performance Settings
- **idle_in_transaction_session_timeout**: 86400000ms (24 hours)
- **track_io_timing**: on (when Query Performance Insight is enabled)
- **pg_qs.query_capture_mode**: all (when Query Performance Insight is enabled)
- **pgms_wait_sampling.query_capture_mode**: all (when Query Performance Insight is enabled)
- **index_tuning.mode**: report (when Index Tuning is enabled)

### Network Configuration
- Private endpoint with delegated subnet
- Private DNS zone integration
- Deployed within Azure VNet

## Notes

1. **CPU and Memory Mapping**: The CPU and memory values in the Kubernetes YAML are approximate mappings from the Azure SKUs. Adjust based on actual NAIS/Kubernetes cluster capabilities and workload requirements.

2. **High Availability**: Only the production environment uses zone-redundant high availability with a standby replica in a different availability zone.

3. **Monitoring**: Diagnostic logs are sent to Azure Log Analytics workspace, including:
   - PostgreSQL server logs
   - Session information
   - Query store runtime statistics
   - Query store wait statistics
   - Table statistics
   - Database transaction statistics

4. **Source Files**: This configuration is derived from:
   - `.azure/modules/postgreSql/create.bicep`
   - `.azure/infrastructure/test.bicepparam`
   - `.azure/infrastructure/yt01.bicepparam`
   - `.azure/infrastructure/staging.bicepparam`
   - `.azure/infrastructure/prod.bicepparam`

