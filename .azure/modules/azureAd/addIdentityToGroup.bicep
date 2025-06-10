@description('The location where the resources will be deployed')
param location string

@description('Tags to apply to resources')
param tags object

@description('The Object ID of the Azure AD group')
param groupObjectId string

@description('The Object ID of the managed identity to add to the group')
param identityObjectId string

@description('A unique name for this deployment script instance')
param deploymentScriptName string

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: '${deploymentScriptName}-script-identity'
  location: location
  tags: tags
}

// Grant the managed identity permissions to manage group membership
resource groupMembershipRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: resourceGroup()
  name: guid(subscription().id, managedIdentity.id, groupObjectId, 'GroupMembership')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '9b895d92-2cd3-44c7-9d02-a6ac2d5ea5c3') // Graph Directory.ReadWrite.All
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource deploymentScript 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: deploymentScriptName
  location: location
  kind: 'AzureCLI'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    azCliVersion: '2.59.0'
    retentionInterval: 'PT1H'
    cleanupPreference: 'OnExpiration'
    arguments: '${groupObjectId} ${identityObjectId}'
    scriptContent: '''
      #!/bin/bash
      set -e
      
      GROUP_OBJECT_ID=$1
      IDENTITY_OBJECT_ID=$2
      
      echo "Adding managed identity to Azure AD group..."
      echo "Group Object ID: $GROUP_OBJECT_ID"
      echo "Identity Object ID: $IDENTITY_OBJECT_ID"
      
      # Check if the identity is already a member of the group
      if az ad group member check --group $GROUP_OBJECT_ID --member-id $IDENTITY_OBJECT_ID --query value --output tsv | grep -q "true"; then
        echo "Identity is already a member of the group"
      else
        echo "Adding identity to group..."
        az ad group member add --group $GROUP_OBJECT_ID --member-id $IDENTITY_OBJECT_ID
        echo "Identity successfully added to group"
      fi
      
      echo "Group membership operation completed"
    '''
  }
  dependsOn: [
    groupMembershipRole
  ]
  tags: tags
}

output deploymentScriptOutput string = deploymentScript.properties.outputs.result 
