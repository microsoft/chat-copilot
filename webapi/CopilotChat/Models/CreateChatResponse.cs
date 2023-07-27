// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace SemanticKernel.Service.CopilotChat.Models;

/// <summary>
/// Response to chatSession/create request.
/// </summary>
public class CreateChatResponse
{
    /// <summary>
    /// Chat ID that is persistent and unique to.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Title of the chat.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; }

    /// <summary>
    /// Initial bot message.
    /// </summary>
    [JsonPropertyName("initialBotMessage")]
    public ChatMessage? InitialBotMessage { get; set; }

    public CreateChatResponse(ChatSession chatSession, ChatMessage initialBotMessage)
    {
        this.Id = chatSession.Id;
        this.Title = chatSession.Title;
        this.InitialBotMessage = initialBotMessage;
    }
}
