// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Parameters for creating a new chat session.
/// Note: This class has been modified to support chat specialization.
/// </summary>
public class CreateChatParameters
{
    /// <summary>
    /// Set a GUID manually here. Optional, as the repository can also generate it.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Title of the chat.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Specialization of the chat.
    /// </summary>
    [JsonPropertyName("specializationId")]
    public string specializationId { get; set; } = string.Empty;
}
