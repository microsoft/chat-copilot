// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Skills.ChatSkills;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.Builder;
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
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Skills.Core;
using Microsoft.SemanticMemory;
using Microsoft.SemanticMemory.MemoryStorage.Qdrant;

namespace CopilotChat.WebApi.Extensions;

/// <summary>
/// Extension methods for registering Semantic Kernel related services.
/// </summary>
internal static class SemanticKernelExtensions
{
    private const int VectorMemoryDimensions = 1536; // $$$ TODO: Consolidate with semantic memory

    /// <summary>
    /// Delegate to register skills with a Semantic Kernel
    /// </summary>
    public delegate Task RegisterSkillsWithKernel(IServiceProvider sp, IKernel kernel);

    /// <summary>
    /// Add Semantic Kernel services
    /// </summary>
    internal static WebApplicationBuilder AddSemanticKernelServices(this WebApplicationBuilder builder)
    {
        // Semantic Memory (for Planner)
        builder.Services.AddSemanticTextMemory(builder.Configuration);

        // Semantic Kernel
        builder.Services.AddScoped<IKernel>(
            sp =>
            {
                var kernel = Kernel.Builder
                    .WithLoggerFactory(sp.GetRequiredService<ILoggerFactory>())
                    .WithMemory(sp.GetRequiredService<ISemanticTextMemory>())
                    .WithCompletionBackend(sp, builder.Configuration)
                    .Build();

                sp.GetRequiredService<RegisterSkillsWithKernel>()(sp, kernel);
                return kernel;
            });

        // Azure Content Safety
        builder.Services.AddContentSafety();

        // Register skills
        builder.Services.AddScoped<RegisterSkillsWithKernel>(sp => RegisterSkillsAsync);

        return builder;
    }

    /// <summary>
    /// Add Planner services
    /// </summary>
    public static WebApplicationBuilder AddPlannerServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<CopilotChatPlanner>(sp =>
        {
            sp.WithBotConfig(builder.Configuration);

            var plannerOptions = sp.GetRequiredService<IOptions<PlannerOptions>>();

            var plannerKernel = Kernel.Builder
                .WithLoggerFactory(sp.GetRequiredService<ILoggerFactory>())
                .WithMemory(sp.GetRequiredService<ISemanticTextMemory>())
                .WithPlannerBackend(sp, builder.Configuration)
                .Build();

            return new CopilotChatPlanner(plannerKernel, plannerOptions?.Value, sp.GetRequiredService<ILogger<CopilotChatPlanner>>());
        });

        // Register Planner skills (AI plugins) here.
        // TODO: [sk Issue #2046] Move planner skill registration from ChatController to this location.

