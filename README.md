# Dialogporten

## Getting started with local development

### Mac 

#### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) (see [global.json](global.json) for the currently required version)

#### Installing Podman (Mac)

1. Install [Podman](https://podman.io/)

2. Install dependencies:
```bash
brew tap cfergeau/crc
# https://github.com/containers/podman/issues/21064
brew install vfkit
brew install docker-compose
```

3. Restart your Mac

4. Finish setup in Podman Desktop

5. Check that `Docker Compatility mode` is enabled, see the bottom left corner

6. Enable privileged [testcontainers-dotnet](https://github.com/testcontainers/testcontainers-dotnet/issues/876#issuecomment-1930397928)  
`echo "ryuk.container.privileged = true" >> $HOME/.testcontainers.properties`

### Windows 

#### Prerequisites

- [Git](https://git-scm.com/download/win)
- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [WSL2](https://docs.microsoft.com/en-us/windows/wsl/install) (To install, open a PowerShell admin window and run `wsl --install`)
- [Virtual Machine Platform](https://support.microsoft.com/en-us/windows/enable-virtualization-on-windows-11-pcs-c5578302-6e43-4b4b-a449-8ced115f58e1) (Installs with WSL2, see the link above)

#### Installing Podman (Windows)

1. Install [Podman Desktop](https://podman.io/getting-started/installation).
 
2. Start Podman Desktop and follow instructions to install Podman.

3. Follow instructions in Podman Desktop to create and start a Podman machine.

4. In Podman Desktop, go to Settings → Resources and run setup for the Compose Extension. This will install docker-compose.

### Running the project

You can run the entire project locally using `podman compose`. (This uses docker-compose behind the scenes.)
```powershell
podman compose up
```

The following GUI services should now be available:
* WebAPI/SwaggerUI: [localhost:7124/swagger](https://localhost:7214/swagger/index.html)
* GraphQl/BananaCakePop: [localhost:7215/graphql](https://localhost:7214/swagger/index.html)
* Redis/Insight: [localhost:7216](https://localhost:7214/swagger/index.html)

The WebAPI and GraphQl services are behind a nginx proxy, and you can change the number of replicas by setting the `scale` property in the `docker-compose.yml` file.


### Running the WebApi/GraphQl in an IDE
If you need do debug the WebApi/GraphQl projects in an IDE, you can alternatively run `podman compose` without the WebAPI/GraphQl.  
First, create a dotnet user secret for the DB connection string.

```powershell
dotnet user-secrets set -p .\src\Digdir.Domain.Dialogporten.WebApi\ "Infrastructure:DialogDbConnectionString" "Server=localhost;Port=5432;Database=Dialogporten;User ID=postgres;Password=supersecret;Include Error Detail=True;"
```

Then run `podman compose` without the WebAPI/GraphQl projects.
```powershell
podman compose -f docker-compose-no-webapi.yml up 
```

## DB development
This project uses Entity Framework core to manage DB migrations. DB development can either be done through Visual Studios Package Manager Console (PMC) or through the CLI. 

### DB development through PMC
Set Digdir.Domain.Dialogporten.Infrastructure as the startup project in Visual Studio's solution explorer, and as the default project in PMC. You are now ready to use [EF core tools through PMC](https://learn.microsoft.com/en-us/ef/core/cli/powershell). 
Run the following command for more information:
```powershell
Get-Help about_EntityFrameworkCore
```

### DB development through CLI
Install the CLI tool with the following command:
```powershell
dotnet tool install --global dotnet-ef
```

You are now ready to use [EF core tools through CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet). Run the following command for more information:
```powershell
dotnet ef --help
```

Remember to target `Digdir.Domain.Dialogporten.Infrastructure` project when running the CLI commands. Either target it through the command using the `-p` option, i.e.
```powershell
dotnet ef migrations add -p .\src\Digdir.Domain.Dialogporten.Infrastructure\ TestMigration
```

Or change your directory to the infrastructure project and then run the command.
```powershell
cd .\src\Digdir.Domain.Dialogporten.Infrastructure\
dotnet ef migrations add TestMigration
```
## Testing

Besides ordinary unit and integration tests, there are test suites for both functional and non-functional end-to-end tests implemented with [K6](https://k6.io/).

See [tests/k6/README.md](tests/k6/README.md) for more information.

## Health Checks

The project includes integrated health checks that are exposed through standard endpoints:
- `/health/startup` - Dependency checks
- `/health/liveness` - Self checks
- `/health/readiness` - Critical service checks
- `/health` - General health status
- `/health/deep` - Comprehensive health check including external services

These health checks are integrated with Azure Container Apps' health probe system and are used to monitor the application's health status.

## Observability with OpenTelemetry

This project uses OpenTelemetry for distributed tracing, metrics collection, and logging. The setup includes:

### Core Features
- Distributed tracing across services
- Runtime and application metrics
- Log aggregation and correlation
- Integration with Azure Monitor/Application Insights
- Support for both OTLP and Azure Monitor exporters
- Automatic instrumentation for:
  - ASP.NET Core
  - HTTP clients
  - Entity Framework Core
  - PostgreSQL
  - FusionCache

### Configuration

OpenTelemetry is configured through environment variables that are automatically provided by Azure Container Apps in production environments:

```json
{
    "OTEL_SERVICE_NAME": "your-service-name",
    "OTEL_EXPORTER_OTLP_ENDPOINT": "http://your-collector:4317",
    "OTEL_EXPORTER_OTLP_PROTOCOL": "grpc",
    "OTEL_RESOURCE_ATTRIBUTES": "key1=value1,key2=value2",
    "APPLICATIONINSIGHTS_CONNECTION_STRING": "your-connection-string"
}
```

### Local Development

For local development, the project includes a docker-compose setup with:
- OpenTelemetry Collector (ports 4317/4318 for OTLP receivers)
- Grafana (port 3000)
- Jaeger (port 16686)
- Loki (port 3100)
- Prometheus (port 9090)

To run the local observability stack:
```bash
podman compose -f docker-compose-otel.yml up
```

### Accessing Observability Tools

Once the local stack is running, you can access the following tools:

#### Distributed Tracing with Jaeger
- URL: http://localhost:16686
- Features:
  - View distributed traces across services
  - Search by service, operation, or trace ID
  - Analyze timing and dependencies
  - Debug request flows and errors

#### Metrics with Prometheus
- URL: http://localhost:9090
- Features:
  - Query raw metrics data
  - View metric targets and service discovery
  - Debug metric collection

#### Log Aggregation with Loki
- Direct URL: http://localhost:3100
- Grafana Integration: http://localhost:3000 (preferred interface)
- Features:
  - Search and filter logs across all services
  - Correlate logs with traces using trace IDs
  - Create log-based alerts and dashboards
  - Use LogQL to query logs:
    ```logql
    # Example: Find all error logs
    {container="web-api"} |= "error"
    
    # Example: Find logs with specific trace ID
    {container=~"web-api|graphql"} |~ "trace_id=([a-f0-9]{32})"
    ```

#### Metrics and Dashboards in Grafana
- URL: http://localhost:3000
- Features:
  - Pre-configured dashboards for:
    - Application metrics
    - Runtime metrics
    - HTTP request metrics
  - Data sources:
    - Prometheus (metrics)
    - Loki (logs)
    - Jaeger (traces)
  - Create custom dashboards
  - Set up alerts

#### OpenTelemetry Collector Endpoints
- OTLP gRPC receiver: localhost:4317
- OTLP HTTP receiver: localhost:4318
- Prometheus metrics: localhost:8888
- Prometheus exporter metrics: localhost:8889

### Request Filtering

The telemetry setup includes smart filtering to:
- Exclude health check endpoints from tracing
- Filter out duplicate traces from Azure SDK clients
- Only record relevant HTTP client calls

For more details about the OpenTelemetry setup, see the `ConfigureTelemetry` method in `AspNetUtilitiesExtensions.cs`.

## Updating the SDK in global.json
When RenovateBot updates `global.json` or base image versions in Dockerfiles, make sure they match. 
The `global.json` file should always have the same SDK version as the base image in the Dockerfiles. 
This is to ensure that the SDK version used in the local development environment matches the SDK version used in the CI/CD pipeline. 
`global.json` is used when building the solution in CI/CD.

## Development in local and test environments
To generate test tokens, see https://github.com/Altinn/AltinnTestTools. There is a request in the Postman collection for this.

### Local development settings
We are able to toggle some external resources in local development. This is done through the `appsettings.Development.json` file. The following settings are available:
```json
"LocalDevelopment": {
    "UseLocalDevelopmentUser": true,
    "UseLocalDevelopmentResourceRegister": true,
    "UseLocalDevelopmentOrganizationRegister": true,
    "UseLocalDevelopmentNameRegister": true,
    "UseLocalDevelopmentAltinnAuthorization": true,
    "UseLocalDevelopmentCloudEventBus": true,
    "UseLocalDevelopmentCompactJwsGenerator": true,
    "DisableCache": true,
    "DisableAuth": true,
    "UseInMemoryServiceBusTransport": true,
    "DisableSubjectResourceSyncOnStartup": false,
    "DisablePolicyInformationSyncOnStartup": true
}
```
Toggling these flags will enable/disable the external resources. The `DisableAuth` flag, for example, will disable authentication in the WebAPI project. This is useful when debugging the WebAPI project in an IDE. These settings will only be respected in the `Development` environment.

### Using `appsettings.local.json`

During local development, it is natural to tweak configurations. Some of these configurations are _meant_ to be shared through git, such as the endpoint for a new integration that may be used during local development. Other configurations are only meant for a specific debug session or a developer's personal preferences, which _should not be shared_ through git, such as lowering the log level below warning.

The configuration in the `appsettings.local.json` file takes precedence over **all** other configurations and is only loaded in the **Development environment**. Additionally, it is ignored by git through the `.gitignore` file.

If developers need to add configuration that should be shared, they should use `appsettings.Development.json`. If the configuration is not meant to be shared, they can create an `appsettings.local.json` file to override the desired settings.

Here is an example of enabling debug logging only locally:
```json5
// appsettings.local.json
{
    "Serilog": {
        "WriteTo": [
            {
                "Name": "Console",
                "Args": {
                    "outputTemplate": "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                }
            }
        ],
        "MinimumLevel": {
            "Default": "Debug"
        }
    }
}
```

#### Adding `appsettings.local.json` to new projects
Add the following to the `Program.cs` file to load the `appsettings.local.json` file:
```csharp
var builder = WebApplication.CreateBuilder(args);
// or var builder = CoconaApp.CreateBuilder(args);
// or var builder = Host.CreateApplicationBuilder(args);
// or some other builder implementing IHostApplicationBuilder

// Left out for brevity
builder.Configuration
    // Add local configuration as the last configuration source to override other configurations
    //.AddSomeOtherConfiguration()
    .AddLocalConfiguration(builder.Environment);

// Left out for brevity
```

## Pull requests
For pull requests, the title must follow [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/).
The title of the PR will be used as the commit message when squashing/merging the pull request, and the body of the PR will be used as the description.

This title will be used to generate the changelog (using [Release Please](https://github.com/google-github-actions/release-please-action))
Using `fix` will add to "Bug Fixes", `feat` will add to "Features", `chore` will add to "Miscellaneous Chores". All the others, `test`, `ci`, `trivial` etc., will be ignored. ([Example release](https://github.com/altinn/dialogporten/releases/tag/v1.12.0))

## Deployment

This repository contains code for both infrastructure and applications. Configurations for infrastructure are located in `.azure/infrastructure`. Application configuration is in `.azure/applications`. 

### Deployment process

Deployments are done using `GitHub Actions` with the following steps:

#### 1. Create and Merge Pull Request
- **Action**: Create a pull request.
- **Merge**: Once the pull request is reviewed and approved, merge it into the `main` branch.

#### 2. Build and Deploy to Test
- **Trigger**: Merging the pull request into `main`.
- **Action**: The code is built and deployed to the test environment.
- **Tag**: The deployment is tagged with `<version>-<git-sha>`.

#### 3. Prepare Release for Staging
- **Passive**: Release-please creates or updates a release pull request.
- **Purpose**: This generates a changelog and bumps the version number.
- **Merge**: Merge the release pull request into the `main` branch.

#### 4. Deploy to Staging and YT01 (Bump Version and Create Tag)
- **Trigger**: Merging the release pull request.
- **Action**: 
  - Bumps the version number.
  - Generates the release and changelog.
  - Deployment is tagged with the new `<version>` without `<git-sha>`
  - The new version is built and deployed to the staging environment (tt02) and the performance environment (yt01).

#### 5. Prepare deployment to Production
- **Action**: Perform a dry run towards the production environment to ensure the deployment can proceed without issues.

#### 6. Deploy to Production
- **Trigger**: Manual trigger of workflow, specify the version to deploy.
- **Action**: The specified version is deployed to the production environment.

#### Visual Workflow

![Deployment process](docs/deploy-process.png)

[Release Please](https://github.com/google-github-actions/release-please-action) is used to create releases, generate changelog and bumping version numbers.

`CHANGELOG.md` and `version.txt` are automatically updated and should not be changed manually.

### Manual deployment (⚠️ handle with care)

This project uses two GitHub dispatch workflows to manage manual deployments: `dispatch-apps.yml` and `dispatch-infrastructure.yml`. These workflows allow for manual triggers of deployments through GitHub Actions, providing flexibility for deploying specific versions to designated environments.

#### Using `dispatch-apps.yml`

The `dispatch-apps.yml` workflow is responsible for deploying applications. To trigger this workflow:

1. Navigate to the Actions tab in the GitHub repository.
2. Select the `Dispatch Apps` workflow.
3. Click on "Run workflow".
4. Fill in the required inputs:
   - **environment**: Choose the target environment (`test`, `yt01`, `staging`, or `prod`).
   - **version**: Specify the version to deploy. Could be git tag or a docker-tag published in packages.
   - **runMigration** (optional): Indicate whether to run database migrations (`true` or `false`).

This workflow will handle the deployment of applications based on the specified parameters, ensuring that the correct version is deployed to the chosen environment.

#### Using `dispatch-infrastructure.yml`

The `dispatch-infrastructure.yml` workflow is used for deploying infrastructure components. To use this workflow:

1. Go to the Actions tab in the GitHub repository.
2. Select the `Dispatch Infrastructure` workflow.
3. Click on "Run workflow".
4. Provide the necessary inputs:
   - **environment**: Select the environment you wish to deploy to (`test`, `yt01`, `staging`, or `prod`).
   - **version**: Enter the version to deploy, which should correspond to a git tag. (e.g., `1.23.4`).

This workflow facilitates the deployment of infrastructure to the specified environment, using the version details provided.

### GitHub Actions

Naming conventions for GitHub Actions:
- `workflow-*.yml`: Reusable workflows
- `ci-cd-*.yml`: Workflows that are triggered by an event
- `dispatch-*.yml`: Workflows that are dispatchable

The `workflow-check-for-changes.yml` workflow uses the `tj-actions/changed-files` action to check which files have been altered since last commit or tag. We use this filter to ensure we only deploy backend code or infrastructure if the respective files have been altered. 

### Infrastructure

Infrastructure definitions for the project are located in the `.azure/infrastructure` folder. To add new infrastructure components, follow the existing pattern found within this directory. This involves creating new Bicep files or modifying existing ones to define the necessary infrastructure resources.

For example, to add a new storage account, you would:
- Create or update a Bicep file within the `.azure/infrastructure` folder to include the storage account resource definition.
- Ensure that the Bicep file is referenced correctly in `.azure/infrastructure/infrastructure.bicep` to be included in the deployment process.

Refer to the existing infrastructure definitions as templates for creating new components.

#### Deploying a new infrastructure environment

A few resources need to be created before we can apply the Bicep to create the main resources. 

The resources refer to a `source key vault` in order to fetch the necessary secrets and store them in the key vault for the environment. An `ssh`-key is also necessary for the `ssh-jumper` used to access the resources in Azure within the `vnet`. 

Use the following steps:

- Ensure a `source key vault` exist for the new environment. Either create a new key vault or use an existing key vault. Currently, two key vaults exist for our environments. One in the test subscription used by Test and Staging, and one in our Production subscription, which Production uses. Ensure you add the necessary secrets that should be used by the new environment. Read here to learn about secret convention [Configuration Guide](docs/Configuration.md). Ensure also that the key vault has the following enabled: `Azure Resource Manager for template deployment`.

- Ensure that a role assignment `Key Vault Secrets User` and `Contributer`(should be inherited) is added for the service principal used by the GitHub Entra Application.

- Create an SSH key in Azure and discard the private key. We will use the `az cli` to access the virtual machine so storing the `ssh key` is only a security risk. 

- Create a new environment in GitHub and add the following secrets: `AZURE_CLIENT_ID`, `AZURE_SOURCE_KEY_VAULT_NAME`, `AZURE_SOURCE_KEY_VAULT_RESOURCE_GROUP`, `AZURE_SOURCE_KEY_VAULT_SUBSCRIPTION_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_TENANT_ID` and `AZURE_SOURCE_KEY_VAULT_SSH_JUMPER_SSH_PUBLIC_KEY`

- Add a new file for the environment `.azure/infrastructure/<env>.bicepparam`. `<env>` must match the environment created in GitHub.

- Add the new environment in the `dispatch-infrastructure.yml` list of environments. 

- Run the GitHub action `Dispatch infrastructure` with the `version` you want to deploy and `environment`. All the resources in `.azure/infrastructure/main.bicep` should now be created. 

- (The GitHub action might need to restart because of a timeout when creating Redis).

#### Connecting to resources in Azure

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
   
   See [scripts/database-forwarder/README.md](scripts/database-forwarder/README.md) for a more user-friendly way to establish database connections through SSH.

### Applications

All application Bicep definitions are located in the `.azure/applications` folder. To add a new application, follow the existing pattern found within this directory. This involves creating a new folder for your application under `.azure/applications` and adding the necessary Bicep files (`main.bicep` and environment-specific parameter files, e.g., `test.bicepparam`, `staging.bicepparam`).

For example, to add a new application named `web-api-new`, you would:
- Create a new folder: `.azure/applications/web-api-new`
- Add a `main.bicep` file within this folder to define the application's infrastructure.
- Use the appropriate `Bicep`-modules within this file. There is one for `Container apps` which you most likely would use.
- Add parameter files for each environment (e.g., `test.bicepparam`, `staging.bicepparam`) to specify environment-specific values.

Refer to the existing applications like `web-api-so` and `web-api-eu` as templates.

#### Deploying applications in a new infrastructure environment

Ensure you have followed the steps in [Deploying a new infrastructure environment](#deploying-a-new-infrastructure-environment) to have the resources required for the applications.

Use the following steps:

- From the infrastructure resources created, add the following GitHub secrets in the new environment (this will not be necessary in the future as secrets would be added directly from infrastructure deployment): `AZURE_APP_CONFIGURATION_NAME`, `AZURE_APP_INSIGHTS_CONNECTION_STRING`, `AZURE_CONTAINER_APP_ENVIRONMENT_NAME`, `AZURE_ENVIRONMENT_KEY_VAULT_NAME`, `AZURE_REDIS_NAME`, `AZURE_RESOURCE_GROUP_NAME`, `AZURE_SERVICE_BUS_NAMESPACE_NAME` and `AZURE_SLACK_NOTIFIER_FUNCTION_APP_NAME`

- Add new parameter files for the environment in all applications `.azure/applications/*/<env>.bicepparam`

- Run the GitHub action `Dispatch applications` in order to deploy all applications to the new environment.

- To expose the applications through APIM, see [Common APIM Guide](docs/CommonAPIM.md)
