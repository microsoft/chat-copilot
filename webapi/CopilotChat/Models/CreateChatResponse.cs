// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace SemanticKernel.Service.CopilotChat.Models;

/// <summary>
/// Response object definition to the 'chatSession/create' request.
/// This groups the initial bot message with the chat session
/// to avoid making two requests.
/// </summary>
public class CreateChatResponse
{
    /// <summary>
    /// The chat session that was created.
    /// </summary>
    [JsonPropertyName("chatSession")]
    public ChatSession ChatSession { get; set; }

    /// <summary>
    /// Initial bot message.
    /// </summary>
    [JsonPropertyName("initialBotMessage")]
    public ChatMessage InitialBotMessage { get; set; }

    public CreateChatResponse(ChatSession chatSession, ChatMessage initialBotMessage)
    {
        this.ChatSession = chatSession;
        this.InitialBotMessage = initialBotMessage;
    }
}
