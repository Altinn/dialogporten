@description('The location where the resources will be deployed')
param location string

@description('Tags to apply to resources')
param tags object

@description('The name of the PostgreSQL server')
param postgresServerName string

@description('The FQDN of the PostgreSQL server')
param postgresServerFqdn string

@description('The name of the database')
param databaseName string

@description('The name of the Azure AD group for PostgreSQL users')
param azureAdGroupName string

@description('The Object ID of the Azure AD group for PostgreSQL users')
param azureAdGroupObjectId string

@description('The Object ID of the Azure AD administrator')
param azureAdAdministratorObjectId string

@description('The Key Vault name for storing connection strings')
param environmentKeyVaultName string

var deploymentScriptName = 'postgres-managed-identity-setup'

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: '${postgresServerName}-script-identity'
  location: location
  tags: tags
}

// Grant the managed identity permission to connect to PostgreSQL as administrator
// This is needed for the deployment script to run SQL commands
resource postgresAdminRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: resourceGroup()
  name: guid(subscription().id, managedIdentity.id, azureAdAdministratorObjectId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c') // Contributor role for PostgreSQL
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource deploymentScript 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: deploymentScriptName
  location: location
  kind: 'AzureCLI'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    azCliVersion: '2.59.0'
    retentionInterval: 'PT1H'
    cleanupPreference: 'OnExpiration'
    arguments: '${postgresServerFqdn} ${databaseName} "${azureAdGroupName}" ${azureAdGroupObjectId}'
    scriptContent: '''
      #!/bin/bash
      set -e
      
      POSTGRES_SERVER_FQDN=$1
      DATABASE_NAME=$2
      AD_GROUP_NAME=$3
      AD_GROUP_OBJECT_ID=$4
      
      echo "Setting up managed identity authentication for PostgreSQL..."
      echo "Server: $POSTGRES_SERVER_FQDN"
      echo "Database: $DATABASE_NAME"
      echo "AD Group: $AD_GROUP_NAME"
      echo "AD Group Object ID: $AD_GROUP_OBJECT_ID"
      
      # Install PostgreSQL client
      apt-get update
      apt-get install -y postgresql-client
      
      # Get access token for PostgreSQL  
      POSTGRES_RESOURCE="https://ossrdbms-aad$(az cloud show --query suffixes.postgresqlServerEndpointSuffix --output tsv)"
      ACCESS_TOKEN=$(az account get-access-token --resource $POSTGRES_RESOURCE --query accessToken --output tsv)
      
      # Create environment for psql connection
      export PGPASSWORD=$ACCESS_TOKEN
      
      # Connect to PostgreSQL and set up roles
      psql "host=$POSTGRES_SERVER_FQDN port=5432 dbname=$DATABASE_NAME user=$(az account show --query user.name --output tsv) sslmode=require" << EOF
      -- Create role for the AD group if it doesn't exist
      DO \$\$
      BEGIN
        IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '$AD_GROUP_NAME') THEN
          CREATE ROLE "$AD_GROUP_NAME" WITH LOGIN;
          RAISE NOTICE 'Role % created', '$AD_GROUP_NAME';
        ELSE
          RAISE NOTICE 'Role % already exists', '$AD_GROUP_NAME';
        END IF;
      END
      \$\$;
      
      -- Grant database privileges
      GRANT ALL PRIVILEGES ON DATABASE $DATABASE_NAME TO "$AD_GROUP_NAME";
      
      -- Grant schema privileges
      GRANT ALL ON SCHEMA public TO "$AD_GROUP_NAME";
      
      -- Grant privileges on existing tables
      GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO "$AD_GROUP_NAME";
      
      -- Grant privileges on sequences
      GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO "$AD_GROUP_NAME";
      
      -- Grant privileges on functions
      GRANT ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA public TO "$AD_GROUP_NAME";
      
      -- Set default privileges for future objects
      ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL PRIVILEGES ON TABLES TO "$AD_GROUP_NAME";
      ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL PRIVILEGES ON SEQUENCES TO "$AD_GROUP_NAME";
      ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL PRIVILEGES ON FUNCTIONS TO "$AD_GROUP_NAME";
      
      \echo 'Managed identity authentication setup completed successfully'
EOF
      
      echo "PostgreSQL managed identity setup completed"
    '''
  }
  dependsOn: [
    postgresAdminRoleAssignment
  ]
  tags: tags
}

// Create a managed identity connection string for applications
module managedIdentityConnectionString '../keyvault/upsertSecret.bicep' = {
  name: 'managedIdentityConnectionString'
  params: {
    destKeyVaultName: environmentKeyVaultName
    secretName: 'dialogportenManagedIdentityConnectionString'
    secretValue: 'Server=${postgresServerFqdn};Database=${databaseName};Port=5432;Ssl Mode=Require;Trust Server Certificate=true;'
    tags: tags
  }
  dependsOn: [deploymentScript]
}

output managedIdentityConnectionStringSecretUri string = managedIdentityConnectionString.outputs.secretUri
output deploymentScriptOutput string = deploymentScript.properties.outputs.result 
