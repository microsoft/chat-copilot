/*
Copyright (c) Microsoft. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.

Bicep template for deploying CopilotChat Azure resources.
*/

@description('Name for the deployment consisting of alphanumeric characters or dashes (\'-\')')
param name string = 'copichat'

@description('SKU for the Azure App Service plan')
@allowed([ 'B1', 'S1', 'S2', 'S3', 'P1V3', 'P2V3', 'I1V2', 'I2V2' ])
param webAppServiceSku string = 'B1'

@description('Location of package to deploy as the web service')
#disable-next-line no-hardcoded-env-urls
param webApiPackageUri string = 'https://aka.ms/copilotchat/webapi/latest'

@description('Location of package to deploy as the memory pipeline')
#disable-next-line no-hardcoded-env-urls
param memoryPipelinePackageUri string = 'https://aka.ms/copilotchat/memorypipeline/latest'

@description('Location of the websearcher plugin to deploy')
#disable-next-line no-hardcoded-env-urls
param webSearcherPackageUri string = 'https://aka.ms/copilotchat/websearcher/latest'

@description('Underlying AI service')
@allowed([
  'AzureOpenAI'
  'OpenAI'
])
param aiService string = 'AzureOpenAI'

@description('Model to use for chat completions')
param completionModel string = 'gpt-4o'

@description('Model to use for text embeddings')
param embeddingModel string = 'text-embedding-ada-002'

@description('Azure OpenAI endpoint to use (Azure OpenAI only)')
param aiEndpoint string = ''

@secure()
@description('Azure OpenAI or OpenAI API key')
param aiApiKey string

@description('Azure AD client ID for the backend web API')
param webApiClientId string

@description('Azure AD client ID for the frontend')
param frontendClientId string

@description('Azure AD tenant ID for authenticating users')
param azureAdTenantId string

@description('Azure AD cloud instance for authenticating users')
param azureAdInstance string = environment().authentication.loginEndpoint

@description('Whether to deploy a new Azure OpenAI instance')
param deployNewAzureOpenAI bool = false

@description('Whether to deploy Cosmos DB for persistent chat storage')
param deployCosmosDB bool = true

@description('What method to use to persist embeddings')
@allowed([
  'AzureAISearch'
  'Qdrant'
])
param memoryStore string = 'AzureAISearch'

@description('Whether to deploy Azure Speech Services to enable input by voice')
param deploySpeechServices bool = true

@description('Whether to deploy the web searcher plugin, which requires a Bing resource')
param deployWebSearcherPlugin bool = false

@description('Whether to deploy pre-built binary packages to the cloud')
param deployPackages bool = true

@description('Region for the resources')
param location string = resourceGroup().location

@description('Hash of the resource group ID')
var rgIdHash = uniqueString(resourceGroup().id)

@description('Deployment name unique to resource group')
var uniqueName = '${name}-${rgIdHash}'

@description('Name of the Azure Storage file share to create')
var storageFileShareName = 'aciqdrantshare'

resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = if (deployNewAzureOpenAI) {
  name: 'ai-${uniqueName}'
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: toLower(uniqueName)
  }
}

resource openAI_completionModel 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = if (deployNewAzureOpenAI) {
  parent: openAI
  name: completionModel
  sku: {
    name: 'Standard'
    capacity: 30
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: completionModel
    }
  }
}

resource openAI_embeddingModel 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = if (deployNewAzureOpenAI) {
  parent: openAI
  name: embeddingModel
  sku: {
    name: 'Standard'
    capacity: 30
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: embeddingModel
    }
  }
  dependsOn: [// This "dependency" is to create models sequentially because the resource
    openAI_completionModel // provider does not support parallel creation of models properly.
  ]
}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: 'asp-${uniqueName}-webapi'
  location: location
  kind: 'app'
  sku: {
    name: webAppServiceSku
  }
}

resource appServiceWeb 'Microsoft.Web/sites@2022-09-01' = {
  name: 'app-${uniqueName}-webapi'
  location: location
  kind: 'app'
  tags: {
    skweb: '1'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    virtualNetworkSubnetId: memoryStore == 'Qdrant' ? virtualNetwork.properties.subnets[0].id : null
    siteConfig: {
      healthCheckPath: '/healthz'
    }
  }
}

