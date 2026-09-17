using './main.bicep'

param environment = 'test'
param location = 'norwayeast'
param imageTag = readEnvironmentVariable('IMAGE_TAG')
// The work is a handful of GRANTs per workload; a run that has not finished well inside this is
// stuck rather than busy.
param replicaTimeOutInSeconds = 600

// name is the identity name between the environment prefix and '-identity'.
param workloads = [
  { name: 'webapi-so', profile: 'dp_api_dml' }
  { name: 'webapi-eu', profile: 'dp_api_dml' }
  { name: 'graphql', profile: 'dp_api_dml' }
  { name: 'service', profile: 'dp_service_dml' }
  { name: 'sync-sr-mappings', profile: 'dp_sync_sr_mappings' }
  { name: 'sync-rp-info', profile: 'dp_sync_rp_info' }
  { name: 'reindex-search', profile: 'dp_reindex_search' }
  { name: 'custom-metrics', profile: 'dp_metrics_read' }
]

//secrets
param containerAppEnvironmentName = readEnvironmentVariable('AZURE_CONTAINER_APP_ENVIRONMENT_NAME')
param environmentKeyVaultName = readEnvironmentVariable('AZURE_ENVIRONMENT_KEY_VAULT_NAME')
