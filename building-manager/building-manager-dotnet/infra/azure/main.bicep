targetScope = 'resourceGroup'

@description('Azure region; defaults to the resource group location.')
param location string = resourceGroup().location

@description('Name prefix for every resource.')
param appName string = 'building-manager'

@description('PostgreSQL administrator login.')
param postgresAdminLogin string = 'bmadmin'

@description('PostgreSQL administrator password.')
@secure()
param postgresAdminPassword string

@description('JWT signing secret for the API (min 32 chars; stored in Key Vault).')
@secure()
param jwtSecret string

@description('Admin account email seeded on first startup (stored in Key Vault).')
param adminEmail string

@description('Admin account password (stored in Key Vault).')
@secure()
param adminPassword string

@description('Origins the API allows via CORS (the Blazor client origins).')
param allowedOrigins array = []

@description('Maximum silo replicas. Orleans silo-to-silo traffic inside Container Apps is single-replica for now.')
param siloMaxReplicas int = 1

var unique = uniqueString(resourceGroup().id)
var acrName = toLower(take('${replace(appName, '-', '')}${unique}', 40))
var kvName = take('${appName}-kv-${unique}', 24)
var pgName = '${appName}-pg-${unique}'
var storageName = toLower(take('${replace(appName, '-', '')}docs${unique}', 24))
var envName = '${appName}-env'
var dbConnString = 'Host=${pgServer.properties.fullyQualifiedDomainName};Port=5432;Database=building_manager_orleans;Username=${postgresAdminLogin};Password=${postgresAdminPassword}'

resource vnet 'Microsoft.Network/virtualNetworks@2023-11-01' = {
  name: '${appName}-vnet'
  location: location
  properties: {
    addressSpace: { addressPrefixes: ['10.0.0.0/16'] }
    subnets: [
      {
        name: 'aca'
        properties: {
          addressPrefix: '10.0.0.0/23'
          delegations: [
            { name: 'Microsoft.App.environments', properties: { serviceName: 'Microsoft.App/environments' } }
          ]
        }
      }
    ]
  }
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: { name: 'Basic' }
  properties: { adminUserEnabled: false }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource staticWebsite 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
  parent: storage
  name: 'default'
  properties: {
    staticWebsite: {
      enabled: true
      indexDocument: 'index.html'
      errorDocument404Path: 'index.html'
    }
    cors: {
      corsRules: [
        {
          allowedOrigins: empty(allowedOrigins) ? ['https://${storageName}.blob.${environment().suffixes.storage}'] : allowedOrigins
          allowedMethods: ['GET']
          allowedHeaders: ['*']
          exposedHeaders: ['*']
          maxAgeInSeconds: 3600
        }
      ]
    }
  }
}

resource pgServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01' = {
  name: pgName
  location: location
  sku: { name: 'Standard_B1ms', tier: 'Burstable' }
  properties: {
    version: '16'
    administratorLogin: postgresAdminLogin
    administratorLoginPassword: postgresAdminPassword
    storage: { storageSizeGB: 32 }
    backup: { backupRetentionDays: 7 }
    highAvailability: { mode: 'Disabled' }
    network: { publicNetworkAccess: 'Enabled' }
    authConfig: {
      activeDirectoryAuthEnabled: false
      passwordAuthEnabled: true
    }
  }

  resource orleansDatabase 'databases' = {
    name: 'building_manager_orleans'
    properties: { charset: 'UTF8' }
  }

  resource allowAzure 'firewallRules' = {
    name: 'allow-azure-services'
    properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: kvName
  location: location
  properties: {
    sku: { name: 'standard', family: 'A' }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: false
  }
}

resource kvJwtSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Jwt--Secret'
  properties: { value: jwtSecret }
}

resource kvAdminEmail 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Auth--AdminEmail'
  properties: { value: adminEmail }
}

resource kvAdminPassword 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Auth--AdminPassword'
  properties: { value: adminPassword }
}

resource kvOrleansCs 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ConnectionStrings--OrleansStorage'
  properties: { value: dbConnString }
}

resource kvBlobCs 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Storage--BlobConnectionString'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
  }
}

resource appEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: envName
  location: location
  properties: {
    vnetConfiguration: {
      infrastructureSubnetId: vnet.properties.subnets[0].id
      internal: false
    }
    zoneRedundant: false
  }
}

resource siloApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${appName}-silo'
  location: location
  identity: { type: 'SystemAssigned' }
  properties: {
    environmentId: appEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        { server: acr.properties.loginServer, identity: 'system' }
      ]
      secrets: [
        { name: 'orleans-connection', keyVaultUrl: kvOrleansCs.properties.secretUri, identity: 'system' }
      ]
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
        additionalPortMappings: [
          { exposedPort: 30000, targetPort: 30000, external: false }
        ]
      }
    }
    template: {
      scale: { minReplicas: 1, maxReplicas: siloMaxReplicas }
      containers: [
        {
          name: 'silo'
          image: '${acr.properties.loginServer}/silo:latest'
          resources: { cpu: json('1.0'), memory: '2Gi' }
          env: [
            { name: 'ConnectionStrings__OrleansStorage', secretRef: 'orleans-connection' }
          ]
        }
      ]
    }
  }
}

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${appName}-api'
  location: location
  identity: { type: 'SystemAssigned' }
  properties: {
    environmentId: appEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        { server: acr.properties.loginServer, identity: 'system' }
      ]
      secrets: [
        { name: 'orleans-connection', keyVaultUrl: kvOrleansCs.properties.secretUri, identity: 'system' }
      ]
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
      }
    }
    template: {
      scale: { minReplicas: 1, maxReplicas: 3 }
      containers: [
        {
          name: 'api'
          image: '${acr.properties.loginServer}/api:latest'
          resources: { cpu: json('1.0'), memory: '1Gi' }
          env: [
            { name: 'ConnectionStrings__OrleansStorage', secretRef: 'orleans-connection' }
            { name: 'KeyVault__VaultUri', value: keyVault.properties.vaultUri }
            { name: 'Storage__ContainerName', value: 'building-documents' }
            { name: 'Orleans__ClusterId', value: 'building-manager' }
            { name: 'Orleans__ServiceId', value: 'building-manager-service' }
            { name: 'Cors__AllowedOrigins__0', value: 'https://${storageName}.blob.${environment().suffixes.storage}' }
          ]
        }
      ]
    }
  }
}

resource acrPullSilo 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, siloApp.id, 'AcrPull')
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalId: siloApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource acrPullApi 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, apiApp.id, 'AcrPull')
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

var kvSecretsUserRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')

resource kvSiloRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, siloApp.id, 'KeyVaultSecretsUser')
  scope: keyVault
  properties: {
    roleDefinitionId: kvSecretsUserRole
    principalId: siloApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource kvApiRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, apiApp.id, 'KeyVaultSecretsUser')
  scope: keyVault
  properties: {
    roleDefinitionId: kvSecretsUserRole
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource blobDataOwnerStorage 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, apiApp.id, 'StorageBlobDataContributor')
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output acrLoginServer string = acr.properties.loginServer
output apiUrl string = 'https://${apiApp.properties.configuration.ingress.fqdn}'
output blazorUrl string = 'https://${storageName}.blob.${environment().suffixes.storage}'
output keyVaultUri string = keyVault.properties.vaultUri
output postgresName string = pgServer.name