resource appServiceWebConfig 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: appServiceWeb
  name: 'web'
  dependsOn: [
    webSubnetConnection
  ]
  properties: {
    alwaysOn: false
    cors: {
      allowedOrigins: [
        'http://localhost:3000'
        'https://localhost:3000'
      ]
      supportCredentials: true
    }
    detailedErrorLoggingEnabled: true
    minTlsVersion: '1.2'
    netFrameworkVersion: 'v6.0'
    use32BitWorkerProcess: false
    vnetRouteAllEnabled: true
    webSocketsEnabled: true
    appSettings: concat([
        {
          name: 'Authentication:Type'
          value: 'AzureAd'
        }
        {
          name: 'Authentication:AzureAd:Instance'
          value: azureAdInstance
        }
        {
          name: 'Authentication:AzureAd:TenantId'
          value: azureAdTenantId
        }
        {
          name: 'Authentication:AzureAd:ClientId'
          value: webApiClientId
        }
        {
          name: 'Authentication:AzureAd:Scopes'
          value: 'access_as_user'
        }
        {
          name: 'ChatStore:Type'
          value: deployCosmosDB ? 'cosmos' : 'volatile'
        }
        {
          name: 'ChatStore:Cosmos:Database'
          value: 'CopilotChat'
        }
        {
          name: 'ChatStore:Cosmos:ChatSessionsContainer'
          value: 'chatsessions'
        }
        {
          name: 'ChatStore:Cosmos:ChatMessagesContainer'
          value: 'chatmessages'
        }
        {
          name: 'ChatStore:Cosmos:ChatMemorySourcesContainer'
          value: 'chatmemorysources'
        }
        {
          name: 'ChatStore:Cosmos:ChatParticipantsContainer'
          value: 'chatparticipants'
        }
        {
          name: 'ChatStore:Cosmos:ConnectionString'
          value: deployCosmosDB ? cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString : ''
        }
        {
          name: 'AzureSpeech:Region'
          value: location
        }
        {
          name: 'AzureSpeech:Key'
          value: deploySpeechServices ? speechAccount.listKeys().key1 : ''
        }
        {
          name: 'AllowedOrigins'
          value: '[*]' // Defer list of allowed origins to the Azure service app's CORS configuration
        }
        {
          name: 'Kestrel:Endpoints:Https:Url'
          value: 'https://localhost:443'
        }
        {
          name: 'Frontend:AadClientId'
          value: frontendClientId
        }
        {
          name: 'Logging:LogLevel:Default'
          value: 'Warning'
        }
        {
          name: 'Logging:LogLevel:CopilotChat.WebApi'
          value: 'Warning'
        }
        {
          name: 'Logging:LogLevel:Microsoft.SemanticKernel'
          value: 'Warning'
        }
        {
          name: 'Logging:LogLevel:Microsoft.AspNetCore.Hosting'
          value: 'Warning'
        }
        {
          name: 'Logging:LogLevel:Microsoft.Hosting.Lifetimel'
          value: 'Warning'
        }
        {
          name: 'Logging:ApplicationInsights:LogLevel:Default'
          value: 'Warning'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~2'
        }
        {
          name: 'KernelMemory:DocumentStorageType'
          value: 'AzureBlobs'
        }
        {
          name: 'KernelMemory:TextGeneratorType'
          value: aiService
        }
        {
          name: 'KernelMemory:DataIngestion:OrchestrationType'
          value: 'Distributed'
        }
        {
          name: 'KernelMemory:DataIngestion:DistributedOrchestration:QueueType'
          value: 'AzureQueue'
        }
        {
          name: 'KernelMemory:DataIngestion:EmbeddingGeneratorTypes:0'
          value: aiService
        }
        {
          name: 'KernelMemory:DataIngestion:MemoryDbTypes:0'
          value: memoryStore
        }
        {
          name: 'KernelMemory:Retrieval:MemoryDbType'
          value: memoryStore
        }
        {
          name: 'KernelMemory:Retrieval:EmbeddingGeneratorType'
          value: aiService
        }
        {
          name: 'KernelMemory:Services:AzureBlobs:Auth'
          value: 'ConnectionString'
        }
        {
          name: 'KernelMemory:Services:AzureBlobs:ConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[1].value}'
        }
        {
          name: 'KernelMemory:Services:AzureBlobs:Container'
          value: 'chatmemory'
        }
        {
          name: 'KernelMemory:Services:AzureQueue:Auth'
          value: 'ConnectionString'
        }
        {
          name: 'KernelMemory:Services:AzureQueue:ConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[1].value}'
        }
        {
          name: 'KernelMemory:Services:AzureAISearch:Auth'
          value: 'ApiKey'
        }
        {
          name: 'KernelMemory:Services:AzureAISearch:Endpoint'
          value: memoryStore == 'AzureAISearch' ? 'https://${azureAISearch.name}.search.windows.net' : ''
        }
        {
          name: 'KernelMemory:Services:AzureAISearch:APIKey'
          value: memoryStore == 'AzureAISearch' ? azureAISearch.listAdminKeys().primaryKey : ''
        }
        {
          name: 'KernelMemory:Services:Qdrant:Endpoint'
          value: memoryStore == 'Qdrant' ? 'https://${appServiceQdrant.properties.defaultHostName}' : ''
        }
        {
          name: 'KernelMemory:Services:AzureOpenAIText:Auth'
          value: 'ApiKey'
        }
        {
          name: 'KernelMemory:Services:AzureOpenAIText:Endpoint'
          value: deployNewAzureOpenAI ? openAI.properties.endpoint : aiEndpoint
        }
        {
          name: 'KernelMemory:Services:AzureOpenAIText:APIKey'
          value: deployNewAzureOpenAI ? openAI.listKeys().key1 : aiApiKey
        }
        {
          name: 'KernelMemory:Services:AzureOpenAIText:Deployment'
          value: completionModel
        }
        {
          name: 'KernelMemory:Services:AzureOpenAIEmbedding:Auth'
          value: 'ApiKey'
        }
        {
          name: 'KernelMemory:Services:AzureOpenAIEmbedding:Endpoint'
          value: deployNewAzureOpenAI ? openAI.properties.endpoint : aiEndpoint
        }
        {
          name: 'KernelMemory:Services:AzureOpenAIEmbedding:APIKey'
          value: deployNewAzureOpenAI ? openAI.listKeys().key1 : aiApiKey
        }
        {
          name: 'KernelMemory:Services:AzureOpenAIEmbedding:Deployment'
          value: embeddingModel
        }
        {
          name: 'KernelMemory:Services:OpenAI:TextModel'
          value: completionModel
        }
        {
          name: 'KernelMemory:Services:OpenAI:EmbeddingModel'
          value: embeddingModel
        }
        {
          name: 'KernelMemory:Services:OpenAI:APIKey'
          value: aiApiKey
        }
        {
          name: 'Plugins:0:Name'
          value: 'Klarna Shopping'
        }
        {
          name: 'Plugins:0:ManifestDomain'
          value: 'https://www.klarna.com'
        }
      ],
      (deployWebSearcherPlugin) ? [
        {
          name: 'Plugins:1:Name'
          value: 'WebSearcher'
        }
        {
          name: 'Plugins:1:ManifestDomain'
          value: 'https://${functionAppWebSearcherPlugin.properties.defaultHostName}'
        }
        {
          name: 'Plugins:1:Key'
          value: listkeys('${functionAppWebSearcherPlugin.id}/host/default/', '2022-09-01').functionKeys.default
        }
      ] : []
    )
  }
}

