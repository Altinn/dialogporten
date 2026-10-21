// Resolves the object ids of the workload managed identities and pairs them with the profile each
// workload is provisioned onto.
//
// This is its own module because the object ids are only known once the deployment runs, and a
// variable loop must be computable at the start of one. A module output loop is evaluated at the
// end, so it can carry them, and the caller consumes the finished array.

@description('The environment name prefix the identities are named after, for example dp-be-test')
@minLength(3)
param namePrefix string

@description('The workloads to resolve. name is the identity name between the prefix and "-identity".')
param workloads { name: string, profile: string }[]

resource workloadIdentities 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = [
  for workload in workloads: {
    name: '${namePrefix}-${workload.name}-identity'
  }
]

@description('One entry per workload: the PostgreSQL login role name, which is the identity name, the object id the role is bound to, and the profile it is made a member of.')
output provisionWorkloads { roleName: string, objectId: string, profile: string }[] = [
  for (workload, i) in workloads: {
    roleName: workloadIdentities[i].name
    objectId: workloadIdentities[i].properties.principalId
    profile: workload.profile
  }
]
