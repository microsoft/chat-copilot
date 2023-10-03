// Copyright (c) Microsoft. All rights reserved.

using System;

namespace CopilotChat.WebApi.Options;

/// <summary>
/// Option for a single plugin.
/// </summary>
public class Plugin
{
    /// <summary>
    /// The name of the plugin.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The url of the plugin.
    /// </summary>
    public Uri ManifestDomain { get; set; } = new Uri("http://localhost");

    /// <summary>
    /// The key of the plugin.
    /// </summary>
    public string Key { get; set; } = string.Empty;
}
