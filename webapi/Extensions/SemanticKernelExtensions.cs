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
using Microsoft.SemanticKernel.Connectors.Memory.Chroma;
using Microsoft.SemanticKernel.Connectors.Memory.Postgres;
using Microsoft.SemanticKernel.Connectors.Memory.Qdrant;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Skills.Core;
using Npgsql;
using Pgvector.Npgsql;
using static CopilotChat.WebApi.Options.MemoryStoreOptions;

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
        // Semantic Kernel
        services.AddScoped<IKernel>(sp =>
        {
            IKernel kernel = Kernel.Builder
                .WithLoggerFactory(sp.GetRequiredService<ILoggerFactory>())
                .WithMemory(sp.GetRequiredService<ISemanticTextMemory>())
                .WithCompletionBackend(sp.GetRequiredService<IOptions<AIServiceOptions>>().Value)
                .WithEmbeddingBackend(sp.GetRequiredService<IOptions<AIServiceOptions>>().Value)
                .Build();

            sp.GetRequiredService<RegisterSkillsWithKernel>()(sp, kernel);
            return kernel;
        });

        // Semantic memory
        services.AddSemanticTextMemory();

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
        IOptions<PlannerOptions>? plannerOptions = services.BuildServiceProvider().GetService<IOptions<PlannerOptions>>();
        services.AddScoped<CopilotChatPlanner>(sp =>
        {
            IKernel plannerKernel = Kernel.Builder
                .WithLoggerFactory(sp.GetRequiredService<ILoggerFactory>())
                .WithMemory(sp.GetRequiredService<ISemanticTextMemory>())
                // TODO: [sk Issue #2046] verify planner has AI service configured
                .WithPlannerBackend(sp.GetRequiredService<IOptions<AIServiceOptions>>().Value)
                .Build();
            return new CopilotChatPlanner(plannerKernel, plannerOptions?.Value, sp.GetRequiredService<ILogger<CopilotChatPlanner>>());
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
        kernel.ImportSkill(new ChatSkill(
                kernel: kernel,
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
    /// Invokes an asynchronous callback function and tags any exception that occurs with function name.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the callback function.</typeparam>
    /// <param name="callback">The asynchronous callback function to invoke.</param>
    /// <param name="functionName">The name of the function that calls this method, for logging purposes.</param>
    /// <returns>A task that represents the asynchronous operation and contains the result of the callback function.</returns>
    public static async Task<T> SafeInvokeAsync<T>(Func<Task<T>> callback, string functionName)
    {
        try
        {
            // Invoke the callback and await the result
            return await callback();
        }
        catch (Exception ex)
        {
            throw new SKException($"{functionName} failed.", ex);
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
                catch (SKException ex)
                {
                    var logger = kernel.LoggerFactory.CreateLogger(nameof(Kernel));
                    logger.LogError("Could not load skill from {Directory}: {Message}", subDir, ex.Message);
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Add the semantic memory.
    /// </summary>
    private static void AddSemanticTextMemory(this IServiceCollection services)
    {
        MemoryStoreOptions config = services.BuildServiceProvider().GetRequiredService<IOptions<MemoryStoreOptions>>().Value;

        switch (config.Type)
        {
            case MemoryStoreType.Volatile:
                services.AddSingleton<IMemoryStore, VolatileMemoryStore>();
                break;

            case MemoryStoreType.Qdrant:
                if (config.Qdrant == null)
                {
                    throw new InvalidOperationException("MemoryStore type is Qdrant and Qdrant configuration is null.");
                }

                services.AddSingleton<IMemoryStore>(sp =>
                {
                    HttpClient httpClient = new(new HttpClientHandler { CheckCertificateRevocationList = true });
                    if (!string.IsNullOrWhiteSpace(config.Qdrant.Key))
                    {
                        httpClient.DefaultRequestHeaders.Add("api-key", config.Qdrant.Key);
                    }

                    var endPointBuilder = new UriBuilder(config.Qdrant.Host);
                    endPointBuilder.Port = config.Qdrant.Port;

                    return new QdrantMemoryStore(
                        httpClient: httpClient,
                        config.Qdrant.VectorSize,
                        endPointBuilder.ToString(),
                        loggerFactory: sp.GetRequiredService<ILoggerFactory>()
                    );
                });
                break;

            case MemoryStoreType.AzureCognitiveSearch:
                if (config.AzureCognitiveSearch == null)
                {
                    throw new InvalidOperationException("MemoryStore type is AzureCognitiveSearch and AzureCognitiveSearch configuration is null.");
                }

                services.AddSingleton<IMemoryStore>(sp =>
                {
                    return new AzureCognitiveSearchMemoryStore(config.AzureCognitiveSearch.Endpoint, config.AzureCognitiveSearch.Key);
                });
                break;

            case MemoryStoreOptions.MemoryStoreType.Chroma:
                if (config.Chroma == null)
                {
                    throw new InvalidOperationException("MemoryStore type is Chroma and Chroma configuration is null.");
                }

                services.AddSingleton<IMemoryStore>(sp =>
                {
                    HttpClient httpClient = new(new HttpClientHandler { CheckCertificateRevocationList = true });
                    var endPointBuilder = new UriBuilder(config.Chroma.Host);
                    endPointBuilder.Port = config.Chroma.Port;

                    return new ChromaMemoryStore(
                        httpClient: httpClient,
                        endpoint: endPointBuilder.ToString(),
                        loggerFactory: sp.GetRequiredService<ILoggerFactory>()
                    );
                });
                break;

            case MemoryStoreOptions.MemoryStoreType.Postgres:
                if (config.Postgres == null)
                {
                    throw new InvalidOperationException("MemoryStore type is Postgres and Postgres configuration is null.");
                }

                var dataSourceBuilder = new NpgsqlDataSourceBuilder(config.Postgres.ConnectionString);
                dataSourceBuilder.UseVector();

                services.AddSingleton<IMemoryStore>(sp =>
                {
                    return new PostgresMemoryStore(
                        dataSource: dataSourceBuilder.Build(),
                        vectorSize: config.Postgres.VectorSize
                    );
                });

                break;

            default:
                throw new InvalidOperationException($"Invalid 'MemoryStore' type '{config.Type}'.");
        }

        services.AddScoped<ISemanticTextMemory>(sp => new SemanticTextMemory(
            sp.GetRequiredService<IMemoryStore>(),
            sp.GetRequiredService<IOptions<AIServiceOptions>>().Value
                .ToTextEmbeddingsService(loggerFactory: sp.GetRequiredService<ILoggerFactory>())));
    }

    /// <summary>
    /// Adds Azure Content Safety
    /// </summary>
    internal static void AddContentSafety(this IServiceCollection services)
    {
        IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        ContentSafetyOptions options = configuration.GetSection(ContentSafetyOptions.PropertyName).Get<ContentSafetyOptions>();

        if (options.Enabled)
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
    /// Add the embedding backend to the kernel config
    /// </summary>
    private static KernelBuilder WithEmbeddingBackend(this KernelBuilder kernelBuilder, AIServiceOptions options)
    {
        return options.Type switch
        {
            AIServiceOptions.AIServiceType.AzureOpenAI
                => kernelBuilder.WithAzureTextEmbeddingGenerationService(options.Models.Embedding, options.Endpoint, options.Key),
            AIServiceOptions.AIServiceType.OpenAI
                => kernelBuilder.WithOpenAITextEmbeddingGenerationService(options.Models.Embedding, options.Key),
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
    /// <param name="loggerFactory">Custom <see cref="ILoggerFactory"/> for logging.</param>
    private static ITextEmbeddingGeneration ToTextEmbeddingsService(this AIServiceOptions options,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        return options.Type switch
        {
            AIServiceOptions.AIServiceType.AzureOpenAI
                => new AzureTextEmbeddingGeneration(options.Models.Embedding, options.Endpoint, options.Key, httpClient: httpClient, loggerFactory: loggerFactory),
            AIServiceOptions.AIServiceType.OpenAI
                => new OpenAITextEmbeddingGeneration(options.Models.Embedding, options.Key, httpClient: httpClient, loggerFactory: loggerFactory),
            _
                => throw new ArgumentException("Invalid AIService value in embeddings backend settings"),
        };
    }
}
