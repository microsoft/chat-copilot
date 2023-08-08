// Copyright (c) Microsoft. All rights reserved.

using System;
using CopilotChat.WebApi.Storage;

namespace CopilotChat.WebApi.Models.Storage;

/// <summary>
/// A chat participant is a user that is part of a chat.
/// A user can be part of multiple chats, thus a user can have multiple chat participants.
/// </summary>
public class ChatParticipant : IStorageEntity
{
    /// <summary>
    /// Participant ID that is persistent and unique.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// User ID that is persistent and unique.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Chat ID that this participant belongs to.
    /// </summary>
    public string ChatId { get; set; }

    public ChatParticipant(string userId, string chatId)
    {
        this.Id = Guid.NewGuid().ToString();
        this.UserId = userId;
        this.ChatId = chatId;
    }
}
