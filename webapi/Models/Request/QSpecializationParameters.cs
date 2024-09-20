// Copyright (c) Quartech. All rights reserved.

using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Request definition for Specialization
/// </summary>
public class QSpecializationParameters : QSpecializationBase
{
    /// <summary>
    /// Image FilePath of the specialization.
    /// </summary>
    [JsonPropertyName("imageFilePath")]
    public string? ImageFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Image FilePath of the specialization.
    /// </summary>
    [JsonPropertyName("iconFilePath")]
    public string IconFilePath { get; set; } = string.Empty;
}
