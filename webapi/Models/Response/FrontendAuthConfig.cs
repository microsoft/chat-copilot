// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using CopilotChat.WebApi.Options;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Configuration to be used by the frontend client to this service.
/// </summary>
public class FrontendAuthConfig
{
    /// <summary>
    /// Type of auth to use.
    /// </summary>
    [JsonPropertyName("authType")]
    public string AuthType { get; set; } = ChatAuthenticationOptions.AuthenticationType.None.ToString();

    /// <summary>
    /// Azure Active Directory authority to use.
    /// </summary>
    [JsonPropertyName("aadAuthority")]
    public string AadAuthority { get; set; } = string.Empty;

    /// <summary>
    /// Azure Active Directory client ID the frontend is to use.
    /// </summary>
    [JsonPropertyName("aadClient")]
    public string AadClient { get; set; } = string.Empty;

    /// <summary>
    /// Azure Active Directory scope the frontend should request.
    /// </summary>
    [JsonPropertyName("aadApiScope")]
    public string AadApiScope { get; set; } = string.Empty;
}
