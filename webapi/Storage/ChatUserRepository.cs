// Copyright (c) Quartech. All rights reserved.

using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A repository for chat users.
/// </summary>
public class ChatUserRepository : Repository<ChatUser>
{
    /// <summary>
    /// Initializes a new instance of the ChatUserRepository class.
    /// </summary>
    /// <param name="storageContext">The storage context.</param>
    public ChatUserRepository(IStorageContext<ChatUser> storageContext)
        : base(storageContext) { }
}
