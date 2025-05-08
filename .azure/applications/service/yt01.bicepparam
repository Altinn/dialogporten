using './main.bicep'

param environment = 'yt01'
param location = 'norwayeast'
param imageTag = readEnvironmentVariable('IMAGE_TAG')
param revisionSuffix = readEnvironmentVariable('REVISION_SUFFIX')
param environmentKeyVaultName = readEnvironmentVariable('AZURE_ENVIRONMENT_KEY_VAULT_NAME')
param appConfigurationName = readEnvironmentVariable('AZURE_APP_CONFIGURATION_NAME')
param containerAppEnvironmentName = readEnvironmentVariable('AZURE_CONTAINER_APP_ENVIRONMENT_NAME')
param serviceBusNamespaceName = readEnvironmentVariable('AZURE_SERVICE_BUS_NAMESPACE_NAME')
param resources = {
    cpu: 2
    memory: '4Gi'
}

param otelTraceSamplerRatio = '0.0'

// secrets
param appInsightConnectionString = readEnvironmentVariable('AZURE_APP_INSIGHTS_CONNECTION_STRING')
