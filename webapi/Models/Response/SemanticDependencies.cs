// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Information about semantic dependencies of the prompt.
/// </summary>
public class SemanticDependency<T>
{
    /// <summary>
    /// Result of the dependency. This is the output that's injected into the prompt.
    /// </summary>
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Context of the dependency. This can be either the prompt template or planner details.
    /// </summary>
    [JsonPropertyName("context")]
    public T? Context { get; set; } = default;

    public SemanticDependency(string result, T? context = default)
    {
        this.Result = result;
        this.Context = context;
    }
}
