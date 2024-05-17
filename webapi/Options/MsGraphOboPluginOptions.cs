// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Options;

public class MsGraphOboPluginOptions
{
    public const string PropertyName = "OnBehalfOf";
    /// <summary>
    /// The authority to use for OBO Auth.
    /// </summary>
    public string? Authority { get; set; }
    /// <summary>
    /// The Tenant Id to use for OBO Auth.
    /// </summary>
    public string? TenantId { get; set; }
    /// <summary>
    /// The Client Id to use for OBO Auth.
    /// </summary>
    public string? ClientId { get; set; }
    /// <summary>
    /// The Client Secret to use for OBO Auth.
    /// </summary>
    public string? ClientSecret { get; set; }
}
