// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ComponentModel.DataAnnotations;

namespace CopilotChat.WebApi.Options;

/// <summary>
/// Configuration options for content safety.
/// </summary>
public class ContentSafetyOptions
{
    public const string PropertyName = "ContentSafety";

    /// <summary>
    /// Whether to enable content safety.
    /// </summary>
    [Required]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Azure Content Safety endpoints
    /// </summary>
    [RequiredOnPropertyValue(nameof(Enabled), true)]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Key to access the content safety service.
    /// </summary>
    [RequiredOnPropertyValue(nameof(Enabled), true)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Set the violation threshold. See https://learn.microsoft.com/en-us/azure/ai-services/content-safety/quickstart-image for details.
    /// </summary>
    [Range(0, 6)]
    public short ViolationThreshold { get; set; } = 4;
}
