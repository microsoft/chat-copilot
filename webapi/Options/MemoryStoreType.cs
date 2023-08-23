// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Options;

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
    /// Cosmos DB persistent memory store.
    /// </summary>
    Postgres,
}
