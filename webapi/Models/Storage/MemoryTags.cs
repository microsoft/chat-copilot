// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Models.Storage;

/// <summary>
/// Tag names for semantic memory.
/// </summary>
internal static class MemoryTags
{
    /// <summary>
    /// Associates memory with a specific chat
    /// </summary>
    public const string TagChatId = "chatid";

    /// <summary>
    /// Associates memory with specific type.
    /// </summary>
    public const string TagMemory = "memory";
}
