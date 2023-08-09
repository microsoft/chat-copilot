﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ComponentModel.DataAnnotations;

namespace CopilotChat.WebApi.Options;

/// <summary>
/// Configuration options for content moderation.
/// </summary>
public class ContentModeratorOptions
{
    public const string PropertyName = "ContentModerator";

    /// <summary>
    /// Whether to enable content moderation.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Azure Content Moderator endpoints
    /// </summary>
    [RequiredOnPropertyValue(nameof(Enabled), true)]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Key to access the content moderation service.
    /// </summary>
    [RequiredOnPropertyValue(nameof(Enabled), true)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Set the violation threshold. See https://github.com/Azure/Project-Carnegie-Private-Preview for details.
    /// </summary>
    [Range(0, 6)]
    public short ViolationThreshold { get; set; } = 4;
}
