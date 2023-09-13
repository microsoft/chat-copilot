// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticMemory;
using Microsoft.SemanticMemory.MemoryStorage.DevTools;

namespace CopilotChat.WebApi.Options;

/// <summary>
/// The type of memory store to use.
/// </summary>
public enum MemoryStoreType
{
    /// <summary>
    /// In-memory volatile memory store.
    /// </summary>
    Volatile,

    /// <summary>
    /// File system based persistent memory store.
    /// </summary>
    TextFile,

    /// <summary>
    /// Qdrant based persistent memory store.
    /// </summary>
    Qdrant,

    /// <summary>
    /// Azure Cognitive Search persistent memory store.
    /// </summary>
    AzureCognitiveSearch,
}

public static class MemoryStoreTypeExtensions
{
    /// <summary>
    /// Gets the memory store type from the configuration.
    /// Volatile and TextFile are storage solutions in SimpleVectorDb.
    /// If SimpleVectorDb is configured, then the storage type is determined by the SimpleVectorDb configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The memory store type.</returns>
    public static MemoryStoreType GetMemoryStoreType(this SemanticMemoryConfig memoryOptions, IConfiguration configuration)
    {
        var type = memoryOptions.Retrieval.VectorDbType;
        if (type.Equals("AzureCognitiveSearch", StringComparison.OrdinalIgnoreCase))
        {
            return MemoryStoreType.AzureCognitiveSearch;
        }
        else if (type.Equals("Qdrant", StringComparison.OrdinalIgnoreCase))
        {
            return MemoryStoreType.Qdrant;
        }
        else if (type.Equals("SimpleVectorDb", StringComparison.OrdinalIgnoreCase))
        {
            var simpleVectorDbConfig = memoryOptions.GetServiceConfig<SimpleVectorDbConfig>(configuration, "SimpleVectorDb");
            if (simpleVectorDbConfig != null)
            {
                type = simpleVectorDbConfig.StorageType.ToString();
                if (type.Equals("Volatile", StringComparison.OrdinalIgnoreCase))
                {
                    return MemoryStoreType.Volatile;
                }
                else if (type.Equals("TextFile", StringComparison.OrdinalIgnoreCase))
                {
                    return MemoryStoreType.TextFile;
                }
            }
        }

        throw new ArgumentException($"Invalid memory store type: {type}");
    }
}
