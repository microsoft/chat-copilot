// Copyright (c) Microsoft. All rights reserved.

namespace SemanticKernel.Service.CopilotChat.Models;

/// <summary>
/// Json body for creating a new chat session.
/// </summary>
public class CreateChatParameters
{
    /// <summary>
    /// Title of the chat.
    /// </summary>
    public string? Title { get; set; }
}
