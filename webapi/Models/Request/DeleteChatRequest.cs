// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Json body for deleting a chat session.
/// </summary>
public class DeleteChatRequest
{
    /// <summary>
    /// Id of the user who initiated chat deletion.
    /// </summary>
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
}
