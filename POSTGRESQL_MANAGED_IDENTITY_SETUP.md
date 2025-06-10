# PostgreSQL Managed Identity Authentication Setup

This document provides a comprehensive guide for implementing managed identity authentication with Azure PostgreSQL Flexible Server for the Dialogporten project.

## Overview

This implementation allows your container apps to authenticate to PostgreSQL using Azure managed identities instead of username/password authentication. The solution uses:

- **Azure AD Groups**: A single AD group contains all managed identities that need database access
- **PostgreSQL Database Roles**: A database role mapped to the AD group with appropriate permissions
- **Bicep Deployment Scripts**: Automated setup of database roles and permissions
- **Azure AD Administrator**: Enables team members to connect using Azure AD authentication

## Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Container     │    │   Azure AD      │    │   PostgreSQL    │
│   Apps          │    │   Group         │    │   Flexible      │
│                 │    │                 │    │   Server        │
│ ┌─────────────┐ │    │ ┌─────────────┐ │    │ ┌─────────────┐ │
│ │ Managed     │─│────┼─│ Group       │─│────┼─│ Database    │ │
│ │ Identity    │ │    │ │ Members     │ │    │ │ Role        │ │
│ └─────────────┘ │    │ └─────────────┘ │    │ └─────────────┘ │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## Implementation Components

### 1. Infrastructure Changes
- **PostgreSQL Server**: Configured with Azure AD authentication enabled
- **Azure AD Administrator**: Allows team members to connect and manage the database
- **Deployment Scripts**: Automate database role creation and permission assignment

### 2. Application Changes
- **Managed Identity Group Membership**: Each app's managed identity is added to the PostgreSQL AD group
- **Connection String Support**: New managed identity connection string available in App Configuration

### 3. Database Setup
- **Database Role**: Single role mapped to the AD group with db_owner permissions
- **Automatic Permissions**: Covers all tables, sequences, functions, and future objects

## Prerequisites

Before deploying this implementation, you need to manually create:

1. **Azure AD Group for PostgreSQL Users**
   ```bash
   # Create the AD group
   az ad group create \
     --display-name "dialogporten-postgres-users-{environment}" \
     --mail-nickname "dialogporten-postgres-users-{environment}" \
     --description "PostgreSQL users for Dialogporten {environment}"
   
   # Get the Object ID (save this for parameters)
   az ad group show \
     --group "dialogporten-postgres-users-{environment}" \
     --query objectId -o tsv
   ```

2. **Azure AD Administrator Group/User**
   ```bash
   # Option 1: Use an existing admin group
   az ad group show --group "your-admin-group" --query objectId -o tsv
   
   # Option 2: Use a specific user
   az ad user show --id "admin@yourdomain.com" --query objectId -o tsv
   ```

## Deployment Parameters

Add these parameters to your infrastructure deployment:

```bicep
// Required: PostgreSQL AD Group (manually created)
param postgresqlAzureAdGroupObjectId string = "12345678-1234-1234-1234-123456789abc"
param postgresqlAzureAdGroupName string = "dialogporten-postgres-users-prod"

// Required: PostgreSQL Administrator 
param postgresqlAzureAdAdministratorObjectId string = "87654321-4321-4321-4321-cba987654321"
param postgresqlAzureAdAdministratorName string = "admin@yourdomain.com"
```

## Application Configuration

For each application, add the PostgreSQL AD group parameter:

```bicep
// In application .bicepparam files
param postgresqlAzureAdGroupObjectId = '12345678-1234-1234-1234-123456789abc'
```

## Step-by-Step Deployment

### Phase 1: Manual Prerequisites

1. **Create PostgreSQL AD Group**
   ```bash
   az ad group create \
     --display-name "dialogporten-postgres-users-prod" \
     --mail-nickname "dialogporten-postgres-users-prod"
   
   # Save the Object ID
   GROUP_ID=$(az ad group show --group "dialogporten-postgres-users-prod" --query objectId -o tsv)
   echo "PostgreSQL Group Object ID: $GROUP_ID"
   ```

