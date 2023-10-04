// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Parameters for editing chat session.
/// </summary>
public class EditChatParameters
{
    /// <summary>
    /// Title of the chat.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// System description of the chat that is used to generate responses.
    /// </summary>
    public string? SystemDescription { get; set; }

    /// <summary>
    /// The balance between long term memory and working term memory.
    /// The higher this value, the more the system will rely on long term memory by lowering
    /// the relevance threshold of long term memory and increasing the threshold score of working memory.
    /// </summary>
    public float? MemoryBalance { get; set; }
}
