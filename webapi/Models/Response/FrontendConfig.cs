// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Configuration to be used by the frontend client to this service.
/// </summary>
public class FrontendConfig
{
    /// <summary>
    /// Type of auth to use.
    /// </summary>
    [JsonPropertyName("authType")]
    public string AuthType { get; set; } = "AzureAd";

    /// <summary>
    /// Azure Active Directory authority to use.
    /// </summary>
    [JsonPropertyName("aadAuthority")]
    public string AadAuthority { get; set; } = string.Empty;

    /// <summary>
    /// Azure Active Directory client ID to frontend is to use.
    /// </summary>
    [JsonPropertyName("aadClient")]
    public string AadClient { get; set; } = string.Empty;

    /// <summary>
    /// Azure Active Directory scope the frontend should request.
    /// </summary>
    [JsonPropertyName("aadApiScope")]
    public string AadApiScope { get; set; } = string.Empty;

    /// <summary>
    /// Backend URI from which to operate.
    /// </summary>
    [JsonPropertyName("backendUri")]
    public Uri? BackendUri { get; set; }
}
