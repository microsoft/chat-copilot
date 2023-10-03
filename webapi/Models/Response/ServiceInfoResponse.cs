// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using CopilotChat.WebApi.Options;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Information on running service.
/// </summary>
public class ServiceInfoResponse
{
    /// <summary>
    /// Configured memory store.
    /// </summary>
    [JsonPropertyName("memoryStore")]
    public MemoryStoreInfoResponse MemoryStore { get; set; } = new MemoryStoreInfoResponse();

    /// <summary>
    /// All the available plugins.
    /// </summary>
    [JsonPropertyName("availablePlugins")]
    public IEnumerable<Plugin> AvailablePlugins { get; set; } = Enumerable.Empty<Plugin>();

    /// <summary>
    /// Version of this application.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// True if content safety if enabled, false otherwise.
    /// </summary>
    [JsonPropertyName("isContentSafetyEnabled")]
    public bool IsContentSafetyEnabled { get; set; } = false;
}

/// <summary>
/// Response to memoryStoreType request.
/// </summary>
public class MemoryStoreInfoResponse
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
