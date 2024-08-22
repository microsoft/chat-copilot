// Copyright (c) Quartech. All rights reserved.

using System;
using System.Text.Json.Serialization;
using CopilotChat.WebApi.Storage;

namespace CopilotChat.WebApi.Models.Storage;

/// <summary>
/// A chat specialization session
/// </summary>
public class ChatSpecializationSession : IStorageEntity
{
    private const string CurrentVersion = "2.0";

    /// <summary>
    /// Chat ID that is persistent and unique.
    /// </summary>
    [JsonIgnore]
    public string Id { get; set; }

    /// <summary>
    /// Specialization Key associated with the chat.
    /// </summary>
    public string SpecializationId { get; set; }

    /// <summary>
    /// The partition key for the session.
    /// </summary>
    [JsonIgnore]
    public string Partition => this.Id;

    /// <summary>
    /// Used to determine if the current chat specialization requires upgrade.
    /// </summary>
    [JsonIgnore]
    public string? Version { get; set; }

    /// <summary>
    /// Timestamp of the chat specialization creation.
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatSpecializationSession"/> class.
    /// </summary>
    /// <param name="chatId">The id of the chat.</param>
    /// <param name="specialization">The specialization key associated with the chat.</param>
    public ChatSpecializationSession(string chatId, string specializationId)
    {
        this.Id = chatId;
        this.SpecializationId = specializationId;
        this.CreatedOn = DateTimeOffset.Now;
        this.Version = CurrentVersion;
    }
}
