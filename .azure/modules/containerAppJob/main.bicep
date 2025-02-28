@description('The location where the resources will be deployed')
param location string

@description('The name of the job')
param name string

@description('The image to be used for the job')
param image string

@description('The ID of the container app environment')
param containerAppEnvId string

@description('The environment variables for the job')
param environmentVariables { name: string, value: string?, secretRef: string? }[] = []

@description('The secrets to be used in the job')
param secrets { name: string, keyVaultUrl: string, identity: 'System' }[] = []

@description('The tags to be applied to the job')
param tags object

@description('The cron expression for the job schedule (optional)')
param cronExpression string = ''

@description('The container args for the job (optional)')
param args string = ''

@description('The ID of the user-assigned managed identity')
@minLength(1)
param userAssignedIdentityId string

@description('The replica timeout for the job in seconds')
param replicaTimeOutInSeconds int

var isScheduled = !empty(cronExpression)

var scheduledJobProperties = {
  triggerType: 'Schedule'
  scheduleTriggerConfig: {
    cronExpression: cronExpression
  }
}

var manualJobProperties = {
  triggerType: 'Manual'
  manualTriggerConfig: {
    parallelism: 1
    replicaCompletionCount: 1
  }
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: last(split(userAssignedIdentityId, '/'))
}

resource job 'Microsoft.App/jobs@2024-03-01' = {
  name: name
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  properties: {
    configuration: union(
      {
        secrets: secrets
        replicaRetryLimit: 1
        replicaTimeout: replicaTimeOutInSeconds
      },
      isScheduled ? scheduledJobProperties : manualJobProperties
    )
    environmentId: containerAppEnvId
    template: {
      containers: [
        {
          env: environmentVariables
          image: image
          name: name
          args: empty(args) ? null : [args]
        }
      ]
    }
  }
  tags: tags
}

output identityPrincipalId string = managedIdentity.properties.principalId
output name string = job.name
