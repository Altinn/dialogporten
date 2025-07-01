using './main.bicep'

param environment = 'yt01'
param location = 'norwayeast'
param imageTag = readEnvironmentVariable('IMAGE_TAG')
param replicaTimeOutInSeconds = 86400

//secrets
param containerAppEnvironmentName = readEnvironmentVariable('AZURE_CONTAINER_APP_ENVIRONMENT_NAME')
param environmentKeyVaultName = readEnvironmentVariable('AZURE_ENVIRONMENT_KEY_VAULT_NAME')
