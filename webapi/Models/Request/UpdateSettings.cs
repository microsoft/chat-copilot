// Copyright (c) Quartech. All rights reserved.

using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Request;

public sealed class UpdateSettings
{
    /// <summary>
    /// Key of the setting
    /// </summary>
    [JsonPropertyName("setting")]
    public string Setting { get; set; } = string.Empty;

    /// <summary>
    /// Enable or Disable setting
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}
