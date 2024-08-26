// Copyright (c) Quartech. All rights reserved.

using System;
using System.Text.Json.Serialization;
using CopilotChat.WebApi.Storage;

namespace CopilotChat.WebApi.Models.Storage;

/// <summary>
/// The ChatUsers settings
/// </summary>
public class ChatUserSettings
{
    /// <summary>
    /// Is Dark Mode Enabled
    /// </summary>
    public bool darkMode { get; set; }

    /// <summary>
    /// Are Plugins & Personas Enabled
    /// </summary>
    public bool pluginsPersonas { get; set; }

    /// <summary>
    /// Is Simplified Chat Enabled
    /// </summary>
    public bool simplifiedChat { get; set; }
}

/// <summary>
/// A chat participant is a user that is part of a chat.
/// A user can be part of multiple chats, thus a user can have multiple chat participants.
/// </summary>
public class ChatUser : IStorageEntity
{
    /// <summary>
    /// Participant ID that is persistent and unique.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The users settings
    /// </summary>
    public ChatUserSettings settings { get; set; }

    /// <summary>
    /// Timestamp of the Chat user creation
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// The partition key for the source.
    /// </summary>
    [JsonIgnore]
    public string Partition => this.Id;

    public ChatUser(string userId)
    {
        this.Id = userId;
        this.CreatedOn = DateTimeOffset.Now;
        this.settings = new ChatUserSettings
        {
            darkMode = false,
            pluginsPersonas = false,
            simplifiedChat = true,
        };
    }
}
