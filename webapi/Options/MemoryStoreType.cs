// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Options;

/// <summary>
/// The type of memory store to use.
/// </summary>
public enum MemoryStoreType
{
    /// <summary>
    /// File system based persistent memory store.
    /// </summary>
    SimpleVectorDb,

    /// <summary>
    /// Qdrant based persistent memory store.
    /// </summary>
    Qdrant,

    /// <summary>
    /// Azure Cognitive Search persistent memory store.
    /// </summary>
    AzureCognitiveSearch,
}
