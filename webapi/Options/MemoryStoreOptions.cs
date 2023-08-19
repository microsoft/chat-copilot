// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Options;

/// <summary>
/// Configuration settings for the memory store.
/// </summary>
public class MemoryStoreOptions
{
    public const string PropertyName = "MemoryStore";

    /// <summary>
    /// The type of memory store to use.
    /// </summary>
    public enum MemoryStoreType
    {
        /// <summary>
        /// Non-persistent memory store.
        /// </summary>
        Volatile,

        /// <summary>
        /// Qdrant based persistent memory store.
        /// </summary>
        Qdrant,

        /// <summary>
        /// Azure Cognitive Search persistent memory store.
        /// </summary>
        AzureCognitiveSearch,

        /// <summary>
        /// Chroma DB persistent memory store.
        /// </summary>
        Chroma,

        /// <summary>
        /// Postgres DB persistent memory store.
        /// </summary>
        Postgres,
    }

    /// <summary>
    /// Gets or sets the type of memory store to use.
    /// </summary>
    public MemoryStoreType Type { get; set; } = MemoryStoreType.Volatile;

    /// <summary>
    /// Gets or sets the configuration for the Qdrant memory store.
    /// </summary>
    [RequiredOnPropertyValue(nameof(Type), MemoryStoreType.Qdrant)]
    public QdrantOptions? Qdrant { get; set; }

    /// <summary>
    /// Gets or sets the configuration for the Chroma memory store.
    /// </summary>
    [RequiredOnPropertyValue(nameof(Type), MemoryStoreType.Chroma)]
    public VectorMemoryWebOptions? Chroma { get; set; }

    /// <summary>
    /// Gets or sets the configuration for the Azure Cognitive Search memory store.
    /// </summary>
    [RequiredOnPropertyValue(nameof(Type), MemoryStoreType.AzureCognitiveSearch)]
    public AzureCognitiveSearchOptions? AzureCognitiveSearch { get; set; }

    /// <summary>
    /// Gets or sets the configuration for the Postgres memory store.
    /// </summary>
    [RequiredOnPropertyValue(nameof(Type), MemoryStoreType.Postgres)]
    public PostgresOptions? Postgres { get; set; }
}
