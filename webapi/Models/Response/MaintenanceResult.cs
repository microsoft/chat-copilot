// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Defines optional messaging for maintenance mode.
/// </summary>
public class MaintenanceResult
{
    /// <summary>
    /// The maintenance notification title.
    /// </summary>
    /// <remarks>
    /// Will utilize default if not defined.
    /// </remarks>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The maintenance notification message.
    /// </summary>
    /// <remarks>
    /// Will utilize default if not defined.
    /// </remarks>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The maintenance notification note.
    /// </summary>
    /// <remarks>
    /// Will utilize default if not defined.
    /// </remarks>
    public string? Note { get; set; }
}
