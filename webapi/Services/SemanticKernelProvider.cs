// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using System.Threading;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryStorage.Qdrant;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.Memory.AzureCognitiveSearch;
using Microsoft.SemanticKernel.Connectors.Memory.Qdrant;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Extension methods for registering Semantic Kernel related services.
/// </summary>
public sealed class SemanticKernelProvider
{
    private static IMemoryStore? _volatileMemoryStore;

    private readonly KernelBuilder _builderChat;
    private readonly KernelBuilder _builderPlanner;
    private readonly MemoryBuilder _builderMemory;

    public SemanticKernelProvider(IServiceProvider serviceProvider, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        this._builderChat = InitializeCompletionKernel(serviceProvider, configuration, httpClientFactory);
        this._builderPlanner = InitializePlannerKernel(serviceProvider, configuration, httpClientFactory);
        this._builderMemory = InitializeMigrationMemory(serviceProvider, configuration, httpClientFactory);
    }

    /// <summary>
    /// Produce semantic-kernel with only completion services for chat.
    /// </summary>
    public IKernel GetCompletionKernel() => this._builderChat.Build();

    /// <summary>
    /// Produce semantic-kernel with only completion services for planner.
    /// </summary>
    public IKernel GetPlannerKernel() => this._builderPlanner.Build();

    /// <summary>
    /// Produce semantic-kernel with semantic-memory.
    /// </summary>
    public ISemanticTextMemory GetMigrationMemory() => this._builderMemory.Build();

    private static KernelBuilder InitializeCompletionKernel(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        var builder = new KernelBuilder();

        builder.WithLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>());

        var memoryOptions = serviceProvider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;

        switch (memoryOptions.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(configuration, "AzureOpenAIText");
#pragma warning disable CA2000 // No need to dispose of HttpClient instances from IHttpClientFactory
                builder.WithAzureChatCompletionService(
                    azureAIOptions.Deployment,
                    azureAIOptions.Endpoint,
                    azureAIOptions.APIKey,
                    httpClient: httpClientFactory.CreateClient());
#pragma warning restore CA2000
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");
#pragma warning disable CA2000 // No need to dispose of HttpClient instances from IHttpClientFactory
                builder.WithOpenAIChatCompletionService(
                    openAIOptions.TextModel,
                    openAIOptions.APIKey,
                    httpClient: httpClientFactory.CreateClient());
#pragma warning restore CA2000
                break;

            default:
                throw new ArgumentException($"Invalid {nameof(memoryOptions.TextGeneratorType)} value in 'KernelMemory' settings.");
        }

        return builder;
    }

    private static KernelBuilder InitializePlannerKernel(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        var builder = new KernelBuilder();

        builder.WithLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>());

        var memoryOptions = serviceProvider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;
        var plannerOptions = serviceProvider.GetRequiredService<IOptions<PlannerOptions>>().Value;

        switch (memoryOptions.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(configuration, "AzureOpenAIText");
#pragma warning disable CA2000 // No need to dispose of HttpClient instances from IHttpClientFactory
                builder.WithAzureChatCompletionService(
                    plannerOptions.Model,
                    azureAIOptions.Endpoint,
                    azureAIOptions.APIKey,
                    httpClient: httpClientFactory.CreateClient());
#pragma warning restore CA2000
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");
#pragma warning disable CA2000 // No need to dispose of HttpClient instances from IHttpClientFactory
                builder.WithOpenAIChatCompletionService(
                    plannerOptions.Model,
                    openAIOptions.APIKey,
                    httpClient: httpClientFactory.CreateClient());
#pragma warning restore CA2000
                break;

            default:
                throw new ArgumentException($"Invalid {nameof(memoryOptions.TextGeneratorType)} value in 'KernelMemory' settings.");
        }

        return builder;
    }

    private static MemoryBuilder InitializeMigrationMemory(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        var memoryOptions = serviceProvider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;

        var builder = new MemoryBuilder();

        builder.WithLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>());
        builder.WithMemoryStore(CreateMemoryStore());

        switch (memoryOptions.Retrieval.EmbeddingGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(configuration, "AzureOpenAIEmbedding");
#pragma warning disable CA2000 // No need to dispose of HttpClient instances from IHttpClientFactory
                builder.WithAzureTextEmbeddingGenerationService(
                    azureAIOptions.Deployment,
                    azureAIOptions.Endpoint,
                    azureAIOptions.APIKey,
                    httpClient: httpClientFactory.CreateClient());
#pragma warning restore CA2000
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");
#pragma warning disable CA2000 // No need to dispose of HttpClient instances from IHttpClientFactory
                builder.WithOpenAITextEmbeddingGenerationService(
                    openAIOptions.EmbeddingModel,
                    openAIOptions.APIKey,
                    httpClient: httpClientFactory.CreateClient());
#pragma warning restore CA2000
                break;

            default:
                throw new ArgumentException($"Invalid {nameof(memoryOptions.Retrieval.EmbeddingGeneratorType)} value in 'KernelMemory' settings.");
        }
        return builder;

        IMemoryStore CreateMemoryStore()
        {
            switch (memoryOptions.Retrieval.VectorDbType)
            {
                case string x when x.Equals("SimpleVectorDb", StringComparison.OrdinalIgnoreCase):
                    // Maintain single instance of volatile memory.
                    Interlocked.CompareExchange(ref _volatileMemoryStore, new VolatileMemoryStore(), null);
                    return _volatileMemoryStore;

                case string x when x.Equals("Qdrant", StringComparison.OrdinalIgnoreCase):
                    var qdrantConfig = memoryOptions.GetServiceConfig<QdrantConfig>(configuration, "Qdrant");

#pragma warning disable CA2000 // Ownership passed to QdrantMemoryStore
                    HttpClient httpClient = new(new HttpClientHandler { CheckCertificateRevocationList = true });
#pragma warning restore CA2000 // Ownership passed to QdrantMemoryStore
                    if (!string.IsNullOrWhiteSpace(qdrantConfig.APIKey))
                    {
                        httpClient.DefaultRequestHeaders.Add("api-key", qdrantConfig.APIKey);
                    }

                    return
                        new QdrantMemoryStore(
                            httpClient: httpClient,
                            1536,
                            qdrantConfig.Endpoint,
                            loggerFactory: serviceProvider.GetRequiredService<ILoggerFactory>());

                case string x when x.Equals("AzureCognitiveSearch", StringComparison.OrdinalIgnoreCase):
                    var acsConfig = memoryOptions.GetServiceConfig<AzureCognitiveSearchConfig>(configuration, "AzureCognitiveSearch");
                    return new AzureCognitiveSearchMemoryStore(acsConfig.Endpoint, acsConfig.APIKey);

                default:
                    throw new InvalidOperationException($"Invalid 'VectorDbType' type '{memoryOptions.Retrieval.VectorDbType}'.");
            }
        }
    }
}
