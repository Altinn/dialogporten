param namePrefix string

@description('The location to deploy the resource to')
param location string

@description('The subnet to deploy the network interface to')
param subnetId string

@description('Tags to be applied to the resource')
param tags object

@description('The SSH public key to be used for the virtual machine')
@secure()
param sshPublicKey string

@description('The object ID of the group to assign the Admin Login role for SSH Jumper')
param adminLoginGroupObjectId string

@description('The size of the virtual machine')
param vmSize string

@description('First day of the nightly patch window, yyyy-MM-dd. Defaults to tomorrow so the value is never in the past when the maintenance configuration is created.')
param patchWindowStartDate string = dateTimeAdd(utcNow(), 'P1D', 'yyyy-MM-dd')

var name = '${namePrefix}-ssh-jumper'

// Nightly customer-managed patch window, installing from live package repositories.
resource maintenanceConfiguration 'Microsoft.Maintenance/maintenanceConfigurations@2023-04-01' = {
  name: '${namePrefix}-vm-maint-conf'
  location: location
  properties: {
    maintenanceScope: 'InGuestPatch'
    extensionProperties: {
      InGuestPatchMode: 'User'
    }
    maintenanceWindow: {
      startDateTime: '${patchWindowStartDate} 01:00'
      duration: '02:00'
      recurEvery: '1Day'
      timeZone: 'W. Europe Standard Time'
    }
    installPatches: {
      rebootSetting: 'IfRequired'
      linuxParameters: {
        classificationsToInclude: [
          'Critical'
          'Security'
        ]
      }
    }
  }
  tags: tags
}

resource publicIp 'Microsoft.Network/publicIPAddresses@2025-07-01' = {
  name: '${name}-ip'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Regional'
  }
  zones: [
    '1'
  ]
  properties: {
    publicIPAddressVersion: 'IPv4'
    publicIPAllocationMethod: 'Static'
    idleTimeoutInMinutes: 4
    ipTags: []
  }
  tags: tags
}

resource networkInterface 'Microsoft.Network/networkInterfaces@2025-07-01' = {
  name: name
  location: location
  properties: {
    ipConfigurations: [
      {
        name: '${name}-ipconfig'
        type: 'Microsoft.Network/networkInterfaces/ipConfigurations'
        properties: {
          privateIPAddress: '10.0.0.5'
          privateIPAllocationMethod: 'Dynamic'
          publicIPAddress: {
            id: publicIp.id
            properties: {
              deleteOption: 'Delete'
            }
          }
          subnet: {
            id: subnetId
          }
          primary: true
          privateIPAddressVersion: 'IPv4'
        }
      }
    ]
    dnsSettings: {
      dnsServers: []
    }
    enableAcceleratedNetworking: false
    enableIPForwarding: false
    disableTcpStateTracking: false
    nicType: 'Standard'
    auxiliaryMode: 'None'
    auxiliarySku: 'None'
  }
}

module virtualMachine '../../modules/virtualMachine/main.bicep' = {
  name: name
  params: {
    name: name
    sshPublicKey: sshPublicKey
    location: location
    tags: tags
    adminLoginGroupObjectId: adminLoginGroupObjectId
    enableJit: true
    maintenanceConfigurationId: maintenanceConfiguration.id
    hardwareProfile: {
      vmSize: vmSize
    }
    additionalCapabilities: {
      hibernationEnabled: false
    }
    storageProfile: {
      imageReference: {
        publisher: 'Canonical'
        offer: '0001-com-ubuntu-server-jammy'
        sku: '22_04-lts-gen2'
        version: 'latest'
      }
      osDisk: {
        osType: 'Linux'
        name: '${name}-osdisk'
        createOption: 'FromImage'
        caching: 'ReadWrite'
        managedDisk: {
          storageAccountType: 'Premium_LRS'
        }
        deleteOption: 'Delete'
        diskSizeGB: 30
      }
      dataDisks: []
      diskControllerType: 'SCSI'
    }
    securityProfile: {
      uefiSettings: {
        secureBootEnabled: true
        vTpmEnabled: true
      }
      securityType: 'TrustedLaunch'
      encryptionAtHost: true
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: networkInterface.id
          properties: {
            deleteOption: 'Delete'
          }
        }
      ]
    }
    diagnosticsProfile: {
      bootDiagnostics: {
        enabled: true
      }
    }
  }
}
