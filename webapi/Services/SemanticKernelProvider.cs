// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Memory;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Extension methods for registering Semantic Kernel related services.
/// </summary>
public sealed class SemanticKernelProvider
{
    private static IMemoryStore? _volatileMemoryStore;

    private readonly IKernelBuilder _builderChat;
    private readonly MemoryBuilder _builderMemory;

    public SemanticKernelProvider(IServiceProvider serviceProvider, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        this._builderChat = InitializeCompletionKernel(serviceProvider, configuration, httpClientFactory);
        this._builderMemory = InitializeMigrationMemory(serviceProvider, configuration, httpClientFactory);
    }

    /// <summary>
    /// Produce semantic-kernel with only completion services for chat.
    /// </summary>
    public Kernel GetCompletionKernel() => this._builderChat.Build();

    /// <summary>
    /// Produce semantic-kernel with kernel memory.
    /// </summary>
    public ISemanticTextMemory MigrationMemory => this._builderMemory.Build();

    private static IKernelBuilder InitializeCompletionKernel(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        var builder = Kernel.CreateBuilder();

        builder.Services.AddLogging();

        var memoryOptions = serviceProvider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;

        switch (memoryOptions.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(configuration, "AzureOpenAIText");
#pragma warning disable CA2000 // No need to dispose of HttpClient instances from IHttpClientFactory
                builder.AddAzureOpenAIChatCompletion(
                    azureAIOptions.Deployment,
                    azureAIOptions.Endpoint,
                    azureAIOptions.APIKey,
                    httpClient: httpClientFactory.CreateClient());
#pragma warning restore CA2000
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");
#pragma warning disable CA2000 // No need to dispose of HttpClient instances from IHttpClientFactory
                builder.AddOpenAIChatCompletion(
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
                builder.WithAzureOpenAITextEmbeddingGeneration(
                    azureAIOptions.Deployment,
                    azureAIOptions.Endpoint,
                    azureAIOptions.APIKey,
                    httpClient: httpClientFactory.CreateClient());
#pragma warning restore CA2000
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");
#pragma warning disable CA2000 // No need to dispose of HttpClient instances from IHttpClientFactory
                builder.WithOpenAITextEmbeddingGeneration(
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
            switch (memoryOptions.Retrieval.MemoryDbType)
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

                case string x when x.Equals("AzureAISearch", StringComparison.OrdinalIgnoreCase):
                    var acsConfig = memoryOptions.GetServiceConfig<AzureAISearchConfig>(configuration, "AzureAISearch");
                    return new AzureAISearchMemoryStore(acsConfig.Endpoint, acsConfig.APIKey);

                default:
                    throw new InvalidOperationException($"Invalid 'MemoryDbType' type '{memoryOptions.Retrieval.MemoryDbType}'.");
            }
        }
    }
}