resource appServiceWebDeploy 'Microsoft.Web/sites/extensions@2022-09-01' = if (deployPackages) {
  name: 'MSDeploy'
  kind: 'string'
  parent: appServiceWeb
  properties: {
    packageUri: webApiPackageUri
  }
  dependsOn: [
    appServiceWebConfig
  ]
}

resource appServiceMemoryPipeline 'Microsoft.Web/sites@2022-09-01' = {
  name: 'app-${uniqueName}-memorypipeline'
  location: location
  kind: 'app'
  tags: {
    skweb: '1'
  }
  properties: {
    httpsOnly: true
    serverFarmId: appServicePlan.id
    virtualNetworkSubnetId: memoryStore == 'Qdrant' ? virtualNetwork.properties.subnets[0].id : null
    siteConfig: {
      alwaysOn: true
    }
  }
}

resource appServiceMemoryPipelineConfig 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: appServiceMemoryPipeline
  name: 'web'
  dependsOn: [
    memSubnetConnection
  ]
  properties: {
    alwaysOn: true
    detailedErrorLoggingEnabled: true
    minTlsVersion: '1.2'
    netFrameworkVersion: 'v6.0'
    use32BitWorkerProcess: false
    vnetRouteAllEnabled: true
    appSettings: [
      {
        name: 'KernelMemory:DocumentStorageType'
        value: 'AzureBlobs'
      }
      {
        name: 'KernelMemory:TextGeneratorType'
        value: aiService
      }
      {
        name: 'KernelMemory:DataIngestion:ImageOcrType'
        value: 'AzureFormRecognizer'
      }
      {
        name: 'KernelMemory:DataIngestion:OrchestrationType'
        value: 'Distributed'
      }
      {
        name: 'KernelMemory:DataIngestion:DistributedOrchestration:QueueType'
        value: 'AzureQueue'
      }
      {
        name: 'KernelMemory:DataIngestion:EmbeddingGeneratorTypes:0'
        value: aiService
      }
      {
        name: 'KernelMemory:DataIngestion:MemoryDbTypes:0'
        value: memoryStore
      }
      {
        name: 'KernelMemory:Retrieval:MemoryDbType'
        value: memoryStore
      }
      {
        name: 'KernelMemory:Retrieval:EmbeddingGeneratorType'
        value: aiService
      }
      {
        name: 'KernelMemory:Services:AzureBlobs:Auth'
        value: 'ConnectionString'
      }
      {
        name: 'KernelMemory:Services:AzureBlobs:ConnectionString'
        value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[1].value}'
      }
      {
        name: 'KernelMemory:Services:AzureBlobs:Container'
        value: 'chatmemory'
      }
      {
        name: 'KernelMemory:Services:AzureQueue:Auth'
        value: 'ConnectionString'
      }
      {
        name: 'KernelMemory:Services:AzureQueue:ConnectionString'
        value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[1].value}'
      }
      {
        name: 'KernelMemory:Services:AzureAISearch:Auth'
        value: 'ApiKey'
      }
      {
        name: 'KernelMemory:Services:AzureAISearch:Endpoint'
        value: memoryStore == 'AzureAISearch' ? 'https://${azureAISearch.name}.search.windows.net' : ''
      }
      {
        name: 'KernelMemory:Services:AzureAISearch:APIKey'
        value: memoryStore == 'AzureAISearch' ? azureAISearch.listAdminKeys().primaryKey : ''
      }
      {
        name: 'KernelMemory:Services:Qdrant:Endpoint'
        value: memoryStore == 'Qdrant' ? 'https://${appServiceQdrant.properties.defaultHostName}' : ''
      }
      {
        name: 'KernelMemory:Services:AzureOpenAIText:Auth'
        value: 'ApiKey'
      }
      {
        name: 'KernelMemory:Services:AzureOpenAIText:Endpoint'
        value: deployNewAzureOpenAI ? openAI.properties.endpoint : aiEndpoint
      }
      {
        name: 'KernelMemory:Services:AzureOpenAIText:APIKey'
        value: deployNewAzureOpenAI ? openAI.listKeys().key1 : aiApiKey
      }
      {
        name: 'KernelMemory:Services:AzureOpenAIText:Deployment'
        value: completionModel
      }
      {
        name: 'KernelMemory:Services:AzureOpenAIEmbedding:Auth'
        value: 'ApiKey'
      }
      {
        name: 'KernelMemory:Services:AzureOpenAIEmbedding:Endpoint'
        value: deployNewAzureOpenAI ? openAI.properties.endpoint : aiEndpoint
      }
      {
        name: 'KernelMemory:Services:AzureOpenAIEmbedding:APIKey'
        value: deployNewAzureOpenAI ? openAI.listKeys().key1 : aiApiKey
      }
      {
        name: 'KernelMemory:Services:AzureOpenAIEmbedding:Deployment'
        value: embeddingModel
      }
      {
        name: 'KernelMemory:Services:AzureFormRecognizer:Auth'
        value: 'ApiKey'
      }
      {
        name: 'KernelMemory:Services:AzureFormRecognizer:Endpoint'
        value: ocrAccount.properties.endpoint
      }
      {
        name: 'KernelMemory:Services:AzureFormRecognizer:APIKey'
        value: ocrAccount.listKeys().key1
      }
      {
        name: 'KernelMemory:Services:OpenAI:TextModel'
        value: completionModel
      }
      {
        name: 'KernelMemory:Services:OpenAI:EmbeddingModel'
        value: embeddingModel
      }
      {
        name: 'KernelMemory:Services:OpenAI:APIKey'
        value: aiApiKey
      }
      {
        name: 'Logging:LogLevel:Default'
        value: 'Information'
      }
      {
        name: 'Logging:LogLevel:AspNetCore'
        value: 'Warning'
      }
      {
        name: 'Logging:ApplicationInsights:LogLevel:Default'
        value: 'Warning'
      }
      {
        name: 'ApplicationInsights:ConnectionString'
        value: appInsights.properties.ConnectionString
      }
    ]
  }
}

