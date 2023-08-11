// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.ContentStorage.AzureBlobs;
using Microsoft.SemanticMemory.Core.ContentStorage.FileSystemStorage;
using Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch;
using Microsoft.SemanticMemory.Core.MemoryStorage;
using Microsoft.SemanticMemory.Core.Pipeline.Queue;
using Microsoft.SemanticMemory.Core.Pipeline.Queue.AzureQueues;
using Microsoft.SemanticMemory.Core.Pipeline.Queue.FileBasedQueues;
using Microsoft.SemanticMemory.Core.Pipeline.Queue.RabbitMq;
using Microsoft.Extensions.Options;

namespace CopilotChat.WebApi.Extensions;

/// <summary>
/// Extension methods for registering Semantic Memory related services.
/// </summary>
internal static class SemanticMemoryExtensions
{
    private const string ConfigRoot = "SemanticMemory";

    /// <summary>
    /// Add Semantic Memory services
    /// </summary>
    public static void AddSemanticMemoryServices(this WebApplicationBuilder builder)
    {
        var memoryConfig = builder.AddSemanticMemoryOptions();

        builder.Services.ConfigureRuntime(memoryConfig);

        builder
            .ConfigureContentStorage(memoryConfig)
            .ConfigureQueueSystem(memoryConfig)
            .ConfigureEmbeddingStorage(memoryConfig);
    }

    /// <summary>
    /// Add Semantic Memory options.
    /// </summary>
    private static SemanticMemoryConfig AddSemanticMemoryOptions(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(ConfigRoot);

        builder.Services.AddOptions<SemanticMemoryConfig>(section);

        // Compatibility shim to match expectations of semantic-memory dependencies 
        builder.Services.AddSingleton<SemanticMemoryConfig>(
            sp => sp.GetRequiredService<IOptions<SemanticMemoryConfig>>().Value);

        var config = section.Get<SemanticMemoryConfig>() ?? throw new ConfigurationException($"Configuration section not found: {ConfigRoot}");

        return config;
    }

    /// <summary>
    /// Service where documents and temporary files are stored
    /// </summary>
    private static WebApplicationBuilder ConfigureContentStorage(this WebApplicationBuilder builder, SemanticMemoryConfig config)
    {
        switch (config.ContentStorageType)
        {
            case string x when x.Equals("AzureBlobs", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddAzureBlobAsContentStorage(builder.Configuration
                    .GetSection(ConfigRoot).GetSection("Services").GetSection("AzureBlobs")
                    .Get<AzureBlobConfig>()!);
                break;

            case string x when x.Equals("FileSystemContentStorage", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddFileSystemAsContentStorage(builder.Configuration
                    .GetSection(ConfigRoot).GetSection("Services").GetSection("FileSystemContentStorage")
                    .Get<FileSystemConfig>()!);
                break;

            default:
                throw new NotSupportedException($"Unknown/unsupported {config.ContentStorageType} content storage");
        }

        return builder;
    }

    /// <summary>
    /// Orchestration dependencies, ie. which queueing system to use
    /// </summary>
    private static WebApplicationBuilder ConfigureQueueSystem(this WebApplicationBuilder builder, SemanticMemoryConfig config)
    {
        switch (config.DataIngestion.DistributedOrchestration.QueueType)
        {
            case string y when y.Equals("AzureQueue", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddAzureQueue(builder.Configuration
                    .GetSection(ConfigRoot).GetSection("Services").GetSection("AzureQueue")
                    .Get<AzureQueueConfig>()!);
                break;

            case string y when y.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddRabbitMq(builder.Configuration
                    .GetSection(ConfigRoot).GetSection("Services").GetSection("RabbitMq")
                    .Get<RabbitMqConfig>()!);
                break;

            case string y when y.Equals("FileBasedQueue", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddFileBasedQueue(builder.Configuration
                    .GetSection(ConfigRoot).GetSection("Services").GetSection("FileBasedQueue")
                    .Get<FileBasedQueueConfig>()!);
                break;

            default:
                throw new NotSupportedException($"Unknown/unsupported {config.DataIngestion.DistributedOrchestration.QueueType} queue type");
        }

        return builder;
    }

    /// <summary>
    /// List of Vector DB list where to store embeddings (multiple DBs allowed during ingestion)
    /// </summary>
    private static WebApplicationBuilder ConfigureEmbeddingStorage(this WebApplicationBuilder builder, SemanticMemoryConfig config)
    {
        var vectorDbServices = new TypeCollection<ISemanticMemoryVectorDb>();
        builder.Services.AddSingleton(vectorDbServices);
        foreach (var type in config.DataIngestion.VectorDbTypes)
        {
            switch (type)
            {
                case string x when x.Equals("AzureCognitiveSearch", StringComparison.OrdinalIgnoreCase):
                    vectorDbServices.Add<AzureCognitiveSearchMemory>();
                    builder.Services.AddAzureCognitiveSearchAsVectorDb(builder.Configuration
                        .GetSection(ConfigRoot).GetSection("Services").GetSection("AzureCognitiveSearch")
                        .Get<AzureCognitiveSearchConfig>()!);
                    break;

                default:
                    throw new NotSupportedException($"Unknown/unsupported {type} vector DB");
            }
        }

        return builder;
    }
}
