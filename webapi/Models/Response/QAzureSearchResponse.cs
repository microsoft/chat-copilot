// Copyright (c) Quartech. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Response definition for AzureAIsearch response
/// This model is built with AzureAISearch response structure as base model.
/// </summary>
public class QAzureSearchResponse
{
    [JsonPropertyName("@odata.count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public List<QSearchValue> values { get; set; } = new List<QSearchValue>();
}

public class QSearchValue
{
    [JsonPropertyName("@search.highlights")]
    public QSearchHighlight highlights { get; set; } = new QSearchHighlight();

    [JsonPropertyName("url")]
#pragma warning disable CA1056 // URI-like properties should not be strings
    public string url { get; set; } = string.Empty;
#pragma warning restore CA1056 // URI-like properties should not be strings

    [JsonPropertyName("filepath")]
    public string filename { get; set; } = string.Empty;

    public string id { get; set; } = string.Empty;

    [JsonPropertyName("meta_json_string")]
    public string metaJsonString { get; set; } = string.Empty;
}

public class QSearchHighlight
{
    [JsonPropertyName("content")]
    public List<string> content { get; set; } = new List<string>();
}
