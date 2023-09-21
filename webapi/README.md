# Chat Copilot backend web API service

This directory contains the source code for Chat Copilot's backend web API service. The front end web application component can be found in the [webapp/](../webapp/) directory.

## Running the Chat Copilot sample

To configure and run either the full Chat Copilot application or only the backend API, please view the [main instructions](../README.md#instructions).

# (Under Development)

The following material is under development and may not be complete or accurate.

## Visual Studio Code

1. build (CopilotChatWebApi)
2. run (CopilotChatWebApi)
3. [optional] watch (CopilotChatWebApi)

## Visual Studio (2022 or newer)

1. Open the solution file in Visual Studio 2022 or newer (`CopilotChat.sln`).
2. In Solution Explorer, right-click on `CopilotChatWebApi` and select `Set as Startup Project`.
3. Start debugging by pressing `F5` or selecting the menu item `Debug`->`Start Debugging`.

4. **(Optional)** To enable support for uploading image file formats such as png, jpg and tiff, there are two options for `SemanticMemory:ImageOcrType` section of `./appsettings.json`, the Tesseract open source library and Azure Form Recognizer.
   - **Tesseract** we have included the [Tesseract](https://www.nuget.org/packages/Tesseract) nuget package.
     - You will need to obtain one or more [tessdata language data files](https://github.com/tesseract-ocr/tessdata) such as `eng.traineddata` and add them to your `./data` directory or the location specified in the `SemanticMemory:Services:Tesseract:FilePath` location in `./appsettings.json`.
     - Set the `Copy to Output Directory` value to `Copy if newer`.
   - **Azure Form Recognizer** we have included the [Azure.AI.FormRecognizer](https://www.nuget.org/packages/Azure.AI.FormRecognizer) nuget package.
     - You will need to obtain an [Azure Form Recognizer](https://azure.microsoft.com/en-us/services/form-recognizer/) resource and add the `SemanticMemory:Services:AzureFormRecognizer:Endpoint` and `SemanticMemory:Services:AzureFormRecognizer:Key` values to the `./appsettings.json` file.

## Running [Memory Service](https://github.com/microsoft/semantic-memory)

The memory service handles the creation and querying of semantic memory, including cognitive memory and documents.

### InProcess Processing (Default)

Running the memory creation pipeline in the webapi process. This also means the memory creation is synchronous.

No additional configuration is needed.

> You can choose either **Volatile** or **TextFile** as the SimpleVectorDb implementation.

### Distributed Processing

Running the memory creation pipeline steps in different processes. This means the memory creation is asynchronous. This allows better scalability if you have many chat sessions active at the same time or you have big documents that require minutes to process.

1. In [./webapi/appsettings.json](./appsettings.json), set `SemanticMemory:DataIngestion:OrchestrationType` to `Distributed`.
2. In [../memorypipeline/appsettings.json](../memorypipeline/appsettings.json), set `SemanticMemory:DataIngestion:OrchestrationType` to `Distributed`.
3. Make sure the following settings in the [./webapi/appsettings.json](./appsettings.json) and [../memorypipeline/appsettings.json](../memorypipeline/appsettings.json) respectively point to the same locations on your machine so that both processes can access the data:
   - `SemanticMemory:Services:SimpleFileStorage:Directory`
   - `SemanticMemory:Services:SimpleQueues:Directory`
   - `SemanticMemory:Services:SimpleVectorDb:Directory`
     > Do not configure SimpleVectorDb to use Volatile. Volatile storage cannot be shared across processes.
4. You need to run both the [webapi](./README.md) and the [memorypipeline](../memorypipeline/README.md).

### (Optional) Use hosted resources: [Azure Storage Account](https://learn.microsoft.com/en-us/azure/storage/common/storage-account-overview), [Azure Cognitive Search](https://learn.microsoft.com/en-us/azure/search/search-what-is-azure-search)

1. In [./webapi/appsettings.json](./appsettings.json) and [../memorypipeline/appsettings.json](../memorypipeline/appsettings.json), set `SemanticMemory:ContentStorageType` to `AzureBlobs`.
2. In [./webapi/appsettings.json](./appsettings.json) and [../memorypipeline/appsettings.json](../memorypipeline/appsettings.json), set `SemanticMemory:DataIngestion:DistributedOrchestration:QueueType` to `AzureQueue`.
3. In [./webapi/appsettings.json](./appsettings.json) and [../memorypipeline/appsettings.json](../memorypipeline/appsettings.json), set `SemanticMemory:DataIngestion:VectorDbTypes:0` to `AzureCognitiveSearch`.
4. In [./webapi/appsettings.json](./appsettings.json) and [../memorypipeline/appsettings.json](../memorypipeline/appsettings.json), set `SemanticMemory:Retrieval:VectorDbType` to `AzureCognitiveSearch`.
5. Run the following to set up the authentication to the resources:

   ```bash
   dotnet user-secrets set SemanticMemory:Services:AzureBlobs:Auth ConnectionString
   dotnet user-secrets set SemanticMemory:Services:AzureBlobs:ConnectionString [your secret]
   dotnet user-secrets set SemanticMemory:Services:AzureQueue:Auth ConnectionString   # Only needed when running distributed processing
   dotnet user-secrets set SemanticMemory:Services:AzureQueue:ConnectionString [your secret]   # Only needed when running distributed processing
   dotnet user-secrets set SemanticMemory:Services:AzureCognitiveSearch:Endpoint [your secret]
   dotnet user-secrets set SemanticMemory:Services:AzureCognitiveSearch:APIKey [your secret]
   ```

6. For more information and other options, please refer to the [memorypipeline](../memorypipeline/README.md).

## Enabling Sequential Planner

If you want to use SequentialPlanner (multi-step) instead ActionPlanner (single-step), we recommend using `gpt-4` or `gpt-3.5-turbo` as the planner model. **SequentialPlanner works best with `gpt-4`.** Using `gpt-3.5-turbo` will require using a relevancy filter.

To enable sequential planner,

1. In [./webapi/appsettings.json](appsettings.json), set `"Type": "Sequential"` under the `Planner` section.
1. Then, set your preferred Planner model (`gpt-4` or `gpt-3.5-turbo`) under the `AIService` configuration section.
1. If using `gpt-4`, no other changes are required.
1. If using `gpt-3.5-turbo`: change [CopilotChatPlanner.cs](Skills/ChatSkills/CopilotChatPlanner.cs) to initialize SequentialPlanner with a RelevancyThreshold\*.
   - Add `using` statement to top of file:
     ```
     using Microsoft.SemanticKernel.Planning.Sequential;
     ```
   - The `CreatePlanAsync` method should return the following line if `this._plannerOptions?.Type == "Sequential"` is true:
     ```
     return new SequentialPlanner(this.Kernel, new SequentialPlannerConfig { RelevancyThreshold = 0.75 }).CreatePlanAsync(goal);
     ```
     \* The `RelevancyThreshold` is a number from 0 to 1 that represents how similar a goal is to a function's name/description/inputs. You want to tune that value when using SequentialPlanner to help keep things scoped while not missing on on things that are relevant or including too many things that really aren't. `0.75` is an arbitrary threshold and we recommend developers play around with this number to see what best fits their scenarios.
1. Restart the `webapi` - Copilot Chat should be now running locally with SequentialPlanner.

## (Optional) Enabling Cosmos Chat Store.

[Azure Cosmos DB](https://learn.microsoft.com/en-us/azure/cosmos-db/introduction) can be used as a persistent chat store for Chat Copilot. Chat stores are used for storing chat sessions, participants, and messages.

### Prerequisites

#### 1. Containers and PartitionKeys

In an effort to optimize performance, each container must be created with a specific partition key:
| Store | ContainerName | PartitionKey |
| ----- | ------------- | ------------ |
| Chat Sessions | chatsessions | /id (default) |
| Chat Messages | chatmessages | /chatId |
| Chat Memory Sources | chatmemorysources | /chatId |
| Chat Partipants | chatparticipants | /userId |

> For existing customers using CosmosDB before [Release 0.3](https://github.com/microsoft/chat-copilot/releases/tag/0.3), our recommendation is to remove the existing Cosmos DB containers and redeploy to realize the performance update related to the partition schema. To preserve existing chats, containers can be migrated as described [here](https://learn.microsoft.com/en-us/azure/cosmos-db/intra-account-container-copy#copy-a-container).

## (Optional) Enabling the Qdrant Memory Store

By default, the service uses an in-memory volatile memory store that, when the service stops or restarts, forgets all memories.
[Qdrant](https://github.com/qdrant/qdrant) is a persistent scalable vector search engine that can be deployed locally in a container or [at-scale in the cloud](https://github.com/Azure-Samples/qdrant-azure).

To enable the Qdrant memory store, you must first deploy Qdrant locally and then configure the Copilot Chat API service to use it.

### 1. Configure your environment

Before you get started, make sure you have the following additional requirements in place:

- [Docker Desktop](https://www.docker.com/products/docker-desktop) for hosting the [Qdrant](https://github.com/qdrant/qdrant) vector search engine.

### 2. Deploy Qdrant VectorDB locally

1. Open a terminal and use Docker to pull down the container image.

   ```bash
   docker pull qdrant/qdrant
   ```

2. Change directory to this repo and create a `./data/qdrant` directory to use as persistent storage.
   Then start the Qdrant container on port `6333` using the `./data/qdrant` folder as the persistent storage location.

   ```bash
   mkdir ./data/qdrant
   docker run --name copilotchat -p 6333:6333 -v "$(pwd)/data/qdrant:/qdrant/storage" qdrant/qdrant
   ```

   > To stop the container, in another terminal window run `docker container stop copilotchat; docker container rm copilotchat;`.

## (Optional) Enabling the Azure Cognitive Search Memory Store

Azure Cognitive Search can be used as a persistent memory store for Copilot Chat.
The service uses its [vector search](https://learn.microsoft.com/en-us/azure/search/vector-search-overview) capabilities.

## (Optional) Enable Application Insights telemetry

Enabling telemetry on CopilotChatApi allows you to capture data about requests to and from the API, allowing you to monitor the deployment and monitor how the application is being used.

To use Application Insights, first create an instance in your Azure subscription that you can use for this purpose.

On the resource overview page, in the top right use the copy button to copy the Connection String and paste this into the `APPLICATIONINSIGHTS_CONNECTION_STRING` setting as either a appsettings value, or add it as a secret.

In addition to this there are some custom events that can inform you how users are using the service such as `SkillFunction`.

To access these custom events the suggested method is to use Azure Data Explorer (ADX). To access data from Application Insights in ADX, create a new dashboard and add a new Data Source (use the ellipsis dropdown in the top right).

In the Cluster URI use the following link: `https://ade.applicationinsights.io/subscriptions/<Your subscription Id>`. The subscription id is shown on the resource page for your Applications Insights instance. You can then select the Database for the Application Insights resource.

For more info see [Query data in Azure Monitor using Azure Data Explorer](https://learn.microsoft.com/en-us/azure/data-explorer/query-monitor-data).

CopilotChat specific events are in a table called `customEvents`.

For example to see the most recent 100 skill function invocations:

```kql
customEvents
| where timestamp between (_startTime .. _endTime)
| where name == "SkillFunction"
| extend skill = tostring(customDimensions.skillName)
| extend function = tostring(customDimensions.functionName)
| extend success = tobool(customDimensions.success)
| extend userId = tostring(customDimensions.userId)
| extend environment = tostring(customDimensions.AspNetCoreEnvironment)
| extend skillFunction = strcat(skill, '/', function)
| project timestamp, skillFunction, success, userId, environment
| order by timestamp desc
| limit 100
```

Or to report the success rate of skill functions against environments, you can first add a parameter to the dashboard to filter the environment.

You can use this query to show the environments available by adding the `Source` as this `Query`:

```kql
customEvents
| where timestamp between (['_startTime'] .. ['_endTime']) // Time range filtering
| extend environment = tostring(customDimensions.AspNetCoreEnvironment)
| distinct environment
```

Name the variable `_environment`, select `Multiple Selection` and tick `Add empty "Select all" value`. Finally `Select all` as the `Default value`.

You can then query the success rate with this query:

```kql
customEvents
| where timestamp between (_startTime .. _endTime)
| where name == "SkillFunction"
| extend skill = tostring(customDimensions.skillName)
| extend function = tostring(customDimensions.functionName)
| extend success = tobool(customDimensions.success)
| extend environment = tostring(customDimensions.AspNetCoreEnvironment)
| extend skillFunction = strcat(skill, '/', function)
| summarize Total=count(), Success=countif(success) by skillFunction, environment
| project skillFunction, SuccessPercentage = 100.0 * Success/Total, environment
| order by SuccessPercentage asc
```

You may wish to use the Visual tab to turn on conditional formatting to highlight low success rates or render it as a chart.

Finally you could render this data over time with a query like this:

```kql
customEvents
| where timestamp between (_startTime .. _endTime)
| where name == "SkillFunction"
| extend skill = tostring(customDimensions.skillName)
| extend function = tostring(customDimensions.functionName)
| extend success = tobool(customDimensions.success)
| extend environment = tostring(customDimensions.AspNetCoreEnvironment)
| extend skillFunction = strcat(skill, '/', function)
| summarize Total=count(), Success=countif(success) by skillFunction, environment, bin(timestamp,1m)
| project skillFunction, SuccessPercentage = 100.0 * Success/Total, environment, timestamp
| order by timestamp asc
```

Then use a Time chart on the Visual tab.