resource appServiceMemoryPipelineDeploy 'Microsoft.Web/sites/extensions@2022-09-01' = if (deployPackages) {
  name: 'MSDeploy'
  kind: 'string'
  parent: appServiceMemoryPipeline
  properties: {
    packageUri: memoryPipelinePackageUri
  }
  dependsOn: [
    appServiceMemoryPipelineConfig
  ]
}

resource functionAppWebSearcherPlugin 'Microsoft.Web/sites@2022-09-01' = if (deployWebSearcherPlugin) {
  name: 'function-${uniqueName}-websearcher-plugin'
  location: location
  kind: 'functionapp'
  tags: {
    skweb: '1'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
    }
  }
}

resource functionAppWebSearcherPluginConfig 'Microsoft.Web/sites/config@2022-09-01' = if (deployWebSearcherPlugin) {
  parent: functionAppWebSearcherPlugin
  name: 'web'
  properties: {
    minTlsVersion: '1.2'
    appSettings: [
      {
        name: 'FUNCTIONS_EXTENSION_VERSION'
        value: '~4'
      }
      {
        name: 'FUNCTIONS_WORKER_RUNTIME'
        value: 'dotnet-isolated'
      }
      {
        name: 'AzureWebJobsStorage'
        value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[1].value}'
      }
      {
        name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
        value: appInsights.properties.InstrumentationKey
      }
      {
        name: 'PluginConfig:BingApiKey'
        value: (deployWebSearcherPlugin) ? bingSearchService.listKeys().key1 : ''
      }
    ]
  }
}

