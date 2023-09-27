// Copyright (c) Microsoft. All rights reserved.

using static CopilotChat.WebApi.Models.Storage.ChatMessage;

namespace CopilotChat.WebApi.Skills.Utils;

/// <summary>
/// Utility methods for prompt generation.
/// </summary>
public static class PromptUtils
{
    /// <summary>
    /// Convert a chat message to a string in the format of: "Role: Content".
    /// </summary>
    /// <param name="role">The role of the author of the message.</param>
    /// <param name="content">The content of the message.</param>
    /// <returns>A formatted chat message string.</returns>
    internal static string? FormatChatHistoryMessage(AuthorRoles role, string content) => $"{role}: {content}";
}
