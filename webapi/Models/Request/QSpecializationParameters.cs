// Copyright (c) Quartech. All rights reserved.

using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Request definition for Specialization
/// </summary>
public class QSpecializationParameters
{
    /// <summary>
    /// Key of the specialization
    /// </summary>
    [JsonPropertyName("key")]
    public string key { get; set; } = string.Empty;

    /// <summary>
    /// Name of the specialization
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the specialization
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// RoleInformation of the specialization
    /// </summary>
    [JsonPropertyName("roleInformation")]
    public string RoleInformation { get; set; } = string.Empty;

    /// <summary>
    /// IndexName of the specialization
    /// </summary>
    [JsonPropertyName("indexName")]
    public string IndexName { get; set; } = string.Empty;

    /// <summary>
    /// Image FilePath of the specialization.
    /// </summary>
    [JsonPropertyName("imageFilePath")]
    public string ImageFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Enable/Disable flag of the specialization.
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool isActive { get; set; } = true;
}
