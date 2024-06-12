// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KernelMemory.Pipeline.Queue.DevTools;

namespace CopilotChat.Shared;

internal sealed class ServiceConfiguration
{
    // Content of appsettings.json, used to access dynamic data under "Services"
    private IConfiguration _rawAppSettings;

    // Normalized configuration
    private KernelMemoryConfig _memoryConfiguration;

    // appsettings.json root node name
    private const string ConfigRoot = "KernelMemory";

    // ASP.NET env var
    private const string AspnetEnvVar = "ASPNETCORE_ENVIRONMENT";

    // OpenAI env var
    private const string OpenAIEnvVar = "OPENAI_API_KEY";

    public ServiceConfiguration(string? settingsDirectory = null)
        : this(ReadAppSettings(settingsDirectory))
    {
    }

    public ServiceConfiguration(IConfiguration rawAppSettings)
        : this(rawAppSettings,
            rawAppSettings.GetSection(ConfigRoot).Get<KernelMemoryConfig>()
            ?? throw new ConfigurationException($"Unable to load Kernel Memory settings from the given configuration. " +
                                                $"There should be a '{ConfigRoot}' root node, " +
                                                $"with data mapping to '{nameof(KernelMemoryConfig)}'"))
    {
    }

    public ServiceConfiguration(
        IConfiguration rawAppSettings,
        KernelMemoryConfig memoryConfiguration)
    {
        this._rawAppSettings = rawAppSettings ?? throw new ConfigurationException("The given app settings configuration is NULL");
        this._memoryConfiguration = memoryConfiguration ?? throw new ConfigurationException("The given memory configuration is NULL");

        if (!this.MinimumConfigurationIsAvailable(false)) { this.SetupForOpenAI(); }

        this.MinimumConfigurationIsAvailable(true);
    }

    public IKernelMemoryBuilder PrepareBuilder(IKernelMemoryBuilder builder)
    {
        return this.BuildUsingConfiguration(builder);
    }

    private IKernelMemoryBuilder BuildUsingConfiguration(IKernelMemoryBuilder builder)
    {
        if (this._memoryConfiguration == null)
        {
            throw new ConfigurationException("The given memory configuration is NULL");
        }

        if (this._rawAppSettings == null)
        {
            throw new ConfigurationException("The given app settings configuration is NULL");
        }

        // Required by ctors expecting KernelMemoryConfig via DI
        builder.AddSingleton(this._memoryConfiguration);

        this.ConfigureMimeTypeDetectionDependency(builder);

        this.ConfigureTextPartitioning(builder);

        this.ConfigureQueueDependency(builder);

        this.ConfigureStorageDependency(builder);

        // The ingestion embedding generators is a list of generators that the "gen_embeddings" handler uses,
        // to generate embeddings for each partition. While it's possible to use multiple generators (e.g. to compare embedding quality)
        // only one generator is used when searching by similarity, and the generator used for search is not in this list.
        // - config.DataIngestion.EmbeddingGeneratorTypes => list of generators, embeddings to generate and store in memory DB
        // - config.Retrieval.EmbeddingGeneratorType      => one embedding generator, used to search, and usually injected into Memory DB constructor

        this.ConfigureIngestionEmbeddingGenerators(builder);

        this.ConfigureSearchClient(builder);

        this.ConfigureRetrievalEmbeddingGenerator(builder);

        // The ingestion Memory DBs is a list of DBs where handlers write records to. While it's possible
        // to write to multiple DBs, e.g. for replication purpose, there is only one Memory DB used to
        // read/search, and it doesn't come from this list. See "config.Retrieval.MemoryDbType".
        // Note: use the aux service collection to avoid mixing ingestion and retrieval dependencies.

        this.ConfigureIngestionMemoryDb(builder);

        this.ConfigureRetrievalMemoryDb(builder);

        this.ConfigureTextGenerator(builder);

        this.ConfigureImageOCR(builder);

        return builder;
    }

