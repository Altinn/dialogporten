using './main.bicep'

param environment = 'yt01'
param location = 'norwayeast'
param whitelistedIPs = [
  '51.13.85.197'
]
param imageTag = readEnvironmentVariable('IMAGE_TAG')
param revisionSuffix = readEnvironmentVariable('REVISION_SUFFIX')
param resources = {
    cpu: 2
    memory: '4Gi'
}

param otelTraceSamplerRatio = '0.05'

// Scale to zero when idle, use HTTP scaling to wake on traffic
param scale = {
  minReplicas: 0
  maxReplicas: 20
  rules: [
    {
      name: 'http-rule'
      http: {
        metadata: {
          concurrentRequests: '10'
        }
      }
    }
    {
      name: 'cpu'
      custom: {
        type: 'cpu'
        metadata: {
          type: 'Utilization'
          value: '50'
        }
      }
    }
    {
      name: 'memory'
      custom: {
        type: 'memory'
        metadata: {
          type: 'Utilization'
          value: '70'
        }
      }
    }
  ]
}

// Use dedicated workload profile
param workloadProfileName = 'Dedicated-D8'

// secrets
param environmentKeyVaultName = readEnvironmentVariable('AZURE_ENVIRONMENT_KEY_VAULT_NAME')
param containerAppEnvironmentName = readEnvironmentVariable('AZURE_CONTAINER_APP_ENVIRONMENT_NAME')
param appInsightConnectionString = readEnvironmentVariable('AZURE_APP_INSIGHTS_CONNECTION_STRING')
param appConfigurationName = readEnvironmentVariable('AZURE_APP_CONFIGURATION_NAME')
