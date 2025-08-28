# PostgreSQL Backup Strategy

This document outlines the comprehensive backup strategy for Dialogporten's PostgreSQL databases across all environments.

## Overview

Dialogporten uses a **two-tier backup strategy** to provide both operational recovery and long-term data retention:

- **Short-Term Backups**: Azure PostgreSQL Flexible Server built-in backups for operational recovery
- **Long-Term Backups**: Azure Backup service for compliance and long-term retention (production only)

## Backup Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────┐
│   PostgreSQL    │    │   Short-Term     │    │    Long-Term        │
│   Flexible      │───▶│   Backups        │    │    Backups          │
│   Server        │    │   (Built-in)     │    │   (Azure Backup)    │
└─────────────────┘    └──────────────────┘    └─────────────────────┘
                              │                          │
                              ▼                          ▼
                       ┌──────────────┐         ┌──────────────┐
                       │ 7-35 days    │         │ 12 months    │
                       │ Point-in-time│         │ Weekly       │
                       │ recovery     │         │ (Production) │
                       └──────────────┘         └──────────────┘
```

## Short-Term Backups (Built-in PostgreSQL)

### Description
Azure PostgreSQL Flexible Server automatically creates backups using the built-in backup functionality. These backups support point-in-time recovery and are ideal for operational scenarios.

### Configuration by Environment

| Environment | Retention Period | Use Case |
|-------------|------------------|----------|
| **Production** | 32 days | Operational recovery, rollback recent changes |
| **Staging** | 7 days | Testing, development support |
| **Test** | 7 days | Testing scenarios |
| **YT01** | 7 days | Development and testing |

### Features
- **Point-in-time recovery**: Restore to any specific moment within the retention period
- **Automatic scheduling**: Continuous backup without manual intervention
- **Geo-redundancy**: Disabled for cost optimization (locally redundant)
- **Fast recovery**: Direct restoration to a new PostgreSQL server

### Restoring from Short-Term Backups

Before proceeding with any restore operation, back up your current database:

```bash
PGPASSWORD='supersecret' pg_dump -h localhost -p 5432 -U postgres -d dialogporten -F c -f local-db-backup.dump
```

#### Setting up Database Connection

If you need to restore from an Azure environment (e.g., tt02), first set up the database forwarder.  
See [database forwarder docs](../scripts/database-forwarder/README.md) for setup instructions.

Set the database password for the current terminal session:
```bash
export PGPASSWORD="get-password-from-key-vault"
```

#### Creating a Backup from Azure

Run the backup from the remote database:
```bash
pg_dump -e pg_trgm -h localhost -p 15432 -U dialogportenPgAdmin -d dialogporten -F c -f azure-db-backup.dump
```

Reset the password back to local default:
```bash
export PGPASSWORD="supersecret"
```

#### Creating Required Users

Azure databases may have different users than your local setup. Create the required users:

```bash
psql -h localhost -U postgres -c 'CREATE ROLE "dialogportenPgAdmin" WITH SUPERUSER LOGIN PASSWORD '\''supersecret'\'';'
psql -h localhost -U postgres -c "CREATE ROLE azure_pg_admin WITH SUPERUSER LOGIN PASSWORD 'fakepassword';"
```

#### Restoring the Database

To restore from the Azure backup:
```bash
pg_restore -h localhost -U postgres -d dialogporten --clean azure-db-backup.dump
```

To restore from a local backup:
```bash
pg_restore -h localhost -U postgres -d dialogporten --clean local-db-backup.dump
```

## Long-Term Backups (Azure Backup Service)

### Description
Production environment uses Azure Backup service to create long-term backups for compliance and extended data retention. This is implemented using Azure Data Protection backup vaults.

### Configuration

**Environment**: Production only

| Setting | Value |
|---------|-------|
| **Retention** | 12 months |
| **Schedule** | Weekly, Sundays 2:00 AM CET |
| **Storage** | Locally Redundant |
| **Backup Type** | Full backups |
| **Soft Delete** | 14 days |

### Infrastructure Implementation

The long-term backup is configured in the PostgreSQL Bicep module:

```bicep
// In prod.bicepparam
postgresConfiguration = {
  // ... other settings ...
  longTermBackup: {
    retentionDurationMonths: 12
  }
}
```

When deployed, this creates:
- Azure Backup Vault (`dp-be-prod-backup-vault-{unique}`)
- Backup Policy with weekly schedule
- Backup Instance linking PostgreSQL server to the policy
- Required RBAC permissions (PostgreSQL Backup and Export Operator)

### Monitoring Long-Term Backups

**Azure Portal Navigation**:
1. Go to **Backup Center** in Azure Portal
2. Select **Backup Instances**
3. Filter by **PostgreSQL** datasource type
4. Find your production server: `dp-be-prod-postgres-{unique}`

**Backup Status**:
- View backup job history
- Monitor backup success/failure
- Check next scheduled backup time
- Review retention policy compliance

### Recovery from Long-Term Backups

> ⚠️ **Note**: Detailed restore procedures for long-term backups will be documented in future updates.

## Environment-Specific Backup Summary

| Environment | Short-Term Retention | Long-Term Backup |
|-------------|---------------------|------------------|
| **Production** | 32 days | ✅ 12 months |
| **Staging** | 7 days | ❌ None |
| **Test** | 7 days | ❌ None |
| **YT01** | 7 days | ❌ None |

## Disaster Recovery Planning

> 📋 **Future Documentation**
> 
> The following sections will be expanded in future updates:
> 

## Related Documentation

- [Infrastructure.md](./Infrastructure.md) - Overall infrastructure documentation
- [Database Forwarder](../scripts/database-forwarder/README.md) - Connection setup for remote database access

## Configuration Changes

To modify backup retention or schedules, update the respective environment parameter files:

- **Short-term retention**: Modify `backupRetentionDays` in `postgresConfiguration`
- **Long-term retention**: Modify `retentionDurationMonths` in `longTermBackup` configuration

Changes require infrastructure deployment to take effect.
