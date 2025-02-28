using './main.bicep'

param environment = 'staging'
param location = 'norwayeast'
param imageTag = readEnvironmentVariable('IMAGE_TAG')
param jobSchedule = '*/5 * * * *' // Runs every 5 minutes
param replicaTimeOutInSeconds = 600

//secrets
param containerAppEnvironmentName = readEnvironmentVariable('AZURE_CONTAINER_APP_ENVIRONMENT_NAME')
param environmentKeyVaultName = readEnvironmentVariable('AZURE_ENVIRONMENT_KEY_VAULT_NAME')
param appInsightConnectionString = readEnvironmentVariable('AZURE_APP_INSIGHTS_CONNECTION_STRING')
