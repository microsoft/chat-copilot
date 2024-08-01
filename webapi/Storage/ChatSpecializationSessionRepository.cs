// Copyright (c) Quartech. All rights reserved.

using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A repository for chat specialization sessions.
/// </summary>
public class ChatSpecializationSessionRepository : Repository<ChatSpecializationSession>
{
    /// <summary>
    /// Initializes a new instance of the ChatSpecializationSessionRepository class.
    /// </summary>
    /// <param name="storageContext">The storage context.</param>
    public ChatSpecializationSessionRepository(IStorageContext<ChatSpecializationSession> storageContext)
        : base(storageContext)
    {
    }
}
