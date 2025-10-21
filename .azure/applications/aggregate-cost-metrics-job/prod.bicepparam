using './main.bicep'

param environment = 'prod'
param location = 'norwayeast'
param imageTag = readEnvironmentVariable('IMAGE_TAG')
param jobSchedule = '0 4 * * *' // 2:00AM every day
param replicaTimeOutInSeconds = 1800 // 30 minutes
param storageContainerName = 'costmetrics'

//secrets
param containerAppEnvironmentName = readEnvironmentVariable('AZURE_CONTAINER_APP_ENVIRONMENT_NAME')
param appInsightConnectionString = readEnvironmentVariable('AZURE_APP_INSIGHTS_CONNECTION_STRING')
param azureSubscriptionId = readEnvironmentVariable('AZURE_SUBSCRIPTION_ID')
