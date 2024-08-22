// Copyright (c) Quartech. All rights reserved.

using System.Text.Json.Serialization;

/// <summary>
/// Request definition for search
/// </summary>
namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Request definition for search
/// This model is built by bearing the MVP requirement of supporting simple text based search.
/// </summary>
public class QSearchParameters
{
    [JsonPropertyName("search")]
    public string Search { get; set; } = string.Empty;

    [JsonPropertyName("specializationId")]
    public string SpecializationId { get; set; } = string.Empty;
}
