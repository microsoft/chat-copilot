// Copyright (c) Microsoft. All rights reserved.

using System;

namespace CopilotChat.WebApi.Options;

/// <summary>
/// Configuration options to be relayed to the frontend.
/// </summary>
public sealed class FrontendOptions
{
    public const string PropertyName = "Frontend";

    /// <summary>
    /// Client ID for the frontend
    /// </summary>
    public string AadClientId { get; set; } = string.Empty;

    /// <summary>
    /// URI of backend that frontend will connect to (i.e. this app's URI).
    /// </summary>
    public Uri? BackendUri { get; set; }
}
