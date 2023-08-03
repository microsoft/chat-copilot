﻿// Copyright (c) Microsoft. All rights reserved.

using CopilotChat.Models;

namespace CopilotChat.Storage;

/// <summary>
/// A repository for chat sessions.
/// </summary>
public class ChatSessionRepository : Repository<ChatSession>
{
    /// <summary>
    /// Initializes a new instance of the ChatSessionRepository class.
    /// </summary>
    /// <param name="storageContext">The storage context.</param>
    public ChatSessionRepository(IStorageContext<ChatSession> storageContext)
        : base(storageContext)
    {
    }
}
