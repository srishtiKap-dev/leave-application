param location string
param appName string
param postgresAdminUser string
@secure()
param postgresAdminPassword string
param staticWebAppSku string

resource log 'Microsoft.OperationalInsights/workspaces@2023-09-01' = { name: '${appName}-log' location: location properties: { sku: { name: 'PerGB2018' } retentionInDays: 30 } }
resource insights 'Microsoft.Insights/components@2020-02-02' = { name: '${appName}-appi' location: location kind: 'web' properties: { Application_Type: 'web' WorkspaceResourceId: log.id } }
resource plan 'Microsoft.Web/serverfarms@2023-12-01' = { name: '${appName}-plan' location: location sku: { name: 'B2' tier: 'Basic' } kind: 'linux' properties: { reserved: true } }
resource pg 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' = { name: '${appName}-pg' location: location sku: { name: 'Standard_B1ms' tier: 'Burstable' } properties: { administratorLogin: postgresAdminUser administratorLoginPassword: postgresAdminPassword version: '16' storage: { storageSizeGB: 32 } backup: { backupRetentionDays: 7 } highAvailability: { mode: 'Disabled' } } }
resource db 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01-preview' = { name: 'leaveportal' parent: pg properties: { charset: 'UTF8' collation: 'en_US.utf8' } }
resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = { name: replace('${appName}st${uniqueString(resourceGroup().id)}', '-', '') location: location sku: { name: 'Standard_LRS' } kind: 'StorageV2' properties: { allowBlobPublicAccess: false minimumTlsVersion: 'TLS1_2' } }
resource receipts 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = { name: 'default/receipts' parent: storage properties: { publicAccess: 'None' } }
resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = { name: '${appName}-kv-${uniqueString(resourceGroup().id)}' location: location properties: { tenantId: subscription().tenantId sku: { family: 'A' name: 'standard' } enableRbacAuthorization: true } }
resource api 'Microsoft.Web/sites@2023-12-01' = { name: '${appName}-api' location: location kind: 'app,linux' properties: { serverFarmId: plan.id httpsOnly: true siteConfig: { linuxFxVersion: 'DOTNETCORE|10.0' alwaysOn: true appSettings: [ { name: 'ConnectionStrings__DefaultConnection' value: 'Host=${pg.properties.fullyQualifiedDomainName};Port=5432;Database=leaveportal;Username=${postgresAdminUser};Password=${postgresAdminPassword};Ssl Mode=Require' } { name: 'ConnectionStrings__Storage' value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}' } { name: 'ApplicationInsights__ConnectionString' value: insights.properties.ConnectionString } { name: 'JwtSettings__Secret' value: 'replace-with-key-vault-secret' } { name: 'JwtSettings__Issuer' value: appName } { name: 'JwtSettings__Audience' value: 'LeavePortalUsers' } { name: 'JwtSettings__AccessTokenExpirationMinutes' value: '60' } { name: 'JwtSettings__RefreshTokenExpirationDays' value: '7' } ] } } }
resource swa 'Microsoft.Web/staticSites@2023-12-01' = { name: '${appName}-web' location: location sku: { name: staticWebAppSku tier: staticWebAppSku } properties: { repositoryUrl: 'https://github.com/replace/replace' branch: 'main' buildProperties: { appLocation: 'frontend' outputLocation: 'dist' } } }
resource cdn 'Microsoft.Cdn/profiles@2023-05-01' = { name: '${appName}-cdn' location: 'global' sku: { name: 'Standard_Microsoft' } }

output apiUrl string = 'https://${api.properties.defaultHostName}'
output staticWebAppUrl string = swa.properties.defaultHostname
