// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Storage;

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

    /// <summary>
    /// Retrieves all chat sessions.
    /// </summary>
    /// <returns>A list of ChatMessages.</returns>
    public Task<IEnumerable<ChatSession>> GetAllChatsAsync()
    {
        return base.StorageContext.QueryEntitiesAsync(e => true);
    }
}
