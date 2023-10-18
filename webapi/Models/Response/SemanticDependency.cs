// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Response;

public interface ISemanticDependency
{
    /// <summary>
    /// Result of the dependency. This is the output that's injected into the prompt.
    /// </summary>
    [JsonPropertyName("result")]
    string Result { get; }

    /// <summary>
    /// Type of dependency, if any.
    /// </summary>
    [JsonPropertyName("type")]
    string? Type { get; }
}

/// <summary>
/// Information about semantic dependencies of the prompt.
/// </summary>
public class SemanticDependency<T> : ISemanticDependency
{
    /// <inheritdoc/>
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Context of the dependency. This can be either the prompt template or planner details.
    /// </summary>
    [JsonPropertyName("context")]
    public T? Context { get; set; } = default;

    public SemanticDependency(string result, T? context = default, string? type = null)
    {
        this.Result = result;
        this.Context = context;
        this.Type = type ?? typeof(T).Name;
    }
}
