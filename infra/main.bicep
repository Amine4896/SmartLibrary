@description('Nom de base de l\'application. Devrait être unique globalement.')
param appName string = 'smartlibrary-${uniqueString(resourceGroup().id)}'

@description('Région de déploiement des ressources Azure.')
param location string = resourceGroup().location

@description('Nom de l\'image Docker à déployer (ex: nom_utilisateur/smartlibrary:latest).')
param dockerImage string = 'docker.io/library/smartlibrary:latest'

@description('Nom d\'administrateur pour le serveur Azure SQL.')
param sqlAdminUsername string = 'dbadmin'

@description('Mot de passe de l\'administrateur Azure SQL (Sécurisé).')
@secure()
param sqlAdminPassword string

@description('Tier du Plan App Service. F1 est le niveau gratuit.')
param appServicePlanSku string = 'F1'

// --- RESOURCES ---

// 1. Plan App Service (Linux)
resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: '${appName}-plan'
  location: location
  sku: {
    name: appServicePlanSku
    tier: appServicePlanSku == 'F1' ? 'Free' : 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true // Requis pour Linux
  }
}

// 2. Serveur Azure SQL
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: '${appName}-sqlserver'
  location: location
  properties: {
    administratorLogin: sqlAdminUsername
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
  }
}

// 3. Azure SQL Database (Basic Tier - idéal pour le budget étudiant)
resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: 'SmartLibraryDB'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
}

// 4. Règle de Pare-feu SQL : Autoriser les services Azure (comme App Service) à se connecter
resource sqlFirewallAllowAzure 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// 5. Web App pour Conteneur Docker (Linux)
resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: appName
  location: location
  kind: 'app,linux,container'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOCKER|${dockerImage}'
      appSettings: [
        {
          name: 'WEBSITES_PORT'
          value: '8080' // Indique à Azure que le conteneur écoute sur le port 8080
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.name};Persist Security Info=False;User ID=${sqlAdminUsername};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
        }
      ]
    }
  }
}

// --- OUTPUTS ---
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output sqlServerName string = sqlServer.properties.fullyQualifiedDomainName
