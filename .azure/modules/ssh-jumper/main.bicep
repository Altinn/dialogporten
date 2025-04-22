@description('The name prefix to be used for the resource')
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

var name = '${namePrefix}-ssh-jumper'

resource publicIp 'Microsoft.Network/publicIPAddresses@2024-05-01' = {
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

resource networkInterface 'Microsoft.Network/networkInterfaces@2024-05-01' = {
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
    hardwareProfile: {
      vmSize: 'Standard_B1s'
    }
    additionalCapabilities: {
      hibernationEnabled: false
    }
    storageProfile: {
      imageReference: {
        publisher: 'canonical'
        offer: 'ubuntu-24_04-lts'
        sku: 'server'
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
