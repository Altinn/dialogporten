// Manually triggered job that provisions Entra token authentication to PostgreSQL for the
// workload managed identities: a login role per identity, and membership of the profile role that
// carries the privileges that workload uses.
//
// It runs as a container app job rather than from a workflow runner because the PostgreSQL server
// is VNet-injected and is only reachable from inside this environment.
//
// The job holds no secret. It authenticates to PostgreSQL with a token for its own managed
// identity, which the infrastructure deployment registers as a Microsoft Entra administrator of
// the server, and reaches the table owner from there with SET ROLE.
//
// The work is additive and idempotent: it creates what is missing and re-applies the expected
// grants and memberships. It removes nothing and does not reconcile drift.
//
// Ordering on a brand-new environment: the workload identities this job reads are created by the
// workloads' own templates, so they do not exist until the apps and jobs have been deployed once
// in the default password authentication mode. The first provisioning run must follow that
// deployment; until then the identity lookups below have nothing to resolve.

targetScope = 'resourceGroup'

import { baseTags } from '../../functions/baseTags.bicep'
import { uniqueResourceName } from '../../functions/resourceName.bicep'

@description('The tag of the image to be used')
@minLength(3)
param imageTag string

@description('The environment for the deployment')
@minLength(3)
param environment string

@description('The location where the resources will be deployed')
@minLength(3)
param location string

@description('The name of the container app environment')
@minLength(3)
@secure()
param containerAppEnvironmentName string

@description('The replica timeout for the job in seconds')
param replicaTimeOutInSeconds int

@description('The workload profile name to use, defaults to "Consumption"')
param workloadProfileName string = 'Consumption'

@description('The name stem of the PostgreSQL server, matching serverNameStem in the infrastructure parameter file for this environment.')
@minLength(1)
param serverNameStem string

@description('The application database on the PostgreSQL server')
@minLength(1)
param databaseName string = 'dialogporten'

@description('The PostgreSQL login that owns the application tables. Grants are issued as this role.')
@minLength(1)
param ownerRoleName string = 'dialogportenPgAdmin'

@description('The workloads to provision. name is the identity name between the environment prefix and "-identity"; profile is the PostgreSQL profile role the workload is made a member of.')
param workloads { name: string, profile: string }[]

var namePrefix = 'dp-be-${environment}'
var baseImageUrl = 'ghcr.io/altinn/dialogporten-'

var name = '${namePrefix}-db-provisioner-job'

var additionalTags = {
  FullName: name
  Description: 'Provisions PostgreSQL roles and grants for workload managed identities'
  JobType: 'Manual'
}

var tags = baseTags(additionalTags, environment)

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2025-10-02-preview' existing = {
  name: containerAppEnvironmentName
}

// Created by the infrastructure deployment, which registers it as a Microsoft Entra administrator
// of the PostgreSQL server. Infrastructure always deploys before applications, so it is present.
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: '${namePrefix}-db-provisioner-identity'
}

// The server name is derived the same way the infrastructure derives it, from the name stem and a
// string unique to this subscription and resource group. This job deploys into that same resource
// group, so the two agree and the FQDN can be read here instead of being passed in.
var postgresServerNameMaxLength = 63
var postgresServerName = uniqueResourceName(
  '${namePrefix}-${serverNameStem}',
  postgresServerNameMaxLength,
  subscription().id,
  resourceGroup().id
)

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2025-08-01' existing = {
  name: postgresServerName
}

// Resolves the workloads' own identities, created by their own templates. Only object ids are read.
module workloadPrincipals 'workloadPrincipals.bicep' = {
  name: 'workloadPrincipals-${name}'
  params: {
    namePrefix: namePrefix
    workloads: workloads
  }
}

var containerAppEnvVars = [
  {
    name: 'PROVISION_WORKLOADS'
    value: string(workloadPrincipals.outputs.provisionWorkloads)
  }
  {
    name: 'PGHOST'
    value: postgres.properties.fullyQualifiedDomainName
  }
  {
    name: 'PG_DATABASE'
    value: databaseName
  }
  {
    // This job's own PostgreSQL role name, which is the name Entra authentication maps its
    // identity to.
    name: 'PG_ADMIN_ROLE'
    value: managedIdentity.name
  }
  {
    name: 'PG_OWNER_ROLE'
    value: ownerRoleName
  }
  {
    name: 'AZURE_CLIENT_ID'
    value: managedIdentity.properties.clientId
  }
]

module provisionerJob '../../modules/containerAppJob/main.bicep' = {
  name: name
  params: {
    name: name
    location: location
    image: '${baseImageUrl}db-provisioner:${imageTag}'
    containerAppEnvId: containerAppEnvironment.id
    environmentVariables: containerAppEnvVars
    tags: tags
    userAssignedIdentityId: managedIdentity.id
    replicaTimeOutInSeconds: replicaTimeOutInSeconds
    workloadProfileName: workloadProfileName
  }
}

output name string = provisionerJob.outputs.name
