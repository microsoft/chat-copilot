// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Skills.ChatSkills;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.Connectors.Memory.AzureCognitiveSearch;
using Microsoft.SemanticKernel.Connectors.Memory.Qdrant;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Skills.Core;
using Microsoft.SemanticKernel.TemplateEngine;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch;
using Microsoft.SemanticMemory.Core.MemoryStorage.Qdrant;

namespace CopilotChat.WebApi.Extensions;

/// <summary>
/// Extension methods for registering Semantic Kernel related services.
/// </summary>
internal static class SemanticKernelExtensions
{
    /// <summary>
    /// Delegate to register skills with a Semantic Kernel
    /// </summary>
    public delegate Task RegisterSkillsWithKernel(IServiceProvider sp, IKernel kernel);

    /// <summary>
    /// Add Semantic Kernel services
    /// </summary>
    internal static IServiceCollection AddSemanticKernelServices(this IServiceCollection services)
    {
        // Semantic Memory
        services.AddSemanticTextMemory();

        // Semantic Kernel
        services.AddScoped<IKernel>(sp =>
        {
            IKernel kernel = Kernel.Builder
                .WithLogger(sp.GetRequiredService<ILogger<IKernel>>())
                .WithMemory(sp.GetRequiredService<ISemanticTextMemory>())
                .WithCompletionBackend(sp.GetRequiredService<IOptions<AIServiceOptions>>().Value)
                .Build();

            sp.GetRequiredService<RegisterSkillsWithKernel>()(sp, kernel);
            return kernel;
        });

        // Azure Content Safety
        services.AddContentSafety();

        // Register skills
        services.AddScoped<RegisterSkillsWithKernel>(sp => RegisterSkillsAsync);

        return services;
    }

    /// <summary>
    /// Add Planner services
    /// </summary>
    public static IServiceCollection AddPlannerServices(this IServiceCollection services)
    {
        services.AddScoped<CopilotChatPlanner>(sp =>
        {
            var plannerOptions = sp.GetRequiredService<IOptions<PlannerOptions>>();
            IKernel plannerKernel = Kernel.Builder
                .WithLogger(sp.GetRequiredService<ILogger<IKernel>>())
                .WithMemory(sp.GetRequiredService<ISemanticTextMemory>())
                // TODO: [sk Issue #2046] verify planner has AI service configured
                .WithPlannerBackend(sp.GetRequiredService<IOptions<AIServiceOptions>>().Value)
                .Build();
            return new CopilotChatPlanner(plannerKernel, plannerOptions?.Value);
        });

        // Register Planner skills (AI plugins) here.
        // TODO: [sk Issue #2046] Move planner skill registration from ChatController to this location.

        return services;
    }

    /// <summary>
    /// Register the chat skill with the kernel.
    /// </summary>
    public static IKernel RegisterChatSkill(this IKernel kernel, IServiceProvider sp)
    {
        // Chat skill
        kernel.ImportSkill(
            new ChatSkill(
                kernel,
                memoryClient: sp.GetRequiredService<ISemanticMemoryClient>(),
                chatMessageRepository: sp.GetRequiredService<ChatMessageRepository>(),
                chatSessionRepository: sp.GetRequiredService<ChatSessionRepository>(),
                messageRelayHubContext: sp.GetRequiredService<IHubContext<MessageRelayHub>>(),
                promptOptions: sp.GetRequiredService<IOptions<PromptsOptions>>(),
                documentImportOptions: sp.GetRequiredService<IOptions<DocumentMemoryOptions>>(),
                contentSafety: sp.GetService<AzureContentSafety>(),
                planner: sp.GetRequiredService<CopilotChatPlanner>(),
                logger: sp.GetRequiredService<ILogger<ChatSkill>>()),
            nameof(ChatSkill));

        return kernel;
    }

