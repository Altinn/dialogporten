@description('The location where the resources will be deployed')
param location string

@description('The environment variables for the container app')
param envVariables array = []

@description('The port on which the container app will run')
param port int = 8080

@description('The name of the container app')
param name string

@description('The image to be used for the container app')
param image string

@description('List of IP address ranges allowed to access the container app ingress (e.g. APIM public IPs)')
param whitelistedIPs array = []

@description('The ID of the container app environment')
param containerAppEnvId string

@description('The tags to be applied to the container app')
param tags object

@description('CPU and memory resources for the container app')
param resources object?

@description('The suffix for the revision of the container app')
param revisionSuffix string

@description('The workload profile to use for the container app')
param workloadProfileName string = 'Consumption'

@export()
type ScaleRule = {
  name: string
  // add additional types as needed: https://keda.sh/docs/2.15/scalers/
  custom: {
    type: 'cpu' | 'memory'
    metadata: {
      type: 'Utilization'
      value: string
    }
  }
}

@export()
type Scale = {
  minReplicas: int
  maxReplicas: int
  rules: ScaleRule[]
}

@description('The scaling configuration for the container app')
param scale Scale = {
  minReplicas: 1
  maxReplicas: 1
  rules: []
}

@export()
type Probes = {
    startup: {
      periodSeconds: int
      initialDelaySeconds: int
      successThreshold: int
      failureThreshold: int
      timeoutSeconds: int
    }
    liveness: {
      periodSeconds: int
      initialDelaySeconds: int
      successThreshold: int
      failureThreshold: int
      timeoutSeconds: int
    }
    readiness: {
      periodSeconds: int
      initialDelaySeconds: int
      successThreshold: int
      failureThreshold: int
      timeoutSeconds: int
    }
}

@description('The health probe configuration for the container app')
param probes Probes = {
  startup: {
    periodSeconds: 10
    initialDelaySeconds: 10
    successThreshold: 1
    failureThreshold: 3
    timeoutSeconds: 2
  }
  readiness: {
    periodSeconds: 5
    initialDelaySeconds: 15
    successThreshold: 1
    failureThreshold: 3
    timeoutSeconds: 2
  }
  liveness: {
    periodSeconds: 5
    initialDelaySeconds: 20
    successThreshold: 1
    failureThreshold: 3
    timeoutSeconds: 2
  }
}

var probeList = [
  {
    periodSeconds: probes.startup.periodSeconds
    initialDelaySeconds: probes.startup.initialDelaySeconds
    successThreshold: probes.startup.successThreshold
    failureThreshold: probes.startup.failureThreshold
    timeoutSeconds: probes.startup.timeoutSeconds
    type: 'Startup'
    httpGet: {
      path: '/health/startup'
      port: port
    }
  }
  {
    periodSeconds: probes.readiness.periodSeconds
    initialDelaySeconds: probes.readiness.initialDelaySeconds
    successThreshold: probes.readiness.successThreshold
    failureThreshold: probes.readiness.failureThreshold
    timeoutSeconds: probes.readiness.timeoutSeconds
    type: 'Readiness'
    httpGet: {
      path: '/health/readiness'
      port: port
    }
  }
  {
    periodSeconds: probes.liveness.periodSeconds
    initialDelaySeconds: probes.liveness.initialDelaySeconds
    failureThreshold: probes.liveness.failureThreshold
    successThreshold: probes.liveness.successThreshold
    timeoutSeconds: probes.liveness.timeoutSeconds
    type: 'Liveness'
    httpGet: {
      path: '/health/liveness'
      port: port
    }
  }
]

@description('The ID of the user-assigned managed identity')
@minLength(1)
param userAssignedIdentityId string

// Container app revision name does not allow '.' character
var cleanedRevisionSuffix = replace(revisionSuffix, '.', '-')

var ipSecurityRestrictions = empty(whitelistedIPs)
  ? []
  : [
      for (ip, i) in whitelistedIPs: {
        name: 'whitelist-${i}'
        action: 'Allow'
        ipAddressRange: ip
      }
    ]

var ingress = {
  targetPort: port
  external: true
  ipSecurityRestrictions: ipSecurityRestrictions
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: last(split(userAssignedIdentityId, '/'))
}

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  properties: {
    configuration: {
      ingress: ingress
    }
    environmentId: containerAppEnvId
    workloadProfileName: workloadProfileName
    template: {
      revisionSuffix: cleanedRevisionSuffix
      scale: scale
      containers: [
        {
          name: name
          image: image
          env: envVariables
          probes: probeList
          resources: resources
        }
      ]
    }
  }
  tags: tags
}

output identityPrincipalId string = managedIdentity.properties.principalId
output name string = containerApp.name
output revisionName string = containerApp.properties.latestRevisionName
