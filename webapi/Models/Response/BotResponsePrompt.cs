// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// The fianl prompt sent to generate bot response.
/// </summary>
public class BotResponsePrompt
{
    /// <summary>
    /// The system persona of the chat, includes SystemDescription and SystemResponse components from PromptsOptions.cs.
    /// </summary>
    [JsonPropertyName("systemPersona")]
    public string SystemPersona { get; set; } = string.Empty;

    /// <summary>
    /// Audience extracted from conversation history.
    /// </summary>
    [JsonPropertyName("audience")]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// User intent extracted from input and conversation history.
    /// </summary>
    [JsonPropertyName("userIntent")]
    public string UserIntent { get; set; } = string.Empty;

    /// <summary>
    /// Chat memories queried from the chat memory store if any, includes long term and working memory.
    /// </summary>
    [JsonPropertyName("chatMemories")]
    public string PastMemories { get; set; } = string.Empty;

    /// <summary>
    /// Relevant additional knowledge extracted using a planner.
    /// </summary>
    [JsonPropertyName("externalInformation")]
    public SemanticDependency<StepwiseThoughtProcess> ExternalInformation { get; set; }

    /// <summary>
    /// Most recent messages from chat history.
    /// </summary>
    [JsonPropertyName("chatHistory")]
    public string ChatHistory { get; set; } = string.Empty;

    /// <summary>
    /// Preamble to the LLM's response.
    /// </summary>
    [JsonPropertyName("systemChatContinuation")]
    public string SystemChatContinuation { get; set; } = string.Empty;

    /// <summary>
    /// Raw content of the rendered prompt.
    /// </summary>
    [JsonPropertyName("rawContent")]
    public string RawContent { get; set; } = string.Empty;

    public BotResponsePrompt(
        string rawContent,
        string systemDescription,
        string systemResponse,
        string audience,
        string userIntent,
        string chatMemories,
        string documentMemories,
        SemanticDependency<StepwiseThoughtProcess> externalInformation,
        string chatHistory,
        string systemChatContinuation
        )
    {
        this.RawContent = rawContent;
        this.SystemPersona = string.Join("\n", systemDescription, systemResponse);
        this.Audience = audience;
        this.UserIntent = userIntent;
        this.PastMemories = string.Join("\n", chatMemories, documentMemories).Trim();
        this.ExternalInformation = externalInformation;
        this.ChatHistory = chatHistory;
        this.SystemChatContinuation = systemChatContinuation;
    }
}
