// Infrastructure for UnizaPlus: a Linux App Service Plan (F1/Free) and a Linux Web App
// running UnizaPlus.Web in Csv (demo) mode.
//
// No database resource is declared here. UnizaPlus.Web has no EF Core DbContext, no
// SQL Server/SQLite dependency, and no connection string anywhere in its config - it
// serves schedule data from a CSV file bundled in the deployment package and keeps
// every visitor's edits in server memory for the lifetime of their session. There is
// nothing for a database to persist.

targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = 'germanywestcentral'

@description('Base name used to derive resource names. Must be globally unique: it becomes <appName>.azurewebsites.net.')
@minLength(2)
@maxLength(40)
param appName string = 'unizaplus'

@description('.NET version for the App Service Linux runtime stack (DOTNETCORE|<version>). Matches <TargetFramework>net10.0</TargetFramework> in UnizaPlus.Web.csproj.')
param dotnetVersion string = '10.0'

@description('Value for the UnizaPlus:DataSource app setting. This deployment only ever supports Csv: Live mode needs the UnizaPlusBackEnd Selenium scraper and a real Chrome browser, neither of which runs on App Service.')
param dataSource string = 'Csv'

var appServicePlanName = '${appName}-plan'
var webAppName = appName

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: 'F1'
    tier: 'Free'
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|${dotnetVersion}'
      // F1 (Free) does not support Always On; the app cold-starts after idling out.
      alwaysOn: false
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'UnizaPlus__DataSource'
          value: dataSource
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          // The GitHub Actions deploy job publishes a ready build and zip-deploys it,
          // so Kudu/Oryx should serve that package as-is instead of trying to restore
          // and build it again on the server.
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: 'false'
        }
        {
          // Runs the app directly from the immutable, read-only deployed package
          // instead of extracting it to disk - the standard mode for zip/OneDeploy.
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

output webAppName string = webApp.name
output webAppDefaultHostName string = webApp.properties.defaultHostName
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
