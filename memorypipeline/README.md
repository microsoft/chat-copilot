# Chat Copilot Memory Pipeline

> **!IMPORTANT**
> This sample is for educational purposes only and is not recommended for production deployments.

> **IMPORTANT:** The pipeline will call Azure OpenAI/OpenAI which will use tokens that you may be billed for.

## Introduction

### Memory

One of the exciting features of the Copilot Chat App is its ability to store contextual information
to [memories](https://github.com/microsoft/semantic-kernel/blob/main/docs/EMBEDDINGS.md) and retrieve
relevant information from memories to provide more meaningful answers to users through out the conversations.

Memories can be generated from conversations as well as imported from external sources, such as documents.
Importing documents enables Copilot Chat to have up-to-date knowledge of specific contexts, such as enterprise and personal data.

### Memory pipeline in Chat Copilot

Chat copilot integrates [Semantic Memory](https://github.com/microsoft/semantic-memory) as the memory solution provider. The memory pipeline is designed to be run as an asynchronous service. If you are expecting to import big documents that can require minutes to process or planning to carry long conversations with the bot, then you can deploy the memory pipeline as a separate service along with the [chat copilot webapi](https://github.com/microsoft/chat-copilot/tree/main/webapi).

### Configuration

(Optional) Before you get started, make sure you have the following requirements in place:

- [An Azure Subscription](https://azure.microsoft.com/en-us/free/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

#### Webapi

Please refer to the [webapi README](../webapi/README.md).

#### Memorypipeline

The memorypipeline is only needed when `SemanticMemory:DataIngestion:OrchestrationType` is set to `Distributed` in [../webapi/appsettings.json](./appsettings.json).

- Content Storage: storage solution to save the original contents. Available options:
  - AzureBlobs
  - SimpleFileStorage: stores data on your local file system.
- [Message Queue](https://learn.microsoft.com/en-us/azure/storage/queues/storage-queues-introduction): asynchronous service to service communication. Available options:
  - AzureQueue
  - RabbitMQ
  - SimpleQueues: stores messages on your local file system.
- [Vector database](https://learn.microsoft.com/en-us/semantic-kernel/memories/vector-db): storage solution for high-dimensional vectors, aka [embeddings](https://github.com/microsoft/semantic-kernel/blob/main/docs/EMBEDDINGS.md). Available options:
  - [AzureCognitiveSearch](https://learn.microsoft.com/en-us/azure/search/search-what-is-azure-search)
  - [Qdrant](https://github.com/qdrant/qdrant)
  - SimpleVectorDb
    - TextFile: stores vectors on your local file system.
    - Volatile: stores vectors in RAM.
      > Note that do not configure the memory pipeline to use Volatile. Use volatile in the webapi only when its `SemanticMemory:DataIngestion:OrchestrationType` is set to `InProcess`.

##### AzureBlobs & AzureQueue

> Note: Make sure to use the same resource for both the webapi and memorypipeline.

1. Create a storage [account](https://learn.microsoft.com/en-us/azure/storage/common/storage-account-create?toc=%2Fazure%2Fstorage%2Fblobs%2Ftoc.json&tabs=azure-portal).
2. Find the **connection string** under **Access keys** on the portal.
3. Run the following to set up the authentication to the resources:
   ```bash
   dotnet user-secrets set SemanticMemory:Services:AzureBlobs:Auth ConnectionString
   dotnet user-secrets set SemanticMemory:Services:AzureBlobs:ConnectionString [your secret]
   dotnet user-secrets set SemanticMemory:Services:AzureQueue:Auth ConnectionString
   dotnet user-secrets set SemanticMemory:Services:AzureQueue:ConnectionString [your secret]
   ```

##### [Azure Cognitive Search](https://learn.microsoft.com/en-us/azure/search/search-what-is-azure-search)

> Note: Make sure to use the same resource for both the webapi and memorypipeline.

1. Create a [search service](https://learn.microsoft.com/en-us/azure/search/search-create-service-portal).
2. Find the **Url** under **Overview** and the **key** under **Keys** on the portal.
3. Run the following to set up the authentication to the resources:
   ```bash
   dotnet user-secrets set SemanticMemory:Services:AzureCognitiveSearch:Endpoint [your secret]
   dotnet user-secrets set SemanticMemory:Services:AzureCognitiveSearch:APIKey [your secret]
   ```

##### RabbitMQ

> Note: Make sure to use the same queue for both webapi and memorypipeline

Run the following:

```
docker run -it --rm --name rabbitmq \
  -e RABBITMQ_DEFAULT_USER=user \
  -e RABBITMQ_DEFAULT_PASS=password \
  -p 5672:5672 \
  rabbitmq:3
```

##### Qdrant

> Note: Make sure to use the same vector storage for both webapi and memorypipeline

```
docker run -it --rm --name qdrant \
-p 6333:6333 \
qdrant/qdrant
```

> To stop the container, in another terminal window run

```
docker container stop [name]
docker container rm [name]
```