resource functionAppWebSearcherDeploy 'Microsoft.Web/sites/extensions@2022-09-01' = if (deployPackages && deployWebSearcherPlugin) {
  name: 'MSDeploy'
  kind: 'string'
  parent: functionAppWebSearcherPlugin
  properties: {
    packageUri: webSearcherPackageUri
  }
  dependsOn: [
    functionAppWebSearcherPluginConfig
  ]
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appins-${uniqueName}'
  location: location
  kind: 'string'
  tags: {
    displayName: 'AppInsight'
  }
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource appInsightExtensionWeb 'Microsoft.Web/sites/siteextensions@2022-09-01' = {
  parent: appServiceWeb
  name: 'Microsoft.ApplicationInsights.AzureWebSites'
  dependsOn: [ appServiceWebDeploy ]
}

resource appInsightExtensionMemory 'Microsoft.Web/sites/siteextensions@2022-09-01' = {
  parent: appServiceMemoryPipeline
  name: 'Microsoft.ApplicationInsights.AzureWebSites'
  dependsOn: [ appServiceMemoryPipelineDeploy ]
}

resource appInsightExtensionWebSearchPlugin 'Microsoft.Web/sites/siteextensions@2022-09-01' = if (deployWebSearcherPlugin) {
  parent: functionAppWebSearcherPlugin
  name: 'Microsoft.ApplicationInsights.AzureWebSites'
  dependsOn: [ functionAppWebSearcherDeploy ]
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'la-${uniqueName}'
  location: location
  tags: {
    displayName: 'Log Analytics'
  }
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 90
    features: {
      searchVersion: 1
      legacy: 0
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: 'st${rgIdHash}' // Not using full unique name to avoid hitting 24 char limit
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
  resource fileservices 'fileServices' = if (memoryStore == 'Qdrant') {
    name: 'default'
    resource share 'shares' = {
      name: storageFileShareName
    }
  }
}

resource appServicePlanQdrant 'Microsoft.Web/serverfarms@2022-03-01' = if (memoryStore == 'Qdrant') {
  name: 'asp-${uniqueName}-qdrant'
  location: location
  kind: 'linux'
  sku: {
    name: 'P1v3'
  }
  properties: {
    reserved: true
  }
}

resource appServiceQdrant 'Microsoft.Web/sites@2022-09-01' = if (memoryStore == 'Qdrant') {
  name: 'app-${uniqueName}-qdrant'
  location: location
  kind: 'app,linux,container'
  properties: {
    serverFarmId: appServicePlanQdrant.id
    httpsOnly: true
    reserved: true
    clientCertMode: 'Required'
    virtualNetworkSubnetId: memoryStore == 'Qdrant' ? virtualNetwork.properties.subnets[1].id : null
    siteConfig: {
      numberOfWorkers: 1
      linuxFxVersion: 'DOCKER|qdrant/qdrant:latest'
      alwaysOn: true
      vnetRouteAllEnabled: true
      ipSecurityRestrictions: [
        {
          vnetSubnetResourceId: memoryStore == 'Qdrant' ? virtualNetwork.properties.subnets[0].id : null
          action: 'Allow'
          priority: 300
          name: 'Allow front vnet'
        }
        {
          ipAddress: 'Any'
          action: 'Deny'
          priority: 2147483647
          name: 'Deny all'
        }
      ]
      azureStorageAccounts: {
        aciqdrantshare: {
          type: 'AzureFiles'
          accountName: memoryStore == 'Qdrant' ? storage.name : 'notdeployed'
          shareName: storageFileShareName
          mountPath: '/qdrant/storage'
          accessKey: memoryStore == 'Qdrant' ? storage.listKeys().keys[0].value : ''
        }
      }
    }
  }
}

resource azureAISearch 'Microsoft.Search/searchServices@2022-09-01' = if (memoryStore == 'AzureAISearch') {
  name: 'acs-${uniqueName}'
  location: location
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
  }
}

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2021-05-01' = if (memoryStore == 'Qdrant') {
  name: 'vnet-${uniqueName}'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
    subnets: [
      {
        name: 'webSubnet'
        properties: {
          addressPrefix: '10.0.1.0/24'
          networkSecurityGroup: {
            id: webNsg.id
          }
          serviceEndpoints: [
            {
              service: 'Microsoft.Web'
              locations: [
                '*'
              ]
            }
          ]
          delegations: [
            {
              name: 'delegation'
              properties: {
                serviceName: 'Microsoft.Web/serverfarms'
              }
            }
          ]
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
      }
      {
        name: 'qdrantSubnet'
        properties: {
          addressPrefix: '10.0.2.0/24'
          networkSecurityGroup: {
            id: qdrantNsg.id
          }
          serviceEndpoints: [
            {
              service: 'Microsoft.Web'
              locations: [
                '*'
              ]
            }
          ]
          delegations: [
            {
              name: 'delegation'
              properties: {
                serviceName: 'Microsoft.Web/serverfarms'
              }
            }
          ]
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
      }
    ]
  }
}

resource webNsg 'Microsoft.Network/networkSecurityGroups@2022-11-01' = if (memoryStore == 'Qdrant') {
  name: 'nsg-${uniqueName}-webapi'
  location: location
  properties: {
    securityRules: [
      {
        name: 'AllowAnyHTTPSInbound'
        properties: {
          protocol: 'TCP'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 100
          direction: 'Inbound'
        }
      }
    ]
  }
}

resource qdrantNsg 'Microsoft.Network/networkSecurityGroups@2022-11-01' = if (memoryStore == 'Qdrant') {
  name: 'nsg-${uniqueName}-qdrant'
  location: location
  properties: {
    securityRules: []
  }
}

resource webSubnetConnection 'Microsoft.Web/sites/virtualNetworkConnections@2022-09-01' = if (memoryStore == 'Qdrant') {
  parent: appServiceWeb
  name: 'webSubnetConnection'
  properties: {
    vnetResourceId: memoryStore == 'Qdrant' ? virtualNetwork.properties.subnets[0].id : null
    isSwift: true
  }
}

resource memSubnetConnection 'Microsoft.Web/sites/virtualNetworkConnections@2022-09-01' = if (memoryStore == 'Qdrant') {
  parent: appServiceMemoryPipeline
  name: 'memSubnetConnection'
  properties: {
    vnetResourceId: memoryStore == 'Qdrant' ? virtualNetwork.properties.subnets[0].id : null
    isSwift: true
  }
}

resource qdrantSubnetConnection 'Microsoft.Web/sites/virtualNetworkConnections@2022-09-01' = if (memoryStore == 'Qdrant') {
  parent: appServiceQdrant
  name: 'qdrantSubnetConnection'
  properties: {
    vnetResourceId: memoryStore == 'Qdrant' ? virtualNetwork.properties.subnets[1].id : null
    isSwift: true
  }
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = if (deployCosmosDB) {
  name: toLower('cosmos-${uniqueName}')
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    consistencyPolicy: { defaultConsistencyLevel: 'Session' }
    locations: [ {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    databaseAccountOfferType: 'Standard'
  }
}

resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = if (deployCosmosDB) {
  parent: cosmosAccount
  name: 'CopilotChat'
  properties: {
    resource: {
      id: 'CopilotChat'
    }
  }
}

resource messageContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = if (deployCosmosDB) {
  parent: cosmosDatabase
  name: 'chatmessages'
  properties: {
    resource: {
      id: 'chatmessages'
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
      }
      partitionKey: {
        paths: [
          '/chatId'
        ]
        kind: 'Hash'
        version: 2
      }
    }
  }
}

resource sessionContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = if (deployCosmosDB) {
  parent: cosmosDatabase
  name: 'chatsessions'
  properties: {
    resource: {
      id: 'chatsessions'
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
      }
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
        version: 2
      }
    }
  }
}

