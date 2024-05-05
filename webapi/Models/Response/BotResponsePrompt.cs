// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// The final prompt sent to generate bot response.
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
    /// Most recent messages from chat history.
    /// </summary>
    [JsonPropertyName("chatHistory")]
    public string ChatHistory { get; set; } = string.Empty;

    /// <summary>
    /// The collection of context messages associated with this chat completions request.
    /// See https://learn.microsoft.com/en-us/dotnet/api/azure.ai.openai.chatcompletionsoptions.messages?view=azure-dotnet-preview#azure-ai-openai-chatcompletionsoptions-messages.
    /// </summary>
    [JsonPropertyName("metaPromptTemplate")]
    public ChatHistory MetaPromptTemplate { get; set; } = new();

    public BotResponsePrompt(
        string systemInstructions,
        string audience,
        string userIntent,
        string chatMemories,
        string chatHistory,
        ChatHistory metaPromptTemplate
    )
    {
        this.SystemPersona = systemInstructions;
        this.Audience = audience;
        this.UserIntent = userIntent;
        this.PastMemories = chatMemories;
        this.ChatHistory = chatHistory;
        this.MetaPromptTemplate = metaPromptTemplate;
    }
}
