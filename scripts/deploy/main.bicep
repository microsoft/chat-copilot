{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "metadata": {
    "_generator": {
      "name": "bicep",
      "version": "0.22.6.54827",
      "templateHash": "3212961623027419689"
    }
  },
  "parameters": {
    "name": {
      "type": "string",
      "defaultValue": "copichat",
      "metadata": {
        "description": "Name for the deployment consisting of alphanumeric characters or dashes ('-')"
      }
    },
    "webAppServiceSku": {
      "type": "string",
      "defaultValue": "B1",
      "allowedValues": [
        "B1",
        "S1",
        "S2",
        "S3",
        "P1V3",
        "P2V3",
        "I1V2",
        "I2V2"
      ],
      "metadata": {
        "description": "SKU for the Azure App Service plan"
      }
    },
    "webApiPackageUri": {
      "type": "string",
      "defaultValue": "https://aka.ms/copilotchat/webapi/latest",
      "metadata": {
        "description": "Location of package to deploy as the web service"
      }
    },
    "memoryPipelinePackageUri": {
      "type": "string",
      "defaultValue": "https://aka.ms/copilotchat/memorypipeline/latest",
      "metadata": {
        "description": "Location of package to deploy as the memory pipeline"
      }
    },
    "webSearcherPackageUri": {
      "type": "string",
      "defaultValue": "https://aka.ms/copilotchat/websearcher/latest",
      "metadata": {
        "description": "Location of the websearcher plugin to deploy"
      }
    },
    "aiService": {
      "type": "string",
      "defaultValue": "AzureOpenAI",
      "allowedValues": [
        "AzureOpenAI",
        "OpenAI"
      ],
      "metadata": {
        "description": "Underlying AI service"
      }
    },
    "completionModel": {
      "type": "string",
      "defaultValue": "gpt-35-turbo",
      "metadata": {
        "description": "Model to use for chat completions"
      }
    },
    "embeddingModel": {
      "type": "string",
      "defaultValue": "text-embedding-ada-002",
      "metadata": {
        "description": "Model to use for text embeddings"
      }
    },
    "plannerModel": {
      "type": "string",
      "defaultValue": "gpt-35-turbo",
      "metadata": {
        "description": "Completion model the task planner should use"
      }
    },
    "aiEndpoint": {
      "type": "string",
      "defaultValue": "",
      "metadata": {
        "description": "Azure OpenAI endpoint to use (Azure OpenAI only)"
      }
    },
    "aiApiKey": {
      "type": "securestring",
      "defaultValue": "",
      "metadata": {
        "description": "Azure OpenAI or OpenAI API key"
      }
    },
    "webApiClientId": {
      "type": "string",
      "defaultValue": "",
      "metadata": {
        "description": "Azure AD client ID for the backend web API"
      }
    },
    "frontendClientId": {
      "type": "string",
      "defaultValue": "",
      "metadata": {
        "description": "Azure AD client ID for the frontend"
      }
    },
    "azureAdTenantId": {
      "type": "string",
      "defaultValue": "",
      "metadata": {
        "description": "Azure AD tenant ID for authenticating users"
      }
    },
    "azureAdInstance": {
      "type": "string",
      "defaultValue": "[environment().authentication.loginEndpoint]",
      "metadata": {
        "description": "Azure AD cloud instance for authenticating users"
      }
    },
    "deployNewAzureOpenAI": {
      "type": "bool",
      "defaultValue": false,
      "metadata": {
        "description": "Whether to deploy a new Azure OpenAI instance"
      }
    },
    "deployCosmosDB": {
      "type": "bool",
      "defaultValue": true,
      "metadata": {
        "description": "Whether to deploy Cosmos DB for persistent chat storage"
      }
    },
    "memoryStore": {
      "type": "string",
      "defaultValue": "AzureCognitiveSearch",
      "allowedValues": [
        "AzureCognitiveSearch",
        "Qdrant"
      ],
      "metadata": {
        "description": "What method to use to persist embeddings"
      }
    },
    "deploySpeechServices": {
      "type": "bool",
      "defaultValue": true,
      "metadata": {
        "description": "Whether to deploy Azure Speech Services to enable input by voice"
      }
    },
    "deployWebSearcherPlugin": {
      "type": "bool",
      "defaultValue": false,
      "metadata": {
        "description": "Whether to deploy the web searcher plugin, which requires a Bing resource"
      }
    },
    "deployPackages": {
      "type": "bool",
      "defaultValue": true,
      "metadata": {
        "description": "Whether to deploy binary packages to the cloud"
      }
    },
    "location": {
      "type": "string",
      "defaultValue": "[resourceGroup().location]",
      "metadata": {
        "description": "Region for the resources"
      }
    }
  },
  "variables": {
    "rgIdHash": "[uniqueString(resourceGroup().id)]",
    "uniqueName": "[format('{0}-{1}', parameters('name'), variables('rgIdHash'))]",
    "storageFileShareName": "aciqdrantshare"
  },
  "resources": [
    {
      "condition": "[equals(parameters('memoryStore'), 'Qdrant')]",
      "type": "Microsoft.Storage/storageAccounts/fileServices/shares",
      "apiVersion": "2022-09-01",
      "name": "[format('{0}/{1}/{2}', format('st{0}', variables('rgIdHash')), 'default', variables('storageFileShareName'))]",
      "dependsOn": [
        "[resourceId('Microsoft.Storage/storageAccounts/fileServices', format('st{0}', variables('rgIdHash')), 'default')]"
      ]
    },
    {
      "condition": "[equals(parameters('memoryStore'), 'Qdrant')]",
      "type": "Microsoft.Storage/storageAccounts/fileServices",
      "apiVersion": "2022-09-01",
      "name": "[format('{0}/{1}', format('st{0}', variables('rgIdHash')), 'default')]",
      "dependsOn": [
        "[resourceId('Microsoft.Storage/storageAccounts', format('st{0}', variables('rgIdHash')))]"
      ]
    },
    {
      "condition": "[parameters('deployNewAzureOpenAI')]",
      "type": "Microsoft.CognitiveServices/accounts",
      "apiVersion": "2023-05-01",
      "name": "[format('ai-{0}', variables('uniqueName'))]",
      "location": "[parameters('location')]",
      "kind": "OpenAI",
      "sku": {
        "name": "S0"
      },
      "properties": {
        "customSubDomainName": "[toLower(variables('uniqueName'))]"
      }
    },
    {
      "condition": "[parameters('deployNewAzureOpenAI')]",
      "type": "Microsoft.CognitiveServices/accounts/deployments",
      "apiVersion": "2023-05-01",
      "name": "[format('{0}/{1}', format('ai-{0}', variables('uniqueName')), parameters('completionModel'))]",
      "sku": {
        "name": "Standard",
        "capacity": 30
      },
      "properties": {
        "model": {
          "format": "OpenAI",
          "name": "[parameters('completionModel')]"
        }
      },
      "dependsOn": [
        "[resourceId('Microsoft.CognitiveServices/accounts', format('ai-{0}', variables('uniqueName')))]"
      ]
    },
    {
      "condition": "[parameters('deployNewAzureOpenAI')]",
      "type": "Microsoft.CognitiveServices/accounts/deployments",
      "apiVersion": "2023-05-01",
      "name": "[format('{0}/{1}', format('ai-{0}', variables('uniqueName')), parameters('embeddingModel'))]",
      "sku": {
        "name": "Standard",
        "capacity": 30
      },
      "properties": {
        "model": {
          "format": "OpenAI",
          "name": "[parameters('embeddingModel')]"
        }
      },
      "dependsOn": [
        "[resourceId('Microsoft.CognitiveServices/accounts', format('ai-{0}', variables('uniqueName')))]",
        "[resourceId('Microsoft.CognitiveServices/accounts/deployments', format('ai-{0}', variables('uniqueName')), parameters('completionModel'))]"
      ]
    },
    {
      "type": "Microsoft.Web/serverfarms",
      "apiVersion": "2022-03-01",
      "name": "[format('asp-{0}-webapi', variables('uniqueName'))]",
      "location": "[parameters('location')]",
      "kind": "app",
      "sku": {
        "name": "[parameters('webAppServiceSku')]"
      }
    },
    {
      "type": "Microsoft.Web/sites",
      "apiVersion": "2022-09-01",
      "name": "[format('app-{0}-webapi', variables('uniqueName'))]",
      "location": "[parameters('location')]",
      "kind": "app",
      "tags": {
        "skweb": "1"
      },
      "properties": {
        "serverFarmId": "[resourceId('Microsoft.Web/serverfarms', format('asp-{0}-webapi', variables('uniqueName')))]",
        "httpsOnly": true,
        "virtualNetworkSubnetId": "[reference(resourceId('Microsoft.Network/virtualNetworks', format('vnet-{0}', variables('uniqueName'))), '2021-05-01').subnets[0].id]",
        "siteConfig": {
          "healthCheckPath": "/healthz"
        }
      },
      "dependsOn": [
        "[resourceId('Microsoft.Web/serverfarms', format('asp-{0}-webapi', variables('uniqueName')))]",
        "[resourceId('Microsoft.Network/virtualNetworks', format('vnet-{0}', variables('uniqueName')))]"
      ]
    },
    {
      "type": "Microsoft.Web/sites/config",
      "apiVersion": "2022-09-01",
      "name": "[format('{0}/{1}', format('app-{0}-webapi', variables('uniqueName')), 'web')]",
      "properties": {
        "alwaysOn": false,
        "cors": {
          "allowedOrigins": [
            "http://localhost:3000",
            "https://localhost:3000"
          ],
          "supportCredentials": true
        },
        "detailedErrorLoggingEnabled": true,
        "minTlsVersion": "1.2",
        "netFrameworkVersion": "v6.0",
        "use32BitWorkerProcess": false,
        "vnetRouteAllEnabled": true,
        "webSocketsEnabled": true,
        "appSettings": "[concat(createArray(createObject('name', 'Authentication:Type', 'value', 'AzureAd'), createObject('name', 'Authentication:AzureAd:Instance', 'value', parameters('azureAdInstance')), createObject('name', 'Authentication:AzureAd:TenantId', 'value', parameters('azureAdTenantId')), createObject('name', 'Authentication:AzureAd:ClientId', 'value', parameters('webApiClientId')), createObject('name', 'Authentication:AzureAd:Scopes', 'value', 'access_as_user'), createObject('name', 'Planner:Model', 'value', parameters('plannerModel')), createObject('name', 'ChatStore:Type', 'value', if(parameters('deployCosmosDB'), 'cosmos', 'volatile')), createObject('name', 'ChatStore:Cosmos:Database', 'value', 'CopilotChat'), createObject('name', 'ChatStore:Cosmos:ChatSessionsContainer', 'value', 'chatsessions'), createObject('name', 'ChatStore:Cosmos:ChatMessagesContainer', 'value', 'chatmessages'), createObject('name', 'ChatStore:Cosmos:ChatMemorySourcesContainer', 'value', 'chatmemorysources'), createObject('name', 'ChatStore:Cosmos:ChatParticipantsContainer', 'value', 'chatparticipants'), createObject('name', 'ChatStore:Cosmos:ConnectionString', 'value', if(parameters('deployCosmosDB'), listConnectionStrings(resourceId('Microsoft.DocumentDB/databaseAccounts', toLower(format('cosmos-{0}', variables('uniqueName')))), '2023-04-15').connectionStrings[0].connectionString, '')), createObject('name', 'AzureSpeech:Region', 'value', parameters('location')), createObject('name', 'AzureSpeech:Key', 'value', if(parameters('deploySpeechServices'), listKeys(resourceId('Microsoft.CognitiveServices/accounts', format('cog-speech-{0}', variables('uniqueName'))), '2022-12-01').key1, '')), createObject('name', 'AllowedOrigins', 'value', '[*]'), createObject('name', 'Kestrel:Endpoints:Https:Url', 'value', 'https://localhost:443'), createObject('name', 'Frontend:AadClientId', 'value', parameters('frontendClientId')), createObject('name', 'Logging:LogLevel:Default', 'value', 'Warning'), createObject('name', 'Logging:LogLevel:CopilotChat.WebApi', 'value', 'Warning'), createObject('name', 'Logging:LogLevel:Microsoft.SemanticKernel', 'value', 'Warning'), createObject('name', 'Logging:LogLevel:Microsoft.AspNetCore.Hosting', 'value', 'Warning'), createObject('name', 'Logging:LogLevel:Microsoft.Hosting.Lifetimel', 'value', 'Warning'), createObject('name', 'Logging:ApplicationInsights:LogLevel:Default', 'value', 'Warning'), createObject('name', 'APPLICATIONINSIGHTS_CONNECTION_STRING', 'value', reference(resourceId('Microsoft.Insights/components', format('appins-{0}', variables('uniqueName'))), '2020-02-02').ConnectionString), createObject('name', 'ApplicationInsightsAgent_EXTENSION_VERSION', 'value', '~2'), createObject('name', 'SemanticMemory:ContentStorageType', 'value', 'AzureBlobs'), createObject('name', 'SemanticMemory:TextGeneratorType', 'value', parameters('aiService')), createObject('name', 'SemanticMemory:DataIngestion:OrchestrationType', 'value', 'Distributed'), createObject('name', 'SemanticMemory:DataIngestion:DistributedOrchestration:QueueType', 'value', 'AzureQueue'), createObject('name', 'SemanticMemory:DataIngestion:EmbeddingGeneratorTypes:0', 'value', parameters('aiService')), createObject('name', 'SemanticMemory:DataIngestion:VectorDbTypes:0', 'value', parameters('memoryStore')), createObject('name', 'SemanticMemory:Retrieval:VectorDbType', 'value', parameters('memoryStore')), createObject('name', 'SemanticMemory:Retrieval:EmbeddingGeneratorType', 'value', parameters('aiService')), createObject('name', 'SemanticMemory:Services:AzureBlobs:Auth', 'value', 'ConnectionString'), createObject('name', 'SemanticMemory:Services:AzureBlobs:ConnectionString', 'value', format('DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}', format('st{0}', variables('rgIdHash')), listKeys(resourceId('Microsoft.Storage/storageAccounts', format('st{0}', variables('rgIdHash'))), '2022-09-01').keys[1].value)), createObject('name', 'SemanticMemory:Services:AzureBlobs:Container', 'value', 'chatmemory'), createObject('name', 'SemanticMemory:Services:AzureQueue:Auth', 'value', 'ConnectionString'), createObject('name', 'SemanticMemory:Services:AzureQueue:ConnectionString', 'value', format('DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}', format('st{0}', variables('rgIdHash')), listKeys(resourceId('Microsoft.Storage/storageAccounts', format('st{0}', variables('rgIdHash'))), '2022-09-01').keys[1].value)), createObject('name', 'SemanticMemory:Services:AzureCognitiveSearch:Auth', 'value', 'ApiKey'), createObject('name', 'SemanticMemory:Services:AzureCognitiveSearch:Endpoint', 'value', if(equals(parameters('memoryStore'), 'AzureCognitiveSearch'), format('https://{0}.search.windows.net', format('acs-{0}', variables('uniqueName'))), '')), createObject('name', 'SemanticMemory:Services:AzureCognitiveSearch:APIKey', 'value', if(equals(parameters('memoryStore'), 'AzureCognitiveSearch'), listAdminKeys(resourceId('Microsoft.Search/searchServices', format('acs-{0}', variables('uniqueName'))), '2022-09-01').primaryKey, '')), createObject('name', 'SemanticMemory:Services:Qdrant:Endpoint', 'value', if(equals(parameters('memoryStore'), 'Qdrant'), format('https://{0}', reference(resourceId('Microsoft.Web/sites', format('app-{0}-qdrant', variables('uniqueName'))), '2022-09-01').defaultHostName), '')), createObject('name', 'SemanticMemory:Services:AzureOpenAIText:Auth', 'value', 'ApiKey'), createObject('name', 'SemanticMemory:Services:AzureOpenAIText:Endpoint', 'value', if(parameters('deployNewAzureOpenAI'), reference(resourceId('Microsoft.CognitiveServices/accounts', format('ai-{0}', variables('uniqueName'))), '2023-05-01').endpoint, parameters('aiEndpoint'))), createObject('name', 'SemanticMemory:Services:AzureOpenAIText:APIKey', 'value', if(parameters('deployNewAzureOpenAI'), listKeys(resourceId('Microsoft.CognitiveServices/accounts', format('ai-{0}', variables('uniqueName'))), '2023-05-01').key1, parameters('aiApiKey'))), createObject('name', 'SemanticMemory:Services:AzureOpenAIText:Deployment', 'value', parameters('completionModel')), createObject('name', 'SemanticMemory:Services:AzureOpenAIEmbedding:Auth', 'value', 'ApiKey'), createObject('name', 'SemanticMemory:Services:AzureOpenAIEmbedding:Endpoint', 'value', if(parameters('deployNewAzureOpenAI'), reference(resourceId('Microsoft.CognitiveServices/accounts', format('ai-{0}', variables('uniqueName'))), '2023-05-01').endpoint, parameters('aiEndpoint'))), createObject('name', 'SemanticMemory:Services:AzureOpenAIEmbedding:APIKey', 'value', if(parameters('deployNewAzureOpenAI'), listKeys(resourceId('Microsoft.CognitiveServices/accounts', format('ai-{0}', variables('uniqueName'))), '2023-05-01').key1, parameters('aiApiKey'))), createObject('name', 'SemanticMemory:Services:AzureOpenAIEmbedding:Deployment', 'value', parameters('embeddingModel')), createObject('name', 'Plugins:0:Name', 'value', 'Klarna Shopping'), createObject('name', 'Plugins:0:ManifestDomain', 'value', 'https://www.klarna.com')), if(parameters('deployWebSearcherPlugin'), createArray(createObject('name', 'Plugins:1:Name', 'value', 'WebSearcher'), createObject('name', 'Plugins:1:ManifestDomain', 'value', format('https://{0}', reference(resourceId('Microsoft.Web/sites', format('function-{0}-websearcher-plugin', variables('uniqueName'))), '2022-09-01').defaultHostName)), createObject('name', 'Plugins:1:Key', 'value', listkeys(format('{0}/host/default/', resourceId('Microsoft.Web/sites', format('function-{0}-websearcher-plugin', variables('uniqueName')))), '2022-09-01').functionKeys.default)), createArray()))]"
      },
      "dependsOn": [
        "[resourceId('Microsoft.Insights/components', format('appins-{0}', variables('uniqueName')))]",
        "[resourceId('Microsoft.Web/sites', format('app-{0}-qdrant', variables('uniqueName')))]",
        "[resourceId('Microsoft.Web/sites', format('app-{0}-webapi', variables('uniqueName')))]",
        "[resourceId('Microsoft.Search/searchServices', format('acs-{0}', variables('uniqueName')))]",
        "[resourceId('Microsoft.DocumentDB/databaseAccounts', toLower(format('cosmos-{0}', variables('uniqueName'))))]",
        "[resourceId('Microsoft.Web/sites', format('function-{0}-websearcher-plugin', variables('uniqueName')))]",
        "[resourceId('Microsoft.CognitiveServices/accounts', format('ai-{0}', variables('uniqueName')))]",
        "[resourceId('Microsoft.CognitiveServices/accounts', format('cog-speech-{0}', variables('uniqueName')))]",
        "[resourceId('Microsoft.Storage/storageAccounts', format('st{0}', variables('rgIdHash')))]"
      ]
    },
    {
      "condition": "[parameters('deployPackages')]",
      "type": "Microsoft.Web/sites/extensions",
      "apiVersion": "2022-09-01",
      "name": "[format('{0}/{1}', format('app-{0}-webapi', variables('uniqueName')), 'MSDeploy')]",
      "kind": "string",
      "properties": {
        "packageUri": "[parameters('webApiPackageUri')]"
      },
      "dependsOn": [
        "[resourceId('Microsoft.Web/sites', format('app-{0}-webapi', variables('uniqueName')))]",
        "[resourceId('Microsoft.Web/sites/config', format('app-{0}-webapi', variables('uniqueName')), 'web')]"
      ]
    },
    {
      "type": "Microsoft.Web/sites",
      "apiVersion": "2022-09-01",
      "name": "[format('app-{0}-memorypipeline', variables('uniqueName'))]",
      "location": "[parameters('location')]",
      "kind": "app",
      "tags": {
        "skweb": "1"
      },
      "properties": {
        "serverFarmId": "[resourceId('Microsoft.Web/serverfarms', format('asp-{0}-webapi', variables('uniqueName')))]",
        "virtualNetworkSubnetId": "[reference(resourceId('Microsoft.Network/virtualNetworks', format('vnet-{0}', variables('uniqueName'))), '2021-05-01').subnets[0].id]",
        "siteConfig": {
          "alwaysOn": true
        }
      },
      "dependsOn": [
        "[resourceId('Microsoft.Web/serverfarms', format('asp-{0}-webapi', variables('uniqueName')))]",
        "[resourceId('Microsoft.Network/virtualNetworks', format('vnet-{0}', variables('uniqueName')))]"
      ]
    },
    {
      "type": "Microsoft.Web/sites/config",
      "apiVersion": "2022-09-01",
      "name": "[format('{0}/{1}', format('app-{0}-memorypipeline', variables('uniqueName')), 'web')]",
      "properties": {
        "alwaysOn": true,
        "detailedErrorLoggingEnabled": true,
        "minTlsVersion": "1.2",
        "netFrameworkVersion": "v6.0",
        "use32BitWorkerProcess": false,
        "vnetName": "webSubnetConnection",
        "vnetRouteAllEnabled": true,
        "appSettings": [
          {
            "name": "SemanticMemory:ContentStorageType",
            "value": "AzureBlobs"
          },
          {
            "name": "SemanticMemory:TextGeneratorType",
            "value": "[parameters('aiService')]"
          },
          {
            "name": "SemanticMemory:ImageOcrType",
            "value": "AzureFormRecognizer"
          },
          {
            "name": "SemanticMemory:DataIngestion:OrchestrationType",
            "value": "Distributed"
          },
          {
            "name": "SemanticMemory:DataIngestion:DistributedOrchestration:QueueType",
            "value": "AzureQueue"
          },
          {
            "name": "SemanticMemory:DataIngestion:EmbeddingGeneratorTypes:0",
            "value": "[parameters('aiService')]"
          },
          {
            "name": "SemanticMemory:DataIngestion:VectorDbTypes:0",
            "value": "[parameters('memoryStore')]"
          },
          {
            "name": "SemanticMemory:Retrieval:VectorDbType",
            "value": "[parameters('memoryStore')]"
          },
          {
            "name": "SemanticMemory:Retrieval:EmbeddingGeneratorType",
            "value": "[parameters('aiService')]"
          },
          {
            "name": "SemanticMemory:Services:AzureBlobs:Auth",
            "value": "ConnectionString"
          },
          {
            "name": "SemanticMemory:Services:AzureBlobs:ConnectionString",
            "value": "[format('DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}', format('st{0}', variables('rgIdHash')), listKeys(resourceId('Microsoft.Storage/storageAccounts', format('st{0}', variables('rgIdHash'))), '2022-09-01').keys[1].value)]"
          },
          {
            "name": "SemanticMemory:Services:AzureBlobs:Container",
            "value": "chatmemory"
          },
          {
            "name": "SemanticMemory:Services:AzureQueue:Auth",
            "value": "ConnectionString"
          },
          {
            "name": "SemanticMemory:Services:AzureQueue:ConnectionString",
            "value": "[format('DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}', format('st{0}', variables('rgIdHash')), listKeys(resourceId('Microsoft.Storage/storageAccounts', format('st{0}', variables('rgIdHash'))), '2022-09-01').keys[1].value)]"
          },
          {
            "name": "SemanticMemory:Services:AzureCognitiveSearch:Auth",
            "value": "ApiKey"
          },
          {
            "name": "SemanticMemory:Services:AzureCognitiveSearch:Endpoint",
            "value": "[if(equals(parameters('memoryStore'), 'AzureCognitiveSearch'), format('https://{0}.search.windows.net', format('acs-{0}', variables('uniqueName'))), '')]"
          },
          {
            "name": "SemanticMemory:Services:AzureCognitiveSearch:APIKey",
            "value": "[if(equals(parameters('memoryStore'), 'AzureCognitiveSearch'), listAdminKeys(resourceId('Microsoft.Search/searchServices', format('acs-{0}', variables('uniqueName'))), '2022-09-01').primaryKey, '')]"
          },
          {
            "name": "SemanticMemory:Services:Qdrant:Endpoint",
            "value": "[if(equals(parameters('memoryStore'), 'Qdrant'), format('https://{0}', reference(resourceId('Microsoft.Web/sites', format('app-{0}-qdrant', variables('uniqueName'))), '2022-09-01').defaultHostName), '')]"
          },
          {
            "name": "SemanticMemory:Services:AzureOpenAIText:Auth",
            "value": "ApiKey"
          },
          {
            "name": "SemanticMemory:Services:AzureOpenAIText:Endpoint",
            "value": "[if(parameters('deployNewAzureOpenAI'), reference(resourceId('Microsoft.CognitiveServices/accounts', format('ai-{0}', variables('uniqueName'))), '2023-05-01').endpoint, parameters('aiEndpoint'))]"
          },
          {
            "name": "SemanticMemory:Services:AzureOpenAIText:APIKey",
            "value": "[if(parameters('deployNewAzureOpenAI'), listKeys(resourceId('Microsoft.CognitiveServices/accounts', format('ai-{0}', variables('uniqueName'))), '2023-05-01').key1, parameters('aiApiKey'))]"
          },
          {
            "name": "SemanticMemory:Services:AzureOpenAIText:Deployment",
            "value": "[parameters('completionModel')]"
          },
          {
            "name": "SemanticMemory:Services:AzureOpenAIEmbedding:Auth",
            "value": "ApiKey"
          },
          {
            "name": "SemanticMemory:Services:AzureOpenAIEmbedding:Endpoint",
            "value": "[if(parameters('deployNewAzureOpenAI'), reference(resourceId('Microsoft.CognitiveServices/accounts', format('ai-{0}', variables('uniqueName'))), '2023-05-01').endpoint, parameters('aiEndpoint'))]"
          },
          {
            "name": "SemanticMemory:Services:AzureOpenAIEmbedding:APIKey",
            "value": "[if(parameters('deployNewAzureOpenAI'), listKeys(resourceId('Microsoft.CognitiveServices/accounts', format('ai-{0}', variables('uniqueName'))), '2023-05-01').key1, parameters('aiApiKey'))]"
          },
          {
            "name": "SemanticMemory:Services:AzureOpenAIEmbedding:Deployment",
            "value": "[parameters('embeddingModel')]"
          },
          {
            "name": "SemanticMemory:Services:AzureFormRecognizer:Auth",
            "value": "ApiKey"
          },
          {
            "name": "SemanticMemory:Services:AzureFormRecognizer:Endpoint",
            "value": "[reference(resourceId('Microsoft.CognitiveServices/accounts', format('cog-ocr-{0}', variables('uniqueName'))), '2022-12-01').endpoint]"
          },
          {
            "name": "SemanticMemory:Services:AzureFormRecognizer:APIKey",
            "value": "[listKeys(resourceId('Microsoft.CognitiveServices/accounts', format('cog-ocr-{0}', variables('uniqueName'))), '2022-12-01').key1]"
          },
          {
            "name": "Logging:LogLevel:Default",
            "value": "Information"
          },
          {
            "name": "Logging:LogLevel:AspNetCore",
            "value": "Warning"
          },
          {
            "name": "Logging:ApplicationInsights:LogLevel:Default",
            "value": "Warning"
          },
          {
            "name": "ApplicationInsights:ConnectionString",
            "value": "[reference(resourceId('Microsoft.Insights/components', format('appins-{0}', variables('uniqueName'))), '2020-02-02').ConnectionString]"
          }
        ]
      },
      "dependsOn": [
        "[resourceId('Microsoft.Insights/components', format('appins-{0}', variables('uniqueName')))]",
        "[resourceId('Microsoft.Web/sites', format('app-{0}-memorypipeline', variables('uniqueName')))]",
        "[resourceId('Microsoft.Web/sites', format('app-{0}-qdrant', variables('uniqueName')))]",
        "[resourceId('Microsoft.Search/searchServices', format('acs-{0}', variables('uniqueName')))]",
        "[resourceId('Microsoft.CognitiveServices/accounts', format('cog-ocr-{0}', variables('uniqueName')))]",
        "[resourceId('Microsoft.CognitiveServices/accounts', format('ai-{0}', variables('uniqueName')))]",
        "[resourceId('Microsoft.Storage/storageAccounts', format('st{0}', variables('rgIdHash')))]",
        "[resourceId('Microsoft.Web/sites/virtualNetworkConnections', format('app-{0}-webapi', variables('uniqueName')), 'webSubnetConnection')]"
      ]
    },
    {
      "condition": "[parameters('deployPackages')]",
      "type": "Microsoft.Web/sites/extensions",
      "apiVersion": "2022-09-01",
      "name": "[format('{0}/{1}', format('app-{0}-memorypipeline', variables('uniqueName')), 'MSDeploy')]",
      "kind": "string",
      "properties": {
        "packageUri": "[parameters('memoryPipelinePackageUri')]"
      },
      "dependsOn": [
        "[resourceId('Microsoft.Web/sites', format('app-{0}-memorypipeline', variables('uniqueName')))]",
        "[resourceId('Microsoft.Web/sites/config', format('app-{0}-memorypipeline', variables('uniqueName')), 'web')]"
      ]
    },
    {
      "condition": "[parameters('deployWebSearcherPlugin')]",
      "type": "Microsoft.Web/sites",
      "apiVersion": "2022-09-01",
      "name": "[format('function-{0}-websearcher-plugin', variables('uniqueName'))]",
      "location": "[parameters('location')]",
      "kind": "functionapp",
      "tags": {
        "skweb": "1"
      },
      "properties": {
        "serverFarmId": "[resourceId('Microsoft.Web/serverfarms', format('asp-{0}-webapi', variables('uniqueName')))]",
        "httpsOnly": true,
        "siteConfig": {
          "alwaysOn": true
        }
      },
      "dependsOn": [
        "[resourceId('Microsoft.Web/serverfarms', format('asp-{0}-webapi', variables('uniqueName')))]"
      ]
    },
    {
      "condition": "[parameters('deployWebSearcherPlugin')]",
      "type": "Microsoft.Web/sites/config",
      "apiVersion": "2022-09-01",
      "name": "[format('{0}/{1}', format('function-{0}-websearcher-plugin', variables('uniqueName')), 'web')]",
      "properties": {
        "minTlsVersion": "1.2",
        "appSettings": [
          {
            "name": "FUNCTIONS_EXTENSION_VERSION",
            "value": "~4"
          },
          {
            "name": "FUNCTIONS_WORKER_RUNTIME",
            "value": "dotnet-isolated"
          },
          {
            "name": "AzureWebJobsStorage",
            "value": "[format('DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}', format('st{0}', variables('rgIdHash')), listKeys(resourceId('Microsoft.Storage/storageAccounts', format('st{0}', variables('rgIdHash'))), '2022-09-01').keys[1].value)]"
          },
          {
            "name": "APPINSIGHTS_INSTRUMENTATIONKEY",
            "value": "[reference(resourceId('Microsoft.Insights/components', format('appins-{0}', variables('uniqueName'))), '2020-02-02').InstrumentationKey]"
          },
          {
            "name": "PluginConfig:BingApiKey",
            "value": "[if(parameters('deployWebSearcherPlugin'), listKeys(resourceId('Microsoft.Bing/accounts', format('bing-search-{0}', variables('uniqueName'))), '2020-06-10').key1, '')]"
          }
        ]
      },
      "dependsOn": [
        "[resourceId('Microsoft.Insights/components', format('appins-{0}', variables('uniqueName')))]",
        "[resourceId('Microsoft.Bing/accounts', format('bing-search-{0}', variables('uniqueName')))]",
        "[resourceId('Microsoft.Web/sites', format('function-{0}-websearcher-plugin', variables('uniqueName')))]",
        "[resourceId('Microsoft.Storage/storageAccounts', format('st{0}', variables('rgIdHash')))]"
      ]
    },
    {
      "condition": "[and(parameters('deployPackages'), parameters('deployWebSearcherPlugin'))]",
      "type": "Microsoft.Web/sites/extensions",
      "apiVersion": "2022-09-01",
      "name": "[format('{0}/{1}', format('function-{0}-websearcher-plugin', variables('uniqueName')), 'MSDeploy')]",
      "kind": "string",
      "properties": {
        "packageUri": "[parameters('webSearcherPackageUri')]"
      },
      "dependsOn": [
        "[resourceId('Microsoft.Web/sites', format('function-{0}-websearcher-plugin', variables('uniqueName')))]",
        "[resourceId('Microsoft.Web/sites/config', format('function-{0}-websearcher-plugin', variables('uniqueName')), 'web')]"
      ]
    },
    {
      "type": "Microsoft.Insights/components",
      "apiVersion": "2020-02-02",
      "name": "[format('appins-{0}', variables('uniqueName'))]",
      "location": "[parameters('location')]",
      "kind": "string",
      "tags": {
        "displayName": "AppInsight"
      },
      "properties": {
        "Application_Type": "web",
        "WorkspaceResourceId": "[resourceId('Microsoft.OperationalInsights/workspaces', format('la-{0}', variables('uniqueName')))]"
      },
      "dependsOn": [
        "[resourceId('Microsoft.OperationalInsights/workspaces', format('la-{0}', variables('uniqueName')))]"
      ]
    },
    {
      "type": "Microsoft.Web/sites/siteextensions",
      "apiVersion": "2022-09-01",
      "name": "[format('{0}/{1}', format('app-{0}-webapi', variables('uniqueName')), 'Microsoft.ApplicationInsights.AzureWebSites')]",
      "dependsOn": [
        "[resourceId('Microsoft.Web/sites', format('app-{0}-webapi', variables('uniqueName')))]",
        "[resourceId('Microsoft.Web/sites/extensions', format('app-{0}-webapi', variables('uniqueName')), 'MSDeploy')]"
      ]
    },
    {
      "type": "Microsoft.Web/sites/siteextensions",
      "apiVersion": "2022-09-01",
      "name": "[format('{0}/{1}', format('app-{0}-memorypipeline', variables('uniqueName')), 'Microsoft.ApplicationInsights.AzureWebSites')]",
      "dependsOn": [
        "[resourceId('Microsoft.Web/sites', format('app-{0}-memorypipeline', variables('uniqueName')))]",
        "[resourceId('Microsoft.Web/sites/extensions', format('app-{0}-memorypipeline', variables('uniqueName')), 'MSDeploy')]"
      ]
    },
    {
      "condition": "[parameters('deployWebSearcherPlugin')]",
      "type": "Microsoft.Web/sites/siteextensions",
      "apiVersion": "2022-09-01",
      "name": "[format('{0}/{1}', format('function-{0}-websearcher-plugin', variables('uniqueName')), 'Microsoft.ApplicationInsights.AzureWebSites')]",
      "dependsOn": [
        "[resourceId('Microsoft.Web/sites/extensions', format('function-{0}-websearcher-plugin', variables('uniqueName')), 'MSDeploy')]",
        "[resourceId('Microsoft.Web/sites', format('function-{0}-websearcher-plugin', variables('uniqueName')))]"
      ]
    },
    {
      "type": "Microsoft.OperationalInsights/workspaces",
      "apiVersion": "2022-10-01",
      "name": "[format('la-{0}', variables('uniqueName'))]",
      "location": "[parameters('location')]",
      "tags": {
        "displayName": "Log Analytics"
      },
      "properties": {
        "sku": {
          "name": "PerGB2018"
        },
        "retentionInDays": 90,
        "features": {
          "searchVersion": 1,
          "legacy": 0,
          "enableLogAccessUsingOnlyResourcePermissions": true
        }
      }
    },
    {
      "type": "Microsoft.Storage/storageAccounts",
      "apiVersion": "2022-09-01",
      "name": "[format('st{0}', variables('rgIdHash'))]",
      "location": "[parameters('location')]",
      "kind": "StorageV2",
      "sku": {
        "name": "Standard_LRS"
      },
      "properties": {
        "supportsHttpsTrafficOnly": true,
        "allowBlobPublicAccess": false
      }
    },
    {
      "condition": "[equals(parameters('memoryStore'), 'Qdrant')]",
      "type": "Microsoft.Web/serverfarms",
      "apiVersion": "2022-03-01",
      "name": "[format('asp-{0}-qdrant', variables('uniqueName'))]",
      "location": "[parameters('location')]",
      "kind": "linux",
      "sku": {
        "name": "P1v3"
      },
      "properties": {
        "reserved": true
      }
    },
    {
      "condition": "[equals(parameters('memoryStore'), 'Qdrant')]",
      "type": "Microsoft.Web/sites",
      "apiVersion": "2022-09-01",
      "name": "[format('app-{0}-qdrant', variables('uniqueName'))]",
      "location": "[parameters('location')]",
      "kind": "app,linux,container",
      "properties": {
        "serverFarmId": "[resourceId('Microsoft.Web/serverfarms', format('asp-{0}-qdrant', variables('uniqueName')))]",
        "httpsOnly": true,
        "reserved": true,
        "clientCertMode": "Required",
        "virtualNetworkSubnetId": "[reference(resourceId('Microsoft.Network/virtualNetworks', format('vnet-{0}', variables('uniqueName'))), '2021-05-01').subnets[1].id]",
        "siteConfig": {
          "numberOfWorkers": 1,
          "linuxFxVersion": "DOCKER|qdrant/qdrant:latest",
          "alwaysOn": true,
          "vnetRouteAllEnabled": true,
          "ipSecurityRestrictions": [
            {
              "vnetSubnetResourceId": "[reference(resourceId('Microsoft.Network/virtualNetworks', format('vnet-{0}', variables('uniqueName'))), '2021-05-01').subnets[0].id]",
              "action": "Allow",
              "priority": 300,
              "name": "Allow front vnet"
            },
            {
              "ipAddress": "Any",
              "action": "Deny",
              "priority": 2147483647,
              "name": "Deny all"
            }
          ],
          "azureStorageAccounts": {
            "aciqdrantshare": {
              "type": "AzureFiles",
              "accountName": "[if(equals(parameters('memoryStore'), 'Qdrant'), format('st{0}', variables('rgIdHash')), 'notdeployed')]",
              "shareName": "[variables('storageFileShareName')]",
              "mountPath": "/qdrant/storage",
              "accessKey": "[if(equals(parameters('memoryStore'), 'Qdrant'), listKeys(resourceId('Microsoft.Storage/storageAccounts', format('st{0}', variables('rgIdHash'))), '2022-09-01').keys[0].value, '')]"
            }
          }
        }
      },
      "dependsOn": [
        "[resourceId('Microsoft.Web/serverfarms', format('asp-{0}-qdrant', variables('uniqueName')))]",
        "[resourceId('Microsoft.Storage/storageAccounts', format('st{0}', variables('rgIdHash')))]",
        "[resourceId('Microsoft.Network/virtualNetworks', format('vnet-{0}', variables('uniqueName')))]"
      ]
    },
    {
      "condition": "[equals(parameters('memoryStore'), 'AzureCognitiveSearch')]",
      "type": "Microsoft.Search/searchServices",
      "apiVersion": "2022-09-01",
      "name": "[format('acs-{0}', variables('uniqueName'))]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "basic"
      },
      "properties": {
        "replicaCount": 1,
        "partitionCount": 1
      }
    },
    {
      "type": "Microsoft.Network/virtualNetworks",
      "apiVersion": "2021-05-01",
      "name": "[format('vnet-{0}', variables('uniqueName'))]",
      "location": "[parameters('location')]",
      "properties": {
        "addressSpace": {
          "addressPrefixes": [
            "10.0.0.0/16"
          ]
        },
        "subnets": [
          {
            "name": "webSubnet",
            "properties": {
              "addressPrefix": "10.0.1.0/24",
              "networkSecurityGroup": {
                "id": "[resourceId('Microsoft.Network/networkSecurityGroups', format('nsg-{0}-webapi', variables('uniqueName')))]"
              },
              "serviceEndpoints": [
                {
                  "service": "Microsoft.Web",
                  "locations": [
                    "*"
                  ]
                }
              ],
              "delegations": [
                {
                  "name": "delegation",
                  "properties": {
                    "serviceName": "Microsoft.Web/serverfarms"
                  }
                }
              ],
              "privateEndpointNetworkPolicies": "Disabled",
              "privateLinkServiceNetworkPolicies": "Enabled"
            }
          },
          {
            "name": "qdrantSubnet",
            "properties": {
              "addressPrefix": "10.0.2.0/24",
              "networkSecurityGroup": {
                "id": "[resourceId('Microsoft.Network/networkSecurityGroups', format('nsg-{0}-qdrant', variables('uniqueName')))]"
              },
              "serviceEndpoints": [
                {
                  "service": "Microsoft.Web",
                  "locations": [
                    "*"
                  ]
                }
              ],
              "delegations": [
                {
                  "name": "delegation",
                  "properties": {
                    "serviceName": "Microsoft.Web/serverfarms"
                  }
                }
              ],
              "privateEndpointNetworkPolicies": "Disabled",
              "privateLinkServiceNetworkPolicies": "Enabled"
            }
          },
          {
            "name": "postgresSubnet",
            "properties": {
              "addressPrefix": "10.0.3.0/24",
              "serviceEndpoints": [],
              "delegations": [],
              "privateEndpointNetworkPolicies": "Disabled",
              "privateLinkServiceNetworkPolicies": "Enabled"
            }
          }
        ]
      },
      "dependsOn": [
        "[resourceId('Microsoft.Network/networkSecurityGroups', format('nsg-{0}-qdrant', variables('uniqueName')))]",
        "[resourceId('Microsoft.Network/networkSecurityGroups', format('nsg-{0}-webapi', variables('uniqueName')))]"
      ]
    },
    {
      "type": "Microsoft.Network/networkSecurityGroups",
      "apiVersion": "2022-11-01",
      "name": "[format('nsg-{0}-webapi', variables('uniqueName'))]",
      "location": "[parameters('location')]",
      "properties": {
        "securityRules": [
          {
            "name": "AllowAnyHTTPSInbound",
            "properties": {
              "protocol": "TCP",
              "sourcePortRange": "*",
              "destinationPortRange": "443",
              "sourceAddressPrefix": "*",
              "destinationAddressPrefix": "*",
              "access": "Allow",
              "priority": 100,
              "direction": "Inbound"
            }
          }
        ]
      }
    },
    {
      "type": "Microsoft.Network/networkSecurityGroups",
      "apiVersion": "2022-11-01",
      "name": "[format('nsg-{0}-qdrant', variables('uniqueName'))]",
      "location": "[parameters('location')]",
      "properties": {
        "securityRules": []
      }
    },
    {
      "type": "Microsoft.Web/sites/virtualNetworkConnections",
      "apiVersion": "2022-09-01",
      "name": "[format('{0}/{1}', format('app-{0}-webapi', variables('uniqueName')), 'webSubnetConnection')]",
      "properties": {
        "vnetResourceId": "[reference(resourceId('Microsoft.Network/virtualNetworks', format('vnet-{0}', variables('uniqueName'))), '2021-05-01').subnets[0].id]",
        "isSwift": true
      },
      "dependsOn": [
        "[resourceId('Microsoft.Web/sites', format('app-{0}-webapi', variables('uniqueName')))]",
        "[resourceId('Microsoft.Network/virtualNetworks', format('vnet-{0}', variables('uniqueName')))]"
      ]
    },
    {
      "condition": "[equals(parameters('memoryStore'), 'Qdrant')]",
      "type": "Microsoft.Web/sites/virtualNetworkConnections",
      "apiVersion": "2022-09-01",
      "name": "[format('{0}/{1}', format('app-{0}-qdrant', variables('uniqueName')), 'qdrantSubnetConnection')]",
      "properties": {
        "vnetResourceId": "[reference(resourceId('Microsoft.Network/virtualNetworks', format('vnet-{0}', variables('uniqueName'))), '2021-05-01').subnets[1].id]",
        "isSwift": true
      },
      "dependsOn": [
        "[resourceId('Microsoft.Web/sites', format('app-{0}-qdrant', variables('uniqueName')))]",
        "[resourceId('Microsoft.Network/virtualNetworks', format('vnet-{0}', variables('uniqueName')))]"
      ]
    },
    {
      "condition": "[parameters('deployCosmosDB')]",
      "type": "Microsoft.DocumentDB/databaseAccounts",
      "apiVersion": "2023-04-15",
      "name": "[toLower(format('cosmos-{0}', variables('uniqueName')))]",
      "location": "[parameters('location')]",
      "kind": "GlobalDocumentDB",
      "properties": {
        "consistencyPolicy": {
          "defaultConsistencyLevel": "Session"
        },
        "locations": [
          {
            "locationName": "[parameters('location')]",
            "failoverPriority": 0,
            "isZoneRedundant": false
          }
        ],
        "databaseAccountOfferType": "Standard"
      }
    },
    {
      "condition": "[parameters('deployCosmosDB')]",
      "type": "Microsoft.DocumentDB/databaseAccounts/sqlDatabases",
      "apiVersion": "2023-04-15",
      "name": "[format('{0}/{1}', toLower(format('cosmos-{0}', variables('uniqueName'))), 'CopilotChat')]",
      "properties": {
        "resource": {
          "id": "CopilotChat"
        }
      },
      "dependsOn": [
        "[resourceId('Microsoft.DocumentDB/databaseAccounts', toLower(format('cosmos-{0}', variables('uniqueName'))))]"
      ]
    },
    {
      "condition": "[parameters('deployCosmosDB')]",
      "type": "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers",
      "apiVersion": "2023-04-15",
      "name": "[format('{0}/{1}/{2}', toLower(format('cosmos-{0}', variables('uniqueName'))), 'CopilotChat', 'chatmessages')]",
      "properties": {
        "resource": {
          "id": "chatmessages",
          "indexingPolicy": {
            "indexingMode": "consistent",
            "automatic": true,
            "includedPaths": [
              {
                "path": "/*"
              }
            ],
            "excludedPaths": [
              {
                "path": "/\"_etag\"/?"
              }
            ]
          },
          "partitionKey": {
            "paths": [
              "/chatId"
            ],
            "kind": "Hash",
            "version": 2
          }
        }
      },
      "dependsOn": [
        "[resourceId('Microsoft.DocumentDB/databaseAccounts/sqlDatabases', toLower(format('cosmos-{0}', variables('uniqueName'))), 'CopilotChat')]"
      ]
    },
    {
      "condition": "[parameters('deployCosmosDB')]",
      "type": "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers",
      "apiVersion": "2023-04-15",
      "name": "[format('{0}/{1}/{2}', toLower(format('cosmos-{0}', variables('uniqueName'))), 'CopilotChat', 'chatsessions')]",
      "properties": {
        "resource": {
          "id": "chatsessions",
          "indexingPolicy": {
            "indexingMode": "consistent",
            "automatic": true,
            "includedPaths": [
              {
                "path": "/*"
              }
            ],
            "excludedPaths": [
              {
                "path": "/\"_etag\"/?"
              }
            ]
          },
          "partitionKey": {
            "paths": [
              "/id"
            ],
            "kind": "Hash",
            "version": 2
          }
        }
      },
      "dependsOn": [
        "[resourceId('Microsoft.DocumentDB/databaseAccounts/sqlDatabases', toLower(format('cosmos-{0}', variables('uniqueName'))), 'CopilotChat')]"
      ]
    },
    {
      "condition": "[parameters('deployCosmosDB')]",
      "type": "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers",
      "apiVersion": "2023-04-15",
      "name": "[format('{0}/{1}/{2}', toLower(format('cosmos-{0}', variables('uniqueName'))), 'CopilotChat', 'chatparticipants')]",
      "properties": {
        "resource": {
          "id": "chatparticipants",
          "indexingPolicy": {
            "indexingMode": "consistent",
            "automatic": true,
            "includedPaths": [
              {
                "path": "/*"
              }
            ],
            "excludedPaths": [
              {
                "path": "/\"_etag\"/?"
              }
            ]
          },
          "partitionKey": {
            "paths": [
              "/userId"
            ],
            "kind": "Hash",
            "version": 2
          }
        }
      },
      "dependsOn": [
        "[resourceId('Microsoft.DocumentDB/databaseAccounts/sqlDatabases', toLower(format('cosmos-{0}', variables('uniqueName'))), 'CopilotChat')]"
      ]
    },
    {
      "condition": "[parameters('deployCosmosDB')]",
      "type": "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers",
      "apiVersion": "2023-04-15",
      "name": "[format('{0}/{1}/{2}', toLower(format('cosmos-{0}', variables('uniqueName'))), 'CopilotChat', 'chatmemorysources')]",
      "properties": {
        "resource": {
          "id": "chatmemorysources",
          "indexingPolicy": {
            "indexingMode": "consistent",
            "automatic": true,
            "includedPaths": [
              {
                "path": "/*"
              }
            ],
            "excludedPaths": [
              {
                "path": "/\"_etag\"/?"
              }
            ]
          },
          "partitionKey": {
            "paths": [
              "/chatId"
            ],
            "kind": "Hash",
            "version": 2
          }
        }
      },
      "dependsOn": [
        "[resourceId('Microsoft.DocumentDB/databaseAccounts/sqlDatabases', toLower(format('cosmos-{0}', variables('uniqueName'))), 'CopilotChat')]"
      ]
    },
    {
      "condition": "[parameters('deploySpeechServices')]",
      "type": "Microsoft.CognitiveServices/accounts",
      "apiVersion": "2022-12-01",
      "name": "[format('cog-speech-{0}', variables('uniqueName'))]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "S0"
      },
      "kind": "SpeechServices",
      "identity": {
        "type": "None"
      },
      "properties": {
        "customSubDomainName": "[format('cog-speech-{0}', variables('uniqueName'))]",
        "networkAcls": {
          "defaultAction": "Allow"
        },
        "publicNetworkAccess": "Enabled"
      }
    },
    {
      "type": "Microsoft.CognitiveServices/accounts",
      "apiVersion": "2022-12-01",
      "name": "[format('cog-ocr-{0}', variables('uniqueName'))]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "S0"
      },
      "kind": "FormRecognizer",
      "identity": {
        "type": "None"
      },
      "properties": {
        "customSubDomainName": "[format('cog-ocr-{0}', variables('uniqueName'))]",
        "networkAcls": {
          "defaultAction": "Allow"
        },
        "publicNetworkAccess": "Enabled"
      }
    },
    {
      "condition": "[parameters('deployWebSearcherPlugin')]",
      "type": "Microsoft.Bing/accounts",
      "apiVersion": "2020-06-10",
      "name": "[format('bing-search-{0}', variables('uniqueName'))]",
      "location": "global",
      "sku": {
        "name": "S1"
      },
      "kind": "Bing.Search.v7"
    }
  ],
  "outputs": {
    "webapiUrl": {
      "type": "string",
      "value": "[reference(resourceId('Microsoft.Web/sites', format('app-{0}-webapi', variables('uniqueName'))), '2022-09-01').defaultHostName]"
    },
    "webapiName": {
      "type": "string",
      "value": "[format('app-{0}-webapi', variables('uniqueName'))]"
    },
    "memoryPipelineName": {
      "type": "string",
      "value": "[format('app-{0}-memorypipeline', variables('uniqueName'))]"
    },
    "pluginNames": {
      "type": "array",
      "value": "[concat(createArray(), if(parameters('deployWebSearcherPlugin'), createArray(format('function-{0}-websearcher-plugin', variables('uniqueName'))), createArray()))]"
    }
  }
}
