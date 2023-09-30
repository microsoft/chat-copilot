// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using CopilotChat.WebApi.Services;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Response definition to image content safety analysis requests.
/// endpoint made by the AzureContentSafety.
/// </summary>
public class ImageAnalysisResponse
{
    /// <summary>
    /// Gets or sets the AnalysisResult related to hate.
    /// </summary>
    [JsonPropertyName("hateResult")]
    public AnalysisResult? HateResult { get; set; }

    /// <summary>
    /// Gets or sets the AnalysisResult related to self-harm.
    /// </summary>
    [JsonPropertyName("selfHarmResult")]
    public AnalysisResult? SelfHarmResult { get; set; }

    /// <summary>
    /// Gets or sets the AnalysisResult related to sexual content.
    /// </summary>
    [JsonPropertyName("sexualResult")]
    public AnalysisResult? SexualResult { get; set; }

    /// <summary>
    /// Gets or sets the AnalysisResult related to violence.
    /// </summary>
    [JsonPropertyName("violenceResult")]
    public AnalysisResult? ViolenceResult { get; set; }
}
