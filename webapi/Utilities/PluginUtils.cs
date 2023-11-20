// Copyright (c) Microsoft. All rights reserved.

using System;
using CopilotChat.WebApi.Models.Request;
using Microsoft.SemanticKernel.Orchestration;

namespace CopilotChat.WebApi.Utilities;

/// <summary>
/// Converts <see cref="Ask"/> variables to <see cref="ContextVariables"/>, inserting some system variables along the way.
/// </summary>
internal static class PluginUtils
{
    /// <summary>
    /// Gets the plugin manifest URI for the given plugin domain.
    /// </summary>
    /// <param name="manifestDomain">The plugin domain as a string.</param>
    /// <returns>The plugin manifest URI.</returns>
    public static Uri GetPluginManifestUri(string manifestDomain)
    {
        Uri uri = new(Uri.UnescapeDataString(manifestDomain));
        return GetPluginManifestUri(uri);
    }

    /// <summary>
    /// Gets the plugin manifest URI for the given plugin domain.
    /// </summary>
    /// <param name="manifestDomain">The plugin domain as an Uri object.</param>
    /// <returns>The plugin manifest URI.</returns>
    public static Uri GetPluginManifestUri(Uri manifestDomain)
    {
        UriBuilder uriBuilder = new(manifestDomain);

        // Expected manifest path as defined by OpenAI: https://platform.openai.com/docs/plugins/getting-started/plugin-manifest
        uriBuilder.Path = "/.well-known/ai-plugin.json";
        return uriBuilder.Uri;
    }

    /// <summary>
    /// Sanitizes the plugin name by removing spaces.
    /// </summary>
    /// <param name="name">The plugin name.</param>
    /// <returns>The sanitized plugin name.</returns>
    public static string SanitizePluginName(string name)
    {
        return name.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