2. **Identify Administrator**
   ```bash
   # For admin group
   ADMIN_ID=$(az ad group show --group "your-admin-group" --query objectId -o tsv)
   ADMIN_NAME="your-admin-group"
   
   # For admin user
   ADMIN_ID=$(az ad user show --id "admin@yourdomain.com" --query objectId -o tsv)
   ADMIN_NAME="admin@yourdomain.com"
   
   echo "Administrator Object ID: $ADMIN_ID"
   echo "Administrator Name: $ADMIN_NAME"
   ```

### Phase 2: Infrastructure Deployment

1. **Update Infrastructure Parameters**
   Add the new parameters to your infrastructure parameter files:
   
   ```bicep
   // .azure/infrastructure/{environment}.bicepparam
   param postgresqlAzureAdGroupObjectId = '{GROUP_ID_FROM_STEP_1}'
   param postgresqlAzureAdGroupName = 'dialogporten-postgres-users-{environment}'
   param postgresqlAzureAdAdministratorObjectId = '{ADMIN_ID_FROM_STEP_2}'
   param postgresqlAzureAdAdministratorName = '{ADMIN_NAME_FROM_STEP_2}'
   ```

2. **Deploy Infrastructure**
   ```bash
   # Deploy infrastructure with new PostgreSQL configuration
   az deployment sub create \
     --location norwayeast \
     --template-file .azure/infrastructure/main.bicep \
     --parameters @.azure/infrastructure/{environment}.bicepparam
   ```

### Phase 3: Application Deployment

1. **Update Application Parameters**
   Add PostgreSQL group configuration to each application's parameter file:
   
   ```bicep
   // .azure/applications/{app-name}/{environment}.bicepparam
   param postgresqlAzureAdGroupObjectId = '{GROUP_ID_FROM_STEP_1}'
   ```

2. **Deploy Applications**
   Deploy each application to add managed identities to the PostgreSQL group:
   
   ```bash
   # Deploy each application
   az deployment group create \
     --resource-group dp-be-{environment}-rg \
     --template-file .azure/applications/{app-name}/main.bicep \
     --parameters @.azure/applications/{app-name}/{environment}.bicepparam
   ```

## Verification

### 1. Verify PostgreSQL Configuration
```bash
# Check Azure AD authentication is enabled
az postgres flexible-server show \
  --name dp-be-{environment}-postgres \
  --resource-group dp-be-{environment}-rg \
  --query "authConfig"
```

### 2. Verify AD Group Membership
```bash
# List group members
az ad group member list \
  --group "dialogporten-postgres-users-{environment}" \
  --query "[].displayName" -o table
```

### 3. Test Database Connection

As an Azure AD administrator:
```bash
# Get access token
ACCESS_TOKEN=$(az account get-access-token --resource https://ossrdbms-aad.database.windows.net --query accessToken -o tsv)

# Connect to PostgreSQL
psql "host=dp-be-{environment}-postgres.postgres.database.azure.com port=5432 dbname=dialogporten user=admin@yourdomain.com sslmode=require" --set PGPASSWORD=$ACCESS_TOKEN

# Check roles
\du

# Check permissions
\l
```

## Connection Strings

### Managed Identity Connection String
Available in App Configuration as `Infrastructure:DialogDbManagedIdentityConnectionString`:
```
Server=dp-be-{environment}-postgres.postgres.database.azure.com;Database=dialogporten;Port=5432;Ssl Mode=Require;Trust Server Certificate=true;
```

### Traditional Connection String (still available)
Available in App Configuration as `Infrastructure:DialogDbConnectionString`:
```
Server=dp-be-{environment}-postgres.postgres.database.azure.com;Database=dialogporten;Port=5432;User Id=dialogportenPgAdmin;Password={password};Ssl Mode=Require;Trust Server Certificate=true;
```

## Application Code Changes