    private static IConfiguration ReadAppSettings(string? settingsDirectory)
    {
        var builder = new ConfigurationBuilder();
        builder.AddKMConfigurationSources(settingsDirectory: settingsDirectory);
        return builder.Build();
    }

    private void ConfigureQueueDependency(IKernelMemoryBuilder builder)
    {
        if (string.Equals(this._memoryConfiguration.DataIngestion.OrchestrationType, "Distributed", StringComparison.OrdinalIgnoreCase))
        {
            switch (this._memoryConfiguration.DataIngestion.DistributedOrchestration.QueueType)
            {
                case string y1 when y1.Equals("AzureQueue", StringComparison.OrdinalIgnoreCase):
                case string y2 when y2.Equals("AzureQueues", StringComparison.OrdinalIgnoreCase):
                    // Check 2 keys for backward compatibility
                    builder.Services.AddAzureQueuesOrchestration(this.GetServiceConfig<AzureQueuesConfig>("AzureQueue"));
                    break;

                case string y when y.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase):
                    // Check 2 keys for backward compatibility
                    builder.Services.AddRabbitMQOrchestration(this.GetServiceConfig<RabbitMqConfig>("RabbitMq"));
                    break;

                case string y when y.Equals("SimpleQueues", StringComparison.OrdinalIgnoreCase):
                    builder.Services.AddSimpleQueues(this.GetServiceConfig<SimpleQueuesConfig>("SimpleQueues"));
                    break;

                default:
                    // NOOP - allow custom implementations, via WithCustomIngestionQueueClientFactory()
                    break;
            }
        }
    }

    private void ConfigureStorageDependency(IKernelMemoryBuilder builder)
    {
        switch (this._memoryConfiguration.DocumentStorageType)
        {
            case string x1 when x1.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase):
            case string x2 when x2.Equals("AzureBlobs", StringComparison.OrdinalIgnoreCase):
                // Check 2 keys for backward compatibility
                builder.WithAzureBlobsDocumentStorage(this.GetServiceConfig<AzureBlobsConfig>("AzureBlobs")
                                                      ?? this.GetServiceConfig<AzureBlobsConfig>("AzureBlob"));
                break;

            case string x when x.Equals("SimpleFileStorage", StringComparison.OrdinalIgnoreCase):
                builder.WithSimpleFileStorage(this.GetServiceConfig<SimpleFileStorageConfig>("SimpleFileStorage"));
                break;

            default:
                // NOOP - allow custom implementations, via WithCustomStorage()
                break;
        }
    }

    private void ConfigureTextPartitioning(IKernelMemoryBuilder builder)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (this._memoryConfiguration.DataIngestion.TextPartitioning != null)
        {
            this._memoryConfiguration.DataIngestion.TextPartitioning.Validate();
            builder.WithCustomTextPartitioningOptions(this._memoryConfiguration.DataIngestion.TextPartitioning);
        }
    }

    private void ConfigureMimeTypeDetectionDependency(IKernelMemoryBuilder builder)
    {
        builder.WithDefaultMimeTypeDetection();
    }

    private void ConfigureIngestionEmbeddingGenerators(IKernelMemoryBuilder builder)
    {
        // Note: using multiple embeddings is not fully supported yet and could cause write errors or incorrect search results
        if (this._memoryConfiguration.DataIngestion.EmbeddingGeneratorTypes.Count > 1)
        {
            throw new NotSupportedException("Using multiple embedding generators is currently unsupported. " +
                                            "You may contact the team if this feature is required, or workaround this exception " +
                                            "using KernelMemoryBuilder methods explicitly.");
        }

        foreach (var type in this._memoryConfiguration.DataIngestion.EmbeddingGeneratorTypes)
        {
            switch (type)
            {
                case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
                case string y when y.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                {
                    var instance = this.GetServiceInstance<ITextEmbeddingGenerator>(builder,
                        s => s.AddAzureOpenAIEmbeddingGeneration(this.GetServiceConfig<AzureOpenAIConfig>("AzureOpenAIEmbedding")));
                    builder.AddIngestionEmbeddingGenerator(instance);
                    break;
                }

                case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                {
                    var instance = this.GetServiceInstance<ITextEmbeddingGenerator>(builder,
                        s => s.AddOpenAITextEmbeddingGeneration(this.GetServiceConfig<OpenAIConfig>("OpenAI")));
                    builder.AddIngestionEmbeddingGenerator(instance);
                    break;
                }

                default:
                    // NOOP - allow custom implementations, via WithCustomEmbeddingGeneration()
                    break;
            }
        }
    }

    private void ConfigureIngestionMemoryDb(IKernelMemoryBuilder builder)
    {
        foreach (var type in this._memoryConfiguration.DataIngestion.MemoryDbTypes)
        {
            switch (type)
            {
                default:
                    throw new ConfigurationException(
                        $"Unknown Memory DB option '{type}'. " +
                        "To use a custom Memory DB, set the configuration value to an empty string, " +
                        "and inject the custom implementation using `IKernelMemoryBuilder.WithCustomMemoryDb(...)`");

                case "":
                    // NOOP - allow custom implementations, via WithCustomMemoryDb()
                    break;

                case string x when x.Equals("AzureAISearch", StringComparison.OrdinalIgnoreCase):
                {
                    var instance = this.GetServiceInstance<IMemoryDb>(builder,
                        s => s.AddAzureAISearchAsMemoryDb(this.GetServiceConfig<AzureAISearchConfig>("AzureAISearch"))
                    );
                    builder.AddIngestionMemoryDb(instance);
                    break;
                }

                case string x when x.Equals("Qdrant", StringComparison.OrdinalIgnoreCase):
                {
                    var instance = this.GetServiceInstance<IMemoryDb>(builder,
                        s => s.AddQdrantAsMemoryDb(this.GetServiceConfig<QdrantConfig>("Qdrant"))
                    );
                    builder.AddIngestionMemoryDb(instance);
                    break;
                }

                case string x when x.Equals("Postgres", StringComparison.OrdinalIgnoreCase):
                {
                    var instance = this.GetServiceInstance<IMemoryDb>(builder,
                        s => s.AddPostgresAsMemoryDb(this.GetServiceConfig<PostgresConfig>("Postgres"))
                    );
                    builder.AddIngestionMemoryDb(instance);
                    break;
                }

                case string x when x.Equals("Redis", StringComparison.OrdinalIgnoreCase):
                {
                    var instance = this.GetServiceInstance<IMemoryDb>(builder,
                        s => s.AddRedisAsMemoryDb(this.GetServiceConfig<RedisConfig>("Redis"))
                    );
                    builder.AddIngestionMemoryDb(instance);
                    break;
                }

                case string x when x.Equals("SimpleVectorDb", StringComparison.OrdinalIgnoreCase):
                {
                    var instance = this.GetServiceInstance<IMemoryDb>(builder,
                        s => s.AddSimpleVectorDbAsMemoryDb(this.GetServiceConfig<SimpleVectorDbConfig>("SimpleVectorDb"))
                    );
                    builder.AddIngestionMemoryDb(instance);
                    break;
                }

                case string x when x.Equals("SimpleTextDb", StringComparison.OrdinalIgnoreCase):
                {
                    var instance = this.GetServiceInstance<IMemoryDb>(builder,
                        s => s.AddSimpleTextDbAsMemoryDb(this.GetServiceConfig<SimpleTextDbConfig>("SimpleTextDb"))
                    );
                    builder.AddIngestionMemoryDb(instance);
                    break;
                }
            }
        }
    }

    private void ConfigureSearchClient(IKernelMemoryBuilder builder)
    {
        // Search settings
        builder.WithSearchClientConfig(this._memoryConfiguration.Retrieval.SearchClient);
    }

    private void ConfigureRetrievalEmbeddingGenerator(IKernelMemoryBuilder builder)
    {
        // Retrieval embeddings - ITextEmbeddingGeneration interface
        switch (this._memoryConfiguration.Retrieval.EmbeddingGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddAzureOpenAIEmbeddingGeneration(this.GetServiceConfig<AzureOpenAIConfig>("AzureOpenAIEmbedding"));
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddOpenAITextEmbeddingGeneration(this.GetServiceConfig<OpenAIConfig>("OpenAI"));
                break;

            default:
                // NOOP - allow custom implementations, via WithCustomEmbeddingGeneration()
                break;
        }
    }

    private void ConfigureRetrievalMemoryDb(IKernelMemoryBuilder builder)
    {
        // Retrieval Memory DB - IMemoryDb interface
        switch (this._memoryConfiguration.Retrieval.MemoryDbType)
        {
            case string x when x.Equals("AzureAISearch", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddAzureAISearchAsMemoryDb(this.GetServiceConfig<AzureAISearchConfig>("AzureAISearch"));
                break;

            case string x when x.Equals("Qdrant", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddQdrantAsMemoryDb(this.GetServiceConfig<QdrantConfig>("Qdrant"));
                break;

            case string x when x.Equals("Postgres", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddPostgresAsMemoryDb(this.GetServiceConfig<PostgresConfig>("Postgres"));
                break;

            case string x when x.Equals("Redis", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddRedisAsMemoryDb(this.GetServiceConfig<RedisConfig>("Redis"));
                break;

            case string x when x.Equals("SimpleVectorDb", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddSimpleVectorDbAsMemoryDb(this.GetServiceConfig<SimpleVectorDbConfig>("SimpleVectorDb"));
                break;

            case string x when x.Equals("SimpleTextDb", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddSimpleTextDbAsMemoryDb(this.GetServiceConfig<SimpleTextDbConfig>("SimpleTextDb"));
                break;

            default:
                // NOOP - allow custom implementations, via WithCustomMemoryDb()
                break;
        }
    }

    private void ConfigureTextGenerator(IKernelMemoryBuilder builder)
    {
        // Text generation
        switch (this._memoryConfiguration.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddAzureOpenAITextGeneration(this.GetServiceConfig<AzureOpenAIConfig>("AzureOpenAIText"));
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddOpenAITextGeneration(this.GetServiceConfig<OpenAIConfig>("OpenAI"));
                break;

            default:
                // NOOP - allow custom implementations, via WithCustomTextGeneration()
                break;
        }
    }

    private void ConfigureImageOCR(IKernelMemoryBuilder builder)
    {
        // Image OCR
        switch (this._memoryConfiguration.DataIngestion.ImageOcrType)
        {
            case string y when string.IsNullOrWhiteSpace(y):
            case string x when x.Equals("None", StringComparison.OrdinalIgnoreCase):
                break;

            case string x when x.Equals("AzureAIDocIntel", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddAzureAIDocIntel(this.GetServiceConfig<AzureAIDocIntelConfig>("AzureAIDocIntel"));
                break;

            default:
                // NOOP - allow custom implementations, via WithCustomImageOCR()
                break;
        }
    }

    /// <summary>
    /// Check the configuration for minimum requirements
    /// </summary>
    /// <param name="throwOnError">Whether to throw or return false when the config is incomplete</param>
    /// <returns>Whether the configuration is valid</returns>
    private bool MinimumConfigurationIsAvailable(bool throwOnError)
    {
        // Check if text generation settings
        if (string.IsNullOrEmpty(this._memoryConfiguration.TextGeneratorType))
        {
            if (!throwOnError) { return false; }

            throw new ConfigurationException("Text generation (TextGeneratorType) is not configured in Kernel Memory.");
        }

        // Check embedding generation ingestion settings
        if (this._memoryConfiguration.DataIngestion.EmbeddingGenerationEnabled)
        {
            if (this._memoryConfiguration.DataIngestion.EmbeddingGeneratorTypes.Count == 0)
            {
                if (!throwOnError) { return false; }

                throw new ConfigurationException("Data ingestion embedding generation (DataIngestion.EmbeddingGeneratorTypes) is not configured in Kernel Memory.");
            }
        }

        // Check embedding generation retrieval settings
        if (string.IsNullOrEmpty(this._memoryConfiguration.Retrieval.EmbeddingGeneratorType))
        {
            if (!throwOnError) { return false; }

            throw new ConfigurationException("Retrieval embedding generation (Retrieval.EmbeddingGeneratorType) is not configured in Kernel Memory.");
        }

        return true;
    }

    /// <summary>
    /// Rewrite configuration using OpenAI, if possible.
    /// </summary>
    private void SetupForOpenAI()
    {
        string openAIKey = Environment.GetEnvironmentVariable(OpenAIEnvVar)?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(openAIKey))
        {
            return;
        }

        var inMemoryConfig = new Dictionary<string, string?>
        {
            { $"{ConfigRoot}:Services:OpenAI:APIKey", openAIKey },
            { $"{ConfigRoot}:TextGeneratorType", "OpenAI" },
            { $"{ConfigRoot}:DataIngestion:EmbeddingGeneratorTypes:0", "OpenAI" },
            { $"{ConfigRoot}:Retrieval:EmbeddingGeneratorType", "OpenAI" }
        };

        var newAppSettings = new ConfigurationBuilder();
        newAppSettings.AddConfiguration(this._rawAppSettings);
        newAppSettings.AddInMemoryCollection(inMemoryConfig);

        this._rawAppSettings = newAppSettings.Build();
        this._memoryConfiguration = this._rawAppSettings.GetSection(ConfigRoot).Get<KernelMemoryConfig>()!;
    }

    /// <summary>
    /// Get an instance of T, using dependencies available in the builder,
    /// except for existing service descriptors for T. Replace/Use the
    /// given action to define T's implementation.
    /// Return an instance of T built using the definition provided by
    /// the action.
    /// </summary>
    /// <param name="builder">KM builder</param>
    /// <param name="addCustomService">Action used to configure the service collection</param>
    /// <typeparam name="T">Target type/interface</typeparam>
    private T GetServiceInstance<T>(IKernelMemoryBuilder builder, Action<IServiceCollection> addCustomService)
    {
        // Clone the list of service descriptors, skipping T descriptor
        IServiceCollection services = new ServiceCollection();
        foreach (ServiceDescriptor d in builder.Services)
        {
            if (d.ServiceType == typeof(T)) { continue; }

            services.Add(d);
        }

        // Add the custom T descriptor
        addCustomService.Invoke(services);

        // Build and return an instance of T, as defined by `addCustomService`
        return services.BuildServiceProvider().GetService<T>()
               ?? throw new ConfigurationException($"Unable to build {nameof(T)}");
    }

    /// <summary>
    /// Read a dependency configuration from IConfiguration
    /// Data is usually retrieved from KernelMemory:Services:{serviceName}, e.g. when using appsettings.json
    /// {
    ///   "KernelMemory": {
    ///     "Services": {
    ///       "{serviceName}": {
    ///         ...
    ///         ...
    ///       }
    ///     }
    ///   }
    /// }
    /// </summary>
    /// <param name="serviceName">Name of the dependency</param>
    /// <typeparam name="T">Type of configuration to return</typeparam>
    /// <returns>Configuration instance, settings for the dependency specified</returns>
    private T GetServiceConfig<T>(string serviceName)
    {
        return this._memoryConfiguration.GetServiceConfig<T>(this._rawAppSettings, serviceName);
    }
}