    /// <summary>
    /// Propagate exception from within semantic function
    /// </summary>
    public static void ThrowIfFailed(this SKContext context)
    {
        if (context.ErrorOccurred)
        {
            context.Logger.LogError(context.LastException, "{0}", context.LastException?.Message);
            throw context.LastException!;
        }
    }

    /// <summary>
    /// Register the skills with the kernel.
    /// </summary>
    private static Task RegisterSkillsAsync(IServiceProvider sp, IKernel kernel)
    {
        // Copilot chat skills
        kernel.RegisterChatSkill(sp);

        // Time skill
        kernel.ImportSkill(new TimeSkill(), nameof(TimeSkill));

        // Semantic skills
        ServiceOptions options = sp.GetRequiredService<IOptions<ServiceOptions>>().Value;
        if (!string.IsNullOrWhiteSpace(options.SemanticSkillsDirectory))
        {
            foreach (string subDir in Directory.GetDirectories(options.SemanticSkillsDirectory))
            {
                try
                {
                    kernel.ImportSemanticSkillFromDirectory(options.SemanticSkillsDirectory, Path.GetFileName(subDir)!);
                }
                catch (TemplateException e)
                {
                    kernel.Logger.LogError("Could not load skill from {Directory}: {Message}", subDir, e.Message);
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Add the semantic memory used by the planner.
    /// </summary>
    private static void AddSemanticTextMemory(this IServiceCollection services)
    {
        services.AddScoped<ISemanticTextMemory>(
            sp =>
                new SemanticTextMemory(
                    sp.GetRequiredService<IMemoryStore>(),
                    sp.GetRequiredService<IOptions<AIServiceOptions>>().Value
                        .ToTextEmbeddingsService(logger: sp.GetRequiredService<ILogger<AIServiceOptions>>())));

        services.AddSingleton<IMemoryStore>(
            sp =>
            {
                var configMemory = sp.GetRequiredService<IOptions<SemanticMemoryConfig>>().Value;

                var memoryType = Enum.Parse<MemoryStoreType>(configMemory.Retrieval.VectorDbType, ignoreCase: true);
                switch (memoryType)
                {
                    //case MemoryStoreType.Volatile: // TODO: $$$
                    //    services.AddSingleton<IMemoryStore, VolatileMemoryStore>();
                    //    break;

                    case MemoryStoreType.Qdrant:
                    {
                        var configStorage = sp.GetService<QdrantConfig>();
                        if (configStorage == null)
                        {
                            throw new InvalidOperationException("MemoryStore type is Qdrant and Qdrant configuration is null.");
                        }

                        HttpClient httpClient = new(new HttpClientHandler { CheckCertificateRevocationList = true });
                        if (!string.IsNullOrWhiteSpace(configStorage.APIKey))
                        {
                            httpClient.DefaultRequestHeaders.Add("api-key", configStorage.APIKey);
                        }

                        return new QdrantMemoryStore(
                            httpClient,
                            1536, // $$$ configStorage.VectorSize,
                            configStorage.Endpoint,
                            logger: sp.GetRequiredService<ILogger<IQdrantVectorDbClient>>()
                        );
                    }

                    case MemoryStoreType.AzureCognitiveSearch:
                    {
                        var configStorage = sp.GetService<AzureCognitiveSearchConfig>();
                        if (configStorage == null)
                        {
                            throw new InvalidOperationException("MemoryStore type is AzureCognitiveSearch and AzureCognitiveSearch configuration is null.");
                        }

                        return new AzureCognitiveSearchMemoryStore(configStorage.Endpoint, configStorage.APIKey);
                    }

                    case MemoryStoreType.Chroma: // TODO: $$$
                    /*
                        if (config.Chroma == null)
                        {
                            throw new InvalidOperationException("MemoryStore type is Chroma and Chroma configuration is null.");
                        }

                        HttpClient httpClient = new(new HttpClientHandler { CheckCertificateRevocationList = true });
                        var endPointBuilder = new UriBuilder(config.Chroma.Host);
                        endPointBuilder.Port = config.Chroma.Port;

                        return new ChromaMemoryStore(
                            httpClient: httpClient,
                            endpoint: endPointBuilder.ToString(),
                            logger: sp.GetRequiredService<ILogger<IChromaClient>>()
                        );
                     */

                    case MemoryStoreType.Postgres: // TODO: $$$
                    /*
                        if (config.Postgres == null)
                        {
                            throw new InvalidOperationException("MemoryStore type is Cosmos and Cosmos configuration is null.");
                        }

                        var dataSourceBuilder = new NpgsqlDataSourceBuilder(config.Postgres.ConnectionString);
                        dataSourceBuilder.UseVector();

                        return new PostgresMemoryStore(
                            dataSource: dataSourceBuilder.Build(),
                            vectorSize: config.Postgres.VectorSize
                        );

                        break;
                     */

                    default:
                        throw new InvalidOperationException($"Invalid 'MemoryStore' type '{configMemory.Retrieval.VectorDbType}'.");
                }
            });
    }

    /// <summary>
    /// Adds Azure Content Safety
    /// </summary>
    internal static void AddContentSafety(this IServiceCollection services)
    {
        IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        var options = configuration.GetSection(ContentSafetyOptions.PropertyName).Get<ContentSafetyOptions>();

        if (options?.Enabled ?? false)
        {
            services.AddSingleton<IContentSafetyService, AzureContentSafety>(sp => new AzureContentSafety(new Uri(options.Endpoint), options.Key, options));
        }
    }

    /// <summary>
    /// Add the completion backend to the kernel config
    /// </summary>
    private static KernelBuilder WithCompletionBackend(this KernelBuilder kernelBuilder, AIServiceOptions options)
    {
        return options.Type switch
        {
            AIServiceOptions.AIServiceType.AzureOpenAI
                => kernelBuilder.WithAzureChatCompletionService(options.Models.Completion, options.Endpoint, options.Key),
            AIServiceOptions.AIServiceType.OpenAI
                => kernelBuilder.WithOpenAIChatCompletionService(options.Models.Completion, options.Key),
            _
                => throw new ArgumentException($"Invalid {nameof(options.Type)} value in '{AIServiceOptions.PropertyName}' settings."),
        };
    }

    /// <summary>
    /// Add the completion backend to the kernel config for the planner.
    /// </summary>
    private static KernelBuilder WithPlannerBackend(this KernelBuilder kernelBuilder, AIServiceOptions options)
    {
        return options.Type switch
        {
            AIServiceOptions.AIServiceType.AzureOpenAI => kernelBuilder.WithAzureChatCompletionService(options.Models.Planner, options.Endpoint, options.Key),
            AIServiceOptions.AIServiceType.OpenAI => kernelBuilder.WithOpenAIChatCompletionService(options.Models.Planner, options.Key),
            _ => throw new ArgumentException($"Invalid {nameof(options.Type)} value in '{AIServiceOptions.PropertyName}' settings."),
        };
    }

    /// <summary>
    /// Construct IEmbeddingGeneration from <see cref="AIServiceOptions"/>
    /// </summary>
    /// <param name="options">The service configuration</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="logger">Application logger</param>
    private static ITextEmbeddingGeneration ToTextEmbeddingsService(this AIServiceOptions options,
        HttpClient? httpClient = null,
        ILogger? logger = null)
    {
        return options.Type switch
        {
            AIServiceOptions.AIServiceType.AzureOpenAI
                => new AzureTextEmbeddingGeneration(options.Models.Embedding, options.Endpoint, options.Key, httpClient: httpClient, logger: logger),
            AIServiceOptions.AIServiceType.OpenAI
                => new OpenAITextEmbeddingGeneration(options.Models.Embedding, options.Key, httpClient: httpClient, logger: logger),
            _
                => throw new ArgumentException("Invalid AIService value in embeddings backend settings"),
        };
    }
}
