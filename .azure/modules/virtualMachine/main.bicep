param name string
param location string
param tags object

type HardwareProfile = {
  vmSize: string
}
@description('Specifies the hardware profile for the virtual machine')
param hardwareProfile HardwareProfile

type AdditionalCapabilities = {
  hibernationEnabled: bool
}
@description('Specifies the additional capabilities for the virtual machine')
param additionalCapabilities AdditionalCapabilities

type SecurityProfile = {
  uefiSettings: {
    secureBootEnabled: bool
    vTpmEnabled: bool
  }
  securityType: string
}
@description('Specifies the security profile for the virtual machine')
param securityProfile SecurityProfile

type NetworkInterface = {
  id: string
  properties: {
    deleteOption: string
  }
}
type NetworkProfile = {
  networkInterfaces: NetworkInterface[]
}
@description('Specifies the network profile for the virtual machine')
param networkProfile NetworkProfile

type DiagnosticsProfile = {
  bootDiagnostics: {
    enabled: bool
  }
}
@description('Specifies the diagnostics profile for the virtual machine')
param diagnosticsProfile DiagnosticsProfile

type StorageProfile = {
  imageReference: {
    publisher: string
    offer: string
    sku: string
    version: string
  }
  osDisk: {
    osType: 'Linux' | 'Windows' | null
    name: string
    createOption: string
    caching: 'None' | 'ReadOnly' | 'ReadWrite' | null
    managedDisk: {
      storageAccountType: string
    }
    deleteOption: string
    diskSizeGB: int
  }
  dataDisks: array
  diskControllerType: string
}
@description('Specifies the storage profile for the virtual machine')
param storageProfile StorageProfile

@description('Specifies the AD group object ID for the virtual machine administrator login')
param adminLoginGroupObjectId string

@description('Specifies the SSH public key for the virtual machine')
@secure()
param sshPublicKey string

@description('Enable Just-in-Time access for the virtual machine')
param enableJit bool = false

resource virtualMachine 'Microsoft.Compute/virtualMachines@2024-07-01' = {
  name: name
  location: location
  zones: [
    '1'
  ]
  properties: {
    hardwareProfile: hardwareProfile
    additionalCapabilities: additionalCapabilities
    storageProfile: storageProfile
    osProfile: {
      computerName: name
      adminUsername: name
      linuxConfiguration: {
        disablePasswordAuthentication: true
        ssh: {
          publicKeys: [
            {
              path: '/home/${name}/.ssh/authorized_keys'
              keyData: sshPublicKey
            }
          ]
        }
        provisionVMAgent: true
        patchSettings: {
          patchMode: 'AutomaticByPlatform'
          automaticByPlatformSettings: {
            rebootSetting: 'IfRequired'
            bypassPlatformSafetyChecksOnUserSchedule: false
          }
          assessmentMode: 'AutomaticByPlatform'
        }
      }
      secrets: []
      allowExtensionOperations: true
    }
    securityProfile: securityProfile
    networkProfile: networkProfile
    diagnosticsProfile: diagnosticsProfile
  }
  identity: {
    type: 'SystemAssigned'
  }
  tags: tags
}

resource jitPolicy 'Microsoft.Security/locations/jitNetworkAccessPolicies@2020-01-01' = if (enableJit) {
  name: '${location}/${name}'
  kind: 'Basic'
  properties: {
    virtualMachines: [
      {
        id: virtualMachine.id
        ports: [
          {
            number: 22
            protocol: '*'
            allowedSourceAddressPrefix: '*'
            maxRequestAccessDuration: 'PT8H'
          }
        ]
      }
    ]
  }
}

resource aadLoginExtension 'Microsoft.Compute/virtualMachines/extensions@2024-07-01' = {
  parent: virtualMachine
  name: 'AADSSHLoginForLinux'
  location: location
  properties: {
    publisher: 'Microsoft.Azure.ActiveDirectory'
    type: 'AADSSHLoginForLinux'
    typeHandlerVersion: '1.0'
    autoUpgradeMinorVersion: true
  }
}

@description('This is the built-in Virtual Machine Administrator Login role. See https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#compute')
resource vmAdminLoginRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: '1c0163c0-47e6-4577-8991-ea5c82e286e4'
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(virtualMachine.id, adminLoginGroupObjectId, vmAdminLoginRoleDefinition.id)
  scope: virtualMachine
  properties: {
    roleDefinitionId: vmAdminLoginRoleDefinition.id
    principalId: adminLoginGroupObjectId
    principalType: 'Group'
  }
}
