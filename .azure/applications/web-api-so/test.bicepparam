using './main.bicep'

param environment = 'test'
param location = 'norwayeast'
param apimIp = '51.120.88.69'
param imageTag = readEnvironmentVariable('IMAGE_TAG')
param revisionSuffix = readEnvironmentVariable('REVISION_SUFFIX')

param otelTraceSamplerRatio = '1'

// secrets
param environmentKeyVaultName = readEnvironmentVariable('AZURE_ENVIRONMENT_KEY_VAULT_NAME')
param containerAppEnvironmentName = readEnvironmentVariable('AZURE_CONTAINER_APP_ENVIRONMENT_NAME')
param appInsightConnectionString = readEnvironmentVariable('AZURE_APP_INSIGHTS_CONNECTION_STRING')
param appConfigurationName = readEnvironmentVariable('AZURE_APP_CONFIGURATION_NAME')
