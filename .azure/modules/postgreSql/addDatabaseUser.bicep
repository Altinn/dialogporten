@description('The name of the PostgreSQL server')
param postgresServerName string

@description('The name of the database')
param databaseName string = 'dialogporten'

@description('The name of the managed identity to add as database user')
param managedIdentityName string

@description('The object ID (principal ID) of the managed identity')
param managedIdentityObjectId string

@description('The location for the deployment script')
param location string

@description('Tags to apply to resources')
param tags object

@description('The name of the virtual network')
param virtualNetworkName string

resource deploymentScript 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: 'add-postgres-user-${managedIdentityName}'
  location: location
  tags: tags
  kind: 'AzureCLI'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', '${replace(virtualNetworkName, '-vnet', '')}-postgres-deployment-script-identity')}': {}
    }
  }
  properties: {
    azCliVersion: '2.63.0'
    retentionInterval: 'PT1H'
    timeout: 'PT30M'
    cleanupPreference: 'OnSuccess'
    containerSettings: {
      subnetIds: [
        {
          id: resourceId('Microsoft.Network/virtualNetworks/subnets', virtualNetworkName, 'default')
        }
      ]
    }
    environmentVariables: [
      {
        name: 'POSTGRES_SERVER'
        value: postgresServerName
      }
      {
        name: 'DATABASE_NAME'
        value: databaseName
      }
      {
        name: 'MANAGED_IDENTITY_NAME'
        value: managedIdentityName
      }
      {
        name: 'MANAGED_IDENTITY_OBJECT_ID'
        value: managedIdentityObjectId
      }
      {
        name: 'AAD_RESOURCE_ENDPOINT'
        value: 'https://ossrdbms-aad${environment().suffixes.sqlServerHostname}'
      }
      {
        name: 'AZ_USER_NAME'
        value: deployer().objectId
      }
    ]
    scriptContent: '''
      #!/bin/bash
      set -euo pipefail

      echo "Adding managed identity '$MANAGED_IDENTITY_NAME' as PostgreSQL user..."
      echo "PostgreSQL Server: $POSTGRES_SERVER"
      echo "Database: $DATABASE_NAME"
      echo "Identity Object ID: $MANAGED_IDENTITY_OBJECT_ID"
      echo "Deployer User: $AZ_USER_NAME"

      POSTGRES_HOST="${POSTGRES_SERVER}.postgres.database.azure.com"

      # Install PostgreSQL client
      echo "Installing PostgreSQL client..."
      apt-get update && apt-get install -y postgresql-client

      # Get access token for Azure AD authentication
      echo "Getting Azure AD access token for PostgreSQL..."
      ACCESS_TOKEN=$(az account get-access-token --resource "$AAD_RESOURCE_ENDPOINT" --query accessToken -o tsv)

      # Create the SQL commands to add the user and grant permissions
      SQL_COMMANDS=$(cat <<EOF
      DO \$\$
      BEGIN
          -- Check if user already exists
          IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '$MANAGED_IDENTITY_NAME') THEN
              -- Create the user for the managed identity
              EXECUTE format('CREATE USER %I', '$MANAGED_IDENTITY_NAME');
              RAISE NOTICE 'Created user: $MANAGED_IDENTITY_NAME';
          ELSE
              RAISE NOTICE 'User already exists: $MANAGED_IDENTITY_NAME';
          END IF;
      END
      \$\$;

      -- Grant necessary permissions
      GRANT CONNECT ON DATABASE $DATABASE_NAME TO "$MANAGED_IDENTITY_NAME";
      GRANT USAGE ON SCHEMA public TO "$MANAGED_IDENTITY_NAME";
      GRANT CREATE ON SCHEMA public TO "$MANAGED_IDENTITY_NAME";
      GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO "$MANAGED_IDENTITY_NAME";
      GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO "$MANAGED_IDENTITY_NAME";

      -- Grant permissions on future tables and sequences
      ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO "$MANAGED_IDENTITY_NAME";
      ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO "$MANAGED_IDENTITY_NAME";

      -- Set up Azure AD authentication for the managed identity
      COMMENT ON ROLE "$MANAGED_IDENTITY_NAME" IS 'Azure AD managed identity with object ID: $MANAGED_IDENTITY_OBJECT_ID';
      EOF
      )

      # Execute the SQL commands using Azure AD authentication
      echo "Executing SQL commands to create user and grant permissions..."
      export PGPASSWORD="$ACCESS_TOKEN"
      echo "$SQL_COMMANDS" | psql -h "$POSTGRES_HOST" -p 5432 -d "$DATABASE_NAME" -U "$AZ_USER_NAME" -v ON_ERROR_STOP=1

      echo "Successfully added managed identity '$MANAGED_IDENTITY_NAME' as PostgreSQL user"
      echo "Granted permissions: CONNECT, USAGE, CREATE, SELECT, INSERT, UPDATE, DELETE on database '$DATABASE_NAME'"
    '''
  }
}

output deploymentScriptName string = deploymentScript.name 
