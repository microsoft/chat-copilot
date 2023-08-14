// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Response given by <seealso cref="ServiceOptionsController"/>
/// </summary>
public class ServiceOptionsResponse
{
    /// <summary>
    /// Dictionary of values to return to caller.
    /// </summary>
    [JsonPropertyName("values")]
    public Dictionary<string, string> Values { get; set; } = new();
}
