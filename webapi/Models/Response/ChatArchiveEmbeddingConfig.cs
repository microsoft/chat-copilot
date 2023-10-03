// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Chat archive embedding configuration.
/// </summary>
public class ChatArchiveEmbeddingConfig
{
    /// <summary>
    /// Supported types of AI services.
    /// </summary>
    public enum AIServiceType
    {
        /// <summary>
        /// Azure OpenAI https://learn.microsoft.com/en-us/azure/cognitive-services/openai/
        /// </summary>
        AzureOpenAIEmbedding,

        /// <summary>
        /// OpenAI https://openai.com/
        /// </summary>
        OpenAI
    }

    /// <summary>
    /// The AI service.
    /// </summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AIServiceType AIService { get; set; } = AIServiceType.AzureOpenAIEmbedding;

    /// <summary>
    /// The deployment or the model id.
    /// </summary>
    public string DeploymentOrModelId { get; set; } = string.Empty;
}