To use managed identity authentication in your applications:

```csharp
// Example for .NET applications
services.AddNpgsql<DialogDbContext>(connectionString, options =>
{
    // Use managed identity token for authentication
    options.UsePeriodicPasswordProvider(async (_, ct) =>
    {
        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(
            new TokenRequestContext(new[] { "https://ossrdbms-aad.database.windows.net/.default" }), 
            ct);
        return token.Token;
    }, TimeSpan.FromHours(1), TimeSpan.FromMinutes(55));
});
```

## Local Development

For local development, you have several options:

### Option 1: Add Your User to the AD Group
```bash
# Add your user to the PostgreSQL AD group
az ad group member add \
  --group "dialogporten-postgres-users-{environment}" \
  --member-id $(az ad signed-in-user show --query objectId -o tsv)
```

### Option 2: Use Traditional Authentication
Continue using the traditional connection string with username/password for local development.

### Option 3: Service Principal
Create a service principal for local development and add it to the AD group.

## Monitoring and Troubleshooting

### Common Issues

1. **Deployment Script Failures**
   - Check that the Azure AD administrator has been properly configured
   - Ensure the deployment script managed identity has appropriate permissions
   - Review deployment script logs in the Azure portal

2. **Connection Issues**
   - Verify the managed identity is a member of the AD group
   - Check that the database role was created correctly
   - Ensure the application is using the correct connection string

3. **Permission Issues**
   - Verify the database role has the necessary permissions
   - Check that schema permissions are correctly applied

### Monitoring Queries

```sql
-- Check current connections
SELECT usename, application_name, client_addr, state 
FROM pg_stat_activity 
WHERE state = 'active';

-- Check role permissions
SELECT r.rolname, r.rolsuper, r.rolinherit, r.rolcreaterole, r.rolcreatedb, r.rolcanlogin
FROM pg_roles r
WHERE r.rolname LIKE '%dialogporten%';

-- Check table permissions
SELECT schemaname, tablename, tableowner
FROM pg_tables 
WHERE schemaname = 'public';
```

## Security Considerations

1. **Principle of Least Privilege**: The implementation currently grants `db_owner` permissions. Consider creating more granular roles if needed.

2. **Network Security**: PostgreSQL server is deployed in a private subnet and only accessible through the VNet.

3. **Audit Logging**: Enable PostgreSQL audit logging for compliance requirements.

4. **Token Rotation**: Managed identity tokens are automatically rotated by Azure.

## Migration Strategy

This implementation supports gradual migration:

1. **Phase 1**: Deploy infrastructure with both authentication methods enabled
2. **Phase 2**: Update applications one by one to use managed identity
3. **Phase 3**: Monitor and verify all applications are using managed identity
4. **Phase 4**: (Future) Disable password authentication if desired

## Rollback Plan

If issues occur:

1. **Applications can fall back** to traditional authentication using the original connection string
2. **Infrastructure remains unchanged** - both authentication methods are enabled
3. **Remove AD group membership** if needed to isolate issues

## Next Steps

After successful deployment:

1. **Update application code** to use managed identity authentication
2. **Implement monitoring** for database connections and performance
3. **Consider granular permissions** if db_owner is too broad
4. **Plan for connection string migration** in application configuration
5. **Set up alerts** for authentication failures

---

## Files Modified

This implementation includes the following new/modified files:

- `.azure/modules/postgreSql/create.bicep` - Added Azure AD authentication configuration
- `.azure/modules/postgreSql/setupManagedIdentityAuth.bicep` - New deployment script for database setup
- `.azure/modules/azureAd/addIdentityToGroup.bicep` - New module for AD group membership
- `.azure/infrastructure/main.bicep` - Added new parameters and module calls
- `.azure/applications/service/main.bicep` - Example application with PostgreSQL group membership
- `POSTGRESQL_MANAGED_IDENTITY_SETUP.md` - This documentation file

For other applications, follow the same pattern as shown in the service application. 