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
using Microsoft.SemanticMemory;
using Microsoft.SemanticMemory.MemoryStorage.Qdrant;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Extension methods for registering Semantic Kernel related services.
/// </summary>
public sealed class SemanticKernelProvider
{
    private static IMemoryStore? _volatileMemoryStore;

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public SemanticKernelProvider(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        this._serviceProvider = serviceProvider;
        this._configuration = configuration;
    }

    /// <summary>
    /// Produce semantic-kernel with only completion services for chat.
    /// </summary>
    public IKernel GetCompletionKernel()
    {
        var builder = Kernel.Builder.WithLoggerFactory(this._serviceProvider.GetRequiredService<ILoggerFactory>());

        this.WithCompletionBackend(builder);

        return builder.Build();
    }

    /// <summary>
    /// Produce semantic-kernel with only completion services for planner.
    /// </summary>
    public IKernel GetPlannerKernel()
    {
        var builder = Kernel.Builder.WithLoggerFactory(this._serviceProvider.GetRequiredService<ILoggerFactory>());

        this.WithPlannerBackend(builder);

        return builder.Build();
    }

    /// <summary>
    /// Produce semantic-kernel with semantic-memory.
    /// </summary>
    public IKernel GetMigrationKernel()
    {
        var builder = Kernel.Builder.WithLoggerFactory(this._serviceProvider.GetRequiredService<ILoggerFactory>());

        this.WithEmbeddingBackend(builder);
        this.WithSemanticTextMemory(builder);

        return builder.Build();
    }

    /// <summary>
    /// Add the completion backend to the kernel config
    /// </summary>
    private KernelBuilder WithCompletionBackend(KernelBuilder kernelBuilder)
    {
        var memoryOptions = this._serviceProvider.GetRequiredService<IOptions<SemanticMemoryConfig>>().Value;

        switch (memoryOptions.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(this._configuration, "AzureOpenAIText");
                return kernelBuilder.WithAzureChatCompletionService(azureAIOptions.Deployment, azureAIOptions.Endpoint, azureAIOptions.APIKey);

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(this._configuration, "OpenAI");
                return kernelBuilder.WithOpenAIChatCompletionService(openAIOptions.TextModel, openAIOptions.APIKey);

            default:
                throw new ArgumentException($"Invalid {nameof(memoryOptions.TextGeneratorType)} value in 'SemanticMemory' settings.");
        }
    }

    /// <summary>
    /// Add the completion backend to the kernel config for the planner.
    /// </summary>
    private KernelBuilder WithPlannerBackend(KernelBuilder kernelBuilder)
    {
        var memoryOptions = this._serviceProvider.GetRequiredService<IOptions<SemanticMemoryConfig>>().Value;
        var plannerOptions = this._serviceProvider.GetRequiredService<IOptions<PlannerOptions>>().Value;

        switch (memoryOptions.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(this._configuration, "AzureOpenAIText");
                return kernelBuilder.WithAzureChatCompletionService(plannerOptions.Model, azureAIOptions.Endpoint, azureAIOptions.APIKey);

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(this._configuration, "OpenAI");
                return kernelBuilder.WithOpenAIChatCompletionService(plannerOptions.Model, openAIOptions.APIKey);

            default:
                throw new ArgumentException($"Invalid {nameof(memoryOptions.TextGeneratorType)} value in 'SemanticMemory' settings.");
        }
    }

    /// <summary>
    /// Add the embedding backend to the kernel config
    /// </summary>
    private KernelBuilder WithEmbeddingBackend(KernelBuilder kernelBuilder)
    {
        var memoryOptions = this._serviceProvider.GetRequiredService<IOptions<SemanticMemoryConfig>>().Value;

        switch (memoryOptions.Retrieval.EmbeddingGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(this._configuration, "AzureOpenAIEmbedding");
                return kernelBuilder.WithAzureTextEmbeddingGenerationService(azureAIOptions.Deployment, azureAIOptions.Endpoint, azureAIOptions.APIKey);

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(this._configuration, "OpenAI");
                return kernelBuilder.WithOpenAITextEmbeddingGenerationService(openAIOptions.EmbeddingModel, openAIOptions.APIKey);

            default:
                throw new ArgumentException($"Invalid {nameof(memoryOptions.Retrieval.EmbeddingGeneratorType)} value in 'SemanticMemory' settings.");
        }
    }

    /// <summary>
    /// Add the semantic text memory.
    /// </summary>
    private void WithSemanticTextMemory(KernelBuilder builder)
    {
        var memoryOptions = this._serviceProvider.GetRequiredService<IOptions<SemanticMemoryConfig>>().Value;

        IMemoryStore memoryStore = CreateMemoryStore();

#pragma warning disable CA2000 // Ownership passed to kernel
        builder.WithMemory(
            new SemanticTextMemory(
                memoryStore,
                this._serviceProvider.GetRequiredService<ITextEmbeddingGeneration>()));
#pragma warning restore CA2000 // Ownership passed to kernel

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
