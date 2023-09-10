// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace CopilotChat.WebApi.Options;

/// <summary>
/// Configuration settings for connecting to Azure MongoDb.
/// </summary>
public class MongoDbOptions
{
    /// <summary>
    /// Gets or sets the MongoDb database name.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MongoDb connection string.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MongoDb collection for chat sessions.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string ChatSessionsCollection { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MongoDb collection for chat messages.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string ChatMessagesCollection { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MongoDb collection for chat memory sources.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string ChatMemorySourcesCollection { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MongoDb collection for chat participants.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string ChatParticipantsCollection { get; set; } = string.Empty;
}
