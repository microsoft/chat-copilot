// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Information on running service.
/// </summary>
public class ServiceOptionsResponse
{
    /// <summary>
    /// Configured memory store.
    /// </summary>
    [JsonPropertyName("memoryStore")]
    public MemoryStoreOptionResponse MemoryStore { get; set; } = new MemoryStoreOptionResponse();

    /// <summary>
    /// Version of this application.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// True if content safety if enabled, false otherwise.
    /// </summary>
    [JsonPropertyName("contentSafetyStatus")]
    public bool IsContentSafetyEnabled { get; set; } = false;
}

/// <summary>
/// Response to memoryStoreType request.
/// </summary>
public class MemoryStoreOptionResponse
{
    /// <summary>
    /// All the available memory store types.
    /// </summary>
    [JsonPropertyName("types")]
    public IEnumerable<string> Types { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// The selected memory store type.
    /// </summary>
    [JsonPropertyName("selectedType")]
    public string SelectedType { get; set; } = string.Empty;
}
