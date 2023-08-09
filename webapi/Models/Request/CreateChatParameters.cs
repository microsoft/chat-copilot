// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Parameters for creating a new chat session.
/// </summary>
public class CreateChatParameters
{
    /// <summary>
    /// Title of the chat.
    /// </summary>
    public string? Title { get; set; }
}