resource participantContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = if (deployCosmosDB) {
  parent: cosmosDatabase
  name: 'chatparticipants'
  properties: {
    resource: {
      id: 'chatparticipants'
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
      }
      partitionKey: {
        paths: [
          '/userId'
        ]
        kind: 'Hash'
        version: 2
      }
    }
  }
}

resource memorySourcesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = if (deployCosmosDB) {
  parent: cosmosDatabase
  name: 'chatmemorysources'
  properties: {
    resource: {
      id: 'chatmemorysources'
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
      }
      partitionKey: {
        paths: [
          '/chatId'
        ]
        kind: 'Hash'
        version: 2
      }
    }
  }
}

resource speechAccount 'Microsoft.CognitiveServices/accounts@2022-12-01' = if (deploySpeechServices) {
  name: 'cog-speech-${uniqueName}'
  location: location
  sku: {
    name: 'S0'
  }
  kind: 'SpeechServices'
  identity: {
    type: 'None'
  }
  properties: {
    customSubDomainName: 'cog-speech-${uniqueName}'
    networkAcls: {
      defaultAction: 'Allow'
    }
    publicNetworkAccess: 'Enabled'
  }
}

resource ocrAccount 'Microsoft.CognitiveServices/accounts@2022-12-01' = {
  name: 'cog-ocr-${uniqueName}'
  location: location
  sku: {
    name: 'S0'
  }
  kind: 'FormRecognizer'
  identity: {
    type: 'None'
  }
  properties: {
    customSubDomainName: 'cog-ocr-${uniqueName}'
    networkAcls: {
      defaultAction: 'Allow'
    }
    publicNetworkAccess: 'Enabled'
  }
}

resource bingSearchService 'Microsoft.Bing/accounts@2020-06-10' = if (deployWebSearcherPlugin) {
  name: 'bing-search-${uniqueName}'
  location: 'global'
  sku: {
    name: 'S1'
  }
  kind: 'Bing.Search.v7'
}

output webapiUrl string = appServiceWeb.properties.defaultHostName
output webapiName string = appServiceWeb.name
output memoryPipelineName string = appServiceMemoryPipeline.name
output pluginNames array = concat(
  [],
  (deployWebSearcherPlugin) ? [ functionAppWebSearcherPlugin.name ] : []
)
