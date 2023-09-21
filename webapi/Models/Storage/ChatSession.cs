// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json.Serialization;
using CopilotChat.WebApi.Storage;

namespace CopilotChat.WebApi.Models.Storage;

/// <summary>
/// A chat session
/// </summary>
public class ChatSession : IStorageEntity
{
    private const string CurrentVersion = "2.0";

    /// <summary>
    /// Chat ID that is persistent and unique.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Title of the chat.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Timestamp of the chat creation.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// System description of the chat that is used to generate responses.
    /// </summary>
    public string SystemDescription { get; set; }

    /// <summary>
    /// The balance between long term memory and working term memory.
    /// The higher this value, the more the system will rely on long term memory by lowering
    /// the relevance threshold of long term memory and increasing the threshold score of working memory.
    /// </summary>
    public float MemoryBalance { get; set; } = 0.5F;

    /// <summary>
    /// Used to determine if the current chat requires upgrade.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// The partition key for the session.
    /// </summary>
    [JsonIgnore]
    public string Partition => this.Id;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatSession"/> class.
    /// </summary>
    /// <param name="title">The title of the chat.</param>
    /// <param name="systemDescription">The system description of the chat.</param>
    public ChatSession(string title, string systemDescription)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Title = title;
        this.CreatedOn = DateTimeOffset.Now;
        this.SystemDescription = systemDescription;
        this.Version = CurrentVersion;
    }
}
