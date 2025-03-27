@description('Base name for the VM set resources')
param namePrefix string

@description('The location to deploy the resources to')
param location string

@description('The subnet to deploy the network interfaces to')
param subnetId string

@description('Tags to be applied to the resources')
param tags object

@description('The SSH public key to be used for the virtual machines')
@secure()
param sshPublicKey string

@description('The object ID of the group to assign the Admin Login role')
param adminLoginGroupObjectId string

@description('Number of VMs to create')
@minValue(1)
@maxValue(3)
param instanceCount int = 2

@description('The application ports to load balance (do not include SSH port 22 here)')
param loadBalancedPorts array = [
  {
    port: 80
    protocol: 'Tcp'
  }
  {
    port: 443
    protocol: 'Tcp'
  }
]

@description('Enable Just-in-Time access for SSH (port 22)')
param enableJit bool = true

// Create a public IP for the load balancer
resource publicIp 'Microsoft.Network/publicIPAddresses@2024-05-01' = {
  name: '${namePrefix}-lb-ip'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Regional'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
    publicIPAddressVersion: 'IPv4'
  }
  tags: tags
}

// Create the load balancer
resource loadBalancer 'Microsoft.Network/loadBalancers@2024-05-01' = {
  name: '${namePrefix}-lb'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Regional'
  }
  properties: {
    frontendIPConfigurations: [
      {
        name: 'frontendIP'
        properties: {
          publicIPAddress: {
            id: publicIp.id
          }
        }
      }
    ]
    backendAddressPools: [
      {
        name: 'backendPool'
      }
    ]
    probes: [for port in loadBalancedPorts: {
      name: 'probe-${port.port}'
      properties: {
        protocol: port.protocol
        port: port.port
        intervalInSeconds: 5
        numberOfProbes: 2
      }
    }]
    loadBalancingRules: [for port in loadBalancedPorts: {
      name: 'lbrule-${port.port}'
      properties: {
        frontendIPConfiguration: {
          id: resourceId('Microsoft.Network/loadBalancers/frontendIPConfigurations', '${namePrefix}-lb', 'frontendIP')
        }
        backendAddressPool: {
          id: resourceId('Microsoft.Network/loadBalancers/backendAddressPools', '${namePrefix}-lb', 'backendPool')
        }
        probe: {
          id: resourceId('Microsoft.Network/loadBalancers/probes', '${namePrefix}-lb', 'probe-${port.port}')
        }
        protocol: port.protocol
        frontendPort: port.port
        backendPort: port.port
        enableFloatingIP: false
        idleTimeoutInMinutes: 4
      }
    }]
  }
  tags: tags
}

// Create network interfaces for each VM
resource networkInterfaces 'Microsoft.Network/networkInterfaces@2024-05-01' = [for i in range(0, instanceCount): {
  name: '${namePrefix}-vm${i + 1}-nic'
  location: location
  properties: {
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          subnet: {
            id: subnetId
          }
          privateIPAllocationMethod: 'Dynamic'
          loadBalancerBackendAddressPools: [
            {
              id: resourceId('Microsoft.Network/loadBalancers/backendAddressPools', loadBalancer.name, 'backendPool')
            }
          ]
        }
      }
    ]
  }
  tags: tags
}]

// Create VMs across availability zones
module virtualMachines '../virtualMachine/main.bicep' = [for i in range(0, instanceCount): {
  name: '${namePrefix}-vm${i + 1}'
  params: {
    name: '${namePrefix}-vm${i + 1}'
    location: location
    tags: tags
    sshPublicKey: sshPublicKey
    adminLoginGroupObjectId: adminLoginGroupObjectId
    enableJit: enableJit
    availabilityZones: [string((i % 3) + 1)]  // Distribute VMs across zones 1, 2, 3
    hardwareProfile: {
      vmSize: 'Standard_B2s'  // Adjust as needed
    }
    additionalCapabilities: {
      hibernationEnabled: false
    }
    storageProfile: {
      imageReference: {
        publisher: 'canonical'
        offer: '0001-com-ubuntu-server-focal'
        sku: '20_04-lts-gen2'
        version: 'latest'
      }
      osDisk: {
        osType: 'Linux'
        name: '${namePrefix}-vm${i + 1}-osdisk'
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
          id: networkInterfaces[i].id
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
}]

output loadBalancerIp string = publicIp.properties.ipAddress
output vmNames array = [for i in range(0, instanceCount): virtualMachines[i].name] 
