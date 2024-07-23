﻿// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Parameters for creating a new chat session.
/// </summary>
public class CreateChatParameters
{
    /// <summary>
    /// Title of the chat.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Specialization of the chat.
    /// </summary>
    [JsonPropertyName("specializationKey")]
    public string specialization { get; set; } = string.Empty;
}