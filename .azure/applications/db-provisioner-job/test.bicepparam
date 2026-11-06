using './main.bicep'

param environment = 'test'
param location = 'norwayeast'
param imageTag = readEnvironmentVariable('IMAGE_TAG')
// The work is a handful of GRANTs per workload; a run that has not finished well inside this is
// stuck rather than busy.
param replicaTimeOutInSeconds = 600

// Keep in sync with postgresConfiguration.serverNameStem in .azure/infrastructure/test.bicepparam.
param serverNameStem = 'postgres2'

// Test (at23) is provisioned by the db-provisioner-job in Altinn/dialogporten-manifests, which
// reads the workload identities from their ApplicationIdentity resources in the AKS cluster. The
// Container Apps workloads in test stay on password authentication, so this job has none to
// provision here.
param workloads = []

//secrets
param containerAppEnvironmentName = readEnvironmentVariable('AZURE_CONTAINER_APP_ENVIRONMENT_NAME')
