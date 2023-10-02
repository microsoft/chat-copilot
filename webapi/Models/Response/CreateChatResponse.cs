// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Response object definition to the 'chats' POST request.
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
