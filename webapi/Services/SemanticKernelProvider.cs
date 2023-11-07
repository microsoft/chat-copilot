// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using System.Threading;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.Memory.AzureCognitiveSearch;
using Microsoft.SemanticKernel.Connectors.Memory.Qdrant;
using Microsoft.SemanticKernel.Memory;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryStorage.Qdrant;
using Microsoft.SemanticKernel.Plugins.Memory;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Extension methods for registering Semantic Kernel related services.
/// </summary>
public sealed class SemanticKernelProvider
{
    private static IMemoryStore? _volatileMemoryStore;

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly KernelBuilder _builder;

    public SemanticKernelProvider(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        this._serviceProvider = serviceProvider;
        this._configuration = configuration;
        this._builder = new KernelBuilder();
    }

    /// <summary>
    /// Produce semantic-kernel with only completion services for chat.
    /// </summary>
    public IKernel GetCompletionKernel()
    {
        this._builder.WithLoggerFactory(this._serviceProvider.GetRequiredService<ILoggerFactory>());

        this.WithCompletionBackend();

        return this._builder.Build();
    }

    /// <summary>
    /// Produce semantic-kernel with only completion services for planner.
    /// </summary>
    public IKernel GetPlannerKernel()
    {
        this._builder.WithLoggerFactory(this._serviceProvider.GetRequiredService<ILoggerFactory>());

        this.WithPlannerBackend();

        return this._builder.Build();
    }

    /// <summary>
    /// Produce semantic-kernel with semantic-memory.
    /// </summary>
    public IKernel GetMigrationKernel()
    {
        var builder = this._builder.WithLoggerFactory(this._serviceProvider.GetRequiredService<ILoggerFactory>());

        this.WithEmbeddingBackend();
        this.WithSemanticTextMemory();

        return builder.Build();
    }

    /// <summary>
    /// Add the completion backend to the kernel config
    /// </summary>
    private void WithCompletionBackend()
    {
        var memoryOptions = this._serviceProvider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;

        switch (memoryOptions.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(this._configuration, "AzureOpenAIText");
                this._builder.WithAzureChatCompletionService(azureAIOptions.Deployment, azureAIOptions.Endpoint, azureAIOptions.APIKey);
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(this._configuration, "OpenAI");
                this._builder.WithOpenAIChatCompletionService(openAIOptions.TextModel, openAIOptions.APIKey);
                break;

            default:
                throw new ArgumentException($"Invalid {nameof(memoryOptions.TextGeneratorType)} value in 'KernelMemory' settings.");
        }
    }

    /// <summary>
    /// Add the completion backend to the kernel config for the planner.
    /// </summary>
    private void WithPlannerBackend()
    {
        var memoryOptions = this._serviceProvider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;
        var plannerOptions = this._serviceProvider.GetRequiredService<IOptions<PlannerOptions>>().Value;

        switch (memoryOptions.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(this._configuration, "AzureOpenAIText");
                this._builder.WithAzureChatCompletionService(plannerOptions.Model, azureAIOptions.Endpoint, azureAIOptions.APIKey);
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(this._configuration, "OpenAI");
                this._builder.WithOpenAIChatCompletionService(plannerOptions.Model, openAIOptions.APIKey);
                break;

            default:
                throw new ArgumentException($"Invalid {nameof(memoryOptions.TextGeneratorType)} value in 'KernelMemory' settings.");
        }
    }

    /// <summary>
    /// Add the embedding backend to the kernel config
    /// </summary>
    private void WithEmbeddingBackend()
    {
        var memoryOptions = this._serviceProvider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;

        switch (memoryOptions.Retrieval.EmbeddingGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(this._configuration, "AzureOpenAIEmbedding");
                this._builder.WithAzureTextEmbeddingGenerationService(azureAIOptions.Deployment, azureAIOptions.Endpoint, azureAIOptions.APIKey);
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(this._configuration, "OpenAI");
                this._builder.WithOpenAITextEmbeddingGenerationService(openAIOptions.EmbeddingModel, openAIOptions.APIKey);
                break;

            default:
                throw new ArgumentException($"Invalid {nameof(memoryOptions.Retrieval.EmbeddingGeneratorType)} value in 'KernelMemory' settings.");
        }
    }

    /// <summary>
    /// Add the semantic text memory.
    /// </summary>
    private void WithSemanticTextMemory()
    {
        var memoryOptions = this._serviceProvider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;

        IMemoryStore memoryStore = CreateMemoryStore();

        this._builder.WithMemory(
            new SemanticTextMemory(
                memoryStore,
                this._serviceProvider.GetRequiredService<ITextEmbeddingGeneration>()));

        IMemoryStore CreateMemoryStore()
        {
            switch (memoryOptions.Retrieval.VectorDbType)
            {
                case string x when x.Equals("SimpleVectorDb", StringComparison.OrdinalIgnoreCase):
                    // Maintain single instance of volatile memory.
                    Interlocked.CompareExchange(ref _volatileMemoryStore, new VolatileMemoryStore(), null);
                    return _volatileMemoryStore;

                case string x when x.Equals("Qdrant", StringComparison.OrdinalIgnoreCase):
                    var qdrantConfig = memoryOptions.GetServiceConfig<QdrantConfig>(this._configuration, "Qdrant");

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
                            loggerFactory: this._serviceProvider.GetRequiredService<ILoggerFactory>());

                case string x when x.Equals("AzureCognitiveSearch", StringComparison.OrdinalIgnoreCase):
                    var acsConfig = memoryOptions.GetServiceConfig<AzureCognitiveSearchConfig>(this._configuration, "AzureCognitiveSearch");
                    return new AzureCognitiveSearchMemoryStore(acsConfig.Endpoint, acsConfig.APIKey);

                default:
                    throw new InvalidOperationException($"Invalid 'VectorDbType' type '{memoryOptions.Retrieval.VectorDbType}'.");
            }
        }
    }
}