        return builder;
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
            var logger = context.LoggerFactory.CreateLogger(nameof(SKContext));
            logger.LogError(context.LastException, "{0}", context.LastException?.Message);
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
    /// Add the semantic memory used by the planner.
    /// </summary>
    private static void AddSemanticTextMemory(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISemanticTextMemory>(
            sp =>
                new SemanticTextMemory(
                    sp.GetRequiredService<IMemoryStore>(),
                    sp.ToTextEmbeddingsService(configuration)));

        services.AddSingleton<IMemoryStore>(
            sp =>
            {
                var configMemory = sp.GetRequiredService<IOptions<SemanticMemoryConfig>>().Value;

                var memoryType = Enum.Parse<MemoryStoreType>(configMemory.Retrieval.VectorDbType, ignoreCase: true);
                switch (memoryType)
                {
                    //case MemoryStoreType.Volatile: // $$$ TODO: Verify needed for planner ???
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
                            VectorMemoryDimensions,
                            configStorage.Endpoint,
                            loggerFactory: sp.GetRequiredService<ILoggerFactory>()
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
    private static KernelBuilder WithCompletionBackend(this KernelBuilder kernelBuilder, IServiceProvider provider, IConfiguration configuration)
    {
        var memoryOptions = provider.GetRequiredService<IOptions<SemanticMemoryConfig>>().Value;

        switch (memoryOptions.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(configuration, "AzureOpenAIText");
                return kernelBuilder.WithAzureChatCompletionService(azureAIOptions.Deployment, azureAIOptions.Endpoint, azureAIOptions.APIKey);

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");
                return kernelBuilder.WithOpenAIChatCompletionService(openAIOptions.TextModel, openAIOptions.APIKey);

            default:
                throw new ArgumentException($"Invalid {nameof(memoryOptions.TextGeneratorType)} value in 'SemanticMemory' settings.");
        }
    }

    /// <summary>
    /// Add the completion backend to the kernel config for the planner.
    /// </summary>
    private static KernelBuilder WithPlannerBackend(this KernelBuilder kernelBuilder, IServiceProvider provider, IConfiguration configuration)
    {
        var memoryOptions = provider.GetRequiredService<IOptions<SemanticMemoryConfig>>().Value;
        var plannerOptions = provider.GetRequiredService<IOptions<PlannerOptions>>().Value;

        switch (memoryOptions.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(configuration, "AzureOpenAIText");
                return kernelBuilder.WithAzureChatCompletionService(plannerOptions.Model, azureAIOptions.Endpoint, azureAIOptions.APIKey);

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");
                return kernelBuilder.WithOpenAIChatCompletionService(plannerOptions.Model, openAIOptions.APIKey);

            default:
                throw new ArgumentException($"Invalid {nameof(memoryOptions.TextGeneratorType)} value in 'SemanticMemory' settings.");
        }
    }

    /// <summary>
    /// Construct IEmbeddingGeneration from <see cref="AIServiceOptions"/>
    /// </summary>
    private static ITextEmbeddingGeneration ToTextEmbeddingsService(
        this IServiceProvider provider,
        IConfiguration configuration,
        ILoggerFactory? loggerFactory = null)
    {
        var logger = provider.GetRequiredService<ILogger<ITextEmbeddingGeneration>>();
        var memoryOptions = provider.GetRequiredService<IOptions<SemanticMemoryConfig>>().Value;

        switch (memoryOptions.Retrieval.EmbeddingGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(configuration, "AzureOpenAIEmbedding");
                return new AzureTextEmbeddingGeneration(azureAIOptions.Deployment, azureAIOptions.Endpoint, azureAIOptions.APIKey, httpClient: null, loggerFactory);

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");
                return new OpenAITextEmbeddingGeneration(openAIOptions.EmbeddingModel, openAIOptions.APIKey, organization: null, httpClient: null, loggerFactory);

            default:
                throw new ArgumentException($"Invalid {nameof(memoryOptions.Retrieval.EmbeddingGeneratorType)} value in 'SemanticMemory' settings.");
        }
    }

    /// <summary>
    /// Get the embedding model from the configuration.
    /// </summary>
    private static BotEmbeddingConfig WithBotConfig(this IServiceProvider provider, IConfiguration configuration)
    {
        var memoryOptions = provider.GetRequiredService<IOptions<SemanticMemoryConfig>>().Value;

        switch (memoryOptions.Retrieval.EmbeddingGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(configuration, "AzureOpenAIEmbedding");
                return
                    new BotEmbeddingConfig
                    {
                        AIService = BotEmbeddingConfig.AIServiceType.AzureOpenAIEmbedding,
                        DeploymentOrModelId = azureAIOptions.Deployment,
                    };

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");
                return
                    new BotEmbeddingConfig
                    {
                        AIService = BotEmbeddingConfig.AIServiceType.OpenAI,
                        DeploymentOrModelId = openAIOptions.EmbeddingModel,
                    };

            default:
                throw new ArgumentException($"Invalid {nameof(memoryOptions.Retrieval.EmbeddingGeneratorType)} value in 'SemanticMemory' settings.");
        }
    }
}
