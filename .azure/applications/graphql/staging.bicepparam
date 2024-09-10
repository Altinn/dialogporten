using './main.bicep'

param environment = 'staging'
param location = 'norwayeast'
// param apimIp = '51.13.86.131'
param imageTag = readEnvironmentVariable('IMAGE_TAG')

// secrets
param environmentKeyVaultName = readEnvironmentVariable('AZURE_ENVIRONMENT_KEY_VAULT_NAME')
param containerAppEnvironmentName = readEnvironmentVariable('AZURE_CONTAINER_APP_ENVIRONMENT_NAME')
param appInsightConnectionString = readEnvironmentVariable('AZURE_APP_INSIGHTS_CONNECTION_STRING')
param appConfigurationName = readEnvironmentVariable('AZURE_APP_CONFIGURATION_NAME')
