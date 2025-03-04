# Infrastructure

## Resource Naming

All resources follow a consistent naming pattern:
- Prefix: `dp-be-{environment}` (dp = Dialogporten, be = Backend)
- Resource Group: `{prefix}-rg`
- Resources within the group append their type identifier:
  - Key Vault: `{prefix}-kv`
  - App Configuration: `{prefix}-appconfig`
  - Application Insights: `{prefix}-ai`
  - PostgreSQL: `{prefix}-psql`
  - Service Bus: `{prefix}-sb`
  - Virtual Network: `{prefix}-vnet`
  - Redis Cache: `{prefix}-redis`
  - SSH Jumper: `{prefix}-ssh-jumper`

## Secret Management and Cross-Environment Configuration

### Source Key Vault Pattern
The infrastructure uses a source Key Vault pattern for managing secrets across environments:

1. **Source Key Vault Configuration**
   - Subscription ID, Resource Group, and Name are passed as secure parameters
   - Used as the central source of truth for cross-environment secrets

2. **Secret Copying Pattern**
   ```
   Source Key Vault -> Environment-specific Key Vault -> App Configuration
   ```

3. **Environment-Specific Secrets**
   - PostgreSQL passwords follow the pattern: `dialogportenPgAdminPassword{environment}`
   - SSH public keys are stored in the source vault
   - Secrets are conditionally copied based on existence in source vault

4. **Secret Management Flow**
   - Secrets are read from environment variables during deployment
   - Copied to environment-specific Key Vaults
   - Referenced by services using managed identities
   - Some secrets are also copied to App Configuration for application use

## Environment Configuration Patterns

### Parameter Files
Each environment (`prod`, `staging`, `test`, `yt01`) has its own `.bicepparam` file containing:
1. Environment name
2. Location (norwayeast)
3. SKU configurations
4. Environment-specific object IDs
5. Environment URLs

### Environment Variables
Required environment variables for deployment:
- `AZURE_KEY_VAULT_SOURCE_KEYS`
- `PG_ADMIN_PASSWORD`
- `AZURE_SOURCE_KEY_VAULT_SUBSCRIPTION_ID`
- `AZURE_SOURCE_KEY_VAULT_RESOURCE_GROUP`
- `AZURE_SOURCE_KEY_VAULT_NAME`
- `AZURE_SOURCE_KEY_VAULT_SSH_JUMPER_SSH_PUBLIC_KEY`

## Tagging Convention

All resources are tagged with:
```json
{
  "Environment": "<environment-name>",
  "Product": "Dialogporten"
}
```

## Network Segmentation Pattern

The Virtual Network follows a consistent subnet allocation pattern:
1. Default subnet: 10.0.0.0/24
2. PostgreSQL subnet: 10.0.1.0/24
3. Container Apps Environment: 10.0.2.0/23
4. Service Bus subnet: 10.0.4.0/24
5. Redis subnet: 10.0.5.0/24

## Security Patterns

1. **Private Endpoint Pattern**
   - All PaaS services use private endpoints
   - Private DNS zones for each service type
   - Private endpoint groups for service integration

2. **Identity Management Pattern**
   - System-assigned managed identities for services
   - AAD group-based access control
   - Different admin groups for prod/non-prod environments

3. **Secret Rotation Pattern**
   - Secrets stored in source Key Vault
   - Copied to environment-specific vaults
   - Referenced by services using managed identities

## Monitoring Pattern

1. **Application Insights Integration**
   - Workspace-based deployment
   - Availability tests for critical endpoints
   - Optional immediate data purge after 30 days

2. **PostgreSQL Monitoring**
   - Index tuning (configurable per environment)
   - Query performance insights (configurable per environment)
   - Integration with Log Analytics workspace

## High Availability Patterns

### Production Environment
- PostgreSQL: Zone-redundant with standby in zone 2
- Service Bus: Premium SKU with zone redundancy
- Container Apps: Multiple replicas across zones
- Redis: Basic SKU (consider upgrading for HA requirements)

### Non-Production Environments
- Single zone deployments
- Reduced SKUs for cost optimization
- Shorter backup retention periods

