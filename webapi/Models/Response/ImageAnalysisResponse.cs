// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using CopilotChat.WebApi.Services;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Response definition to the /contentsafety/image:analyze 
/// endpoint made by the AzureContentModerator.
/// </summary>
public class ImageAnalysisResponse
{
    /// <summary>
    /// Gets or sets the AnalysisResult related to hate.
    /// </summary>
    [JsonPropertyName("hateAnalysisResult")]
    public AnalysisResult? HateAnalysisResult { get; set; }

    /// <summary>
    /// Gets or sets the AnalysisResult related to self-harm.
    /// </summary>
    [JsonPropertyName("selfHarmAnalysisResult")]
    public AnalysisResult? SelfHarmAnalysisResult { get; set; }

    /// <summary>
    /// Gets or sets the AnalysisResult related to sexual content.
    /// </summary>
    [JsonPropertyName("sexualAnalysisResult")]
    public AnalysisResult? SexualAnalysisResult { get; set; }

    /// <summary>
    /// Gets or sets the AnalysisResult related to violence.
    /// </summary>
    [JsonPropertyName("violenceAnalysisResult")]
    public AnalysisResult? ViolenceAnalysisResult { get; set; }
}

