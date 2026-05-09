targetScope = 'subscription'

@description('Azure region')
param location string = 'centralindia'
param resourceGroupName string = 'rg-leave-portal'
param appName string = 'leave-portal'
@secure()
param postgresAdminPassword string
param postgresAdminUser string = 'leaveadmin'
param staticWebAppSku string = 'Free'

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
}

module resources 'resources.bicep' = {
  name: 'leavePortalResources'
  scope: rg
  params: {
    location: location
    appName: appName
    postgresAdminUser: postgresAdminUser
    postgresAdminPassword: postgresAdminPassword
    staticWebAppSku: staticWebAppSku
  }
}