## Deploying a new infrastructure environment

A few resources need to be created before we can apply the Bicep to create the main resources. 

The resources refer to a `source key vault` in order to fetch the necessary secrets and store them in the key vault for the environment. An `ssh`-key is also necessary for the `ssh-jumper` used to access the resources in Azure within the `vnet`. 

Use the following steps:

- Ensure a `source key vault` exist for the new environment. Either create a new key vault or use an existing key vault. Currently, two key vaults exist for our environments. One in the test subscription used by Test and Staging, and one in our Production subscription, which Production uses. Ensure you add the necessary secrets that should be used by the new environment. Read here to learn about secret convention [Configuration Guide](Configuration.md). Ensure also that the key vault has the following enabled: `Azure Resource Manager for template deployment`.

- Ensure that a role assignment `Key Vault Secrets User` and `Contributer`(should be inherited) is added for the service principal used by the GitHub Entra Application.

- Create an SSH key in Azure and discard the private key. We will use the `az cli` to access the virtual machine so storing the `ssh key` is only a security risk. 

- Create a new environment in GitHub and add the following secrets: `AZURE_CLIENT_ID`, `AZURE_SOURCE_KEY_VAULT_NAME`, `AZURE_SOURCE_KEY_VAULT_RESOURCE_GROUP`, `AZURE_SOURCE_KEY_VAULT_SUBSCRIPTION_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_TENANT_ID` and `AZURE_SOURCE_KEY_VAULT_SSH_JUMPER_SSH_PUBLIC_KEY`

- Add a new file for the environment `.azure/infrastructure/<env>.bicepparam`. `<env>` must match the environment created in GitHub.

- Add the new environment in the `dispatch-infrastructure.yml` list of environments. 

- Run the GitHub action `Dispatch infrastructure` with the `version` you want to deploy and `environment`. All the resources in `.azure/infrastructure/main.bicep` should now be created. 

- (The GitHub action might need to restart because of a timeout when creating Redis).

## Deploying applications in a new infrastructure environment

Ensure you have followed the steps in [Deploying a new infrastructure environment](#deploying-a-new-infrastructure-environment) to have the resources required for the applications.

Use the following steps:

- From the infrastructure resources created, add the following GitHub secrets in the new environment (this will not be necessary in the future as secrets would be added directly from infrastructure deployment): `AZURE_APP_CONFIGURATION_NAME`, `AZURE_APP_INSIGHTS_CONNECTION_STRING`, `AZURE_CONTAINER_APP_ENVIRONMENT_NAME`, `AZURE_ENVIRONMENT_KEY_VAULT_NAME`, `AZURE_REDIS_NAME`, `AZURE_RESOURCE_GROUP_NAME`, `AZURE_SERVICE_BUS_NAMESPACE_NAME` and `AZURE_SLACK_NOTIFIER_FUNCTION_APP_NAME`

- Add new parameter files for the environment in all applications `.azure/applications/*/<env>.bicepparam`

- Run the GitHub action `Dispatch applications` in order to deploy all applications to the new environment.

- To expose the applications through APIM, see [Common APIM Guide](CommonAPIM.md)

## Connecting to resources in Azure

There is a `ssh-jumper` virtual machine deployed with the infrastructure. This can be used to create a `ssh`-tunnel into the `vnet`. There are two ways to establish connections:

1. Using `az ssh` commands directly:
   ```bash
   # Connect to the VNet using:
   az ssh vm --resource-group dp-be-<env>-rg --vm-name dp-be-<env>-ssh-jumper
   
   # Or create an SSH tunnel for specific resources (e.g., PostgreSQL database):
   az ssh vm -g dp-be-<env>-rg -n dp-be-<env>-ssh-jumper -- -L 5432:<database-host-name>:5432
   ```
   This example forwards the PostgreSQL default port (5432) to your localhost. Adjust the ports and hostnames as needed for other resources.

   You may be prompted to install the ssh extension.

2. Using the forwarding utility script:
   
   See [scripts/database-forwarder/README.md](../scripts/database-forwarder/README.md) for a more user-friendly way to establish database connections through SSH.