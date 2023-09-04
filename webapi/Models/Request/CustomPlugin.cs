// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Custom plugin imported from ChatGPT Manifest file.
/// Docs: https://platform.openai.com/docs/plugins/introduction.
/// </summary>
public class CustomPlugin
{
    /// <summary>
    /// Human-readable name, such as the full company name.
    /// </summary>
    [JsonPropertyName("nameForHuman")]
    public string NameForHuman { get; set; } = string.Empty;

    /// <summary>
    /// Name the model will use to target the plugin.
    /// </summary>
    [JsonPropertyName("nameForModel")]
    public string NameForModel { get; set; } = string.Empty;

    /// <summary>
    /// Expected request header tag containing auth information.
    /// </summary>
    [JsonPropertyName("authHeaderTag")]
    public string AuthHeaderTag { get; set; } = string.Empty;

    /// <summary>
    /// Auth type. Currently limited to either 'none'
    /// or user PAT (https://platform.openai.com/docs/plugins/authentication/user-level)
    /// </summary>
    [JsonPropertyName("authType")]
    public string AuthType { get; set; } = string.Empty;

    /// <summary>
    /// Website domain hosting the plugin files.
    /// </summary>
    [JsonPropertyName("manifestDomain")]
    public string ManifestDomain { get; set; } = string.Empty;
}
