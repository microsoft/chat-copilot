// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Defines optional messaging for maintenace mode.
/// </summary>
public class MaintenanceResult
{
    /// <summary>
    /// The maintenace notification title.
    /// </summary>
    /// <remarks>
    /// Will utilize default if not defined.
    /// </remarks>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The maintenace notification message.
    /// </summary>
    /// <remarks>
    /// Will utilize default if not defined.
    /// </remarks>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The maintenace notification note.
    /// </summary>
    /// <remarks>
    /// Will utilize default if not defined.
    /// </remarks>
    public string? Note { get; set; }
}
