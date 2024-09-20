// Copyright (c) Quartech. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Request definition for Specialization
/// </summary>
public class QSpecializationBase
{
    /// <summary>
    /// Key of the specialization
    /// </summary>
    [JsonPropertyName("label")]
    public string label { get; set; } = string.Empty;

    /// <summary>
    /// Name of the specialization
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the specialization
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// RoleInformation of the specialization
    /// </summary>
    [JsonPropertyName("roleInformation")]
    public string RoleInformation { get; set; } = string.Empty;

    /// <summary>
    /// List of group memberships for the user.
    /// </summary>
    [JsonPropertyName("groupMemberships")]
    public IList<string> GroupMemberships { get; set; } = new List<string>();

    /// <summary>
    /// IndexName of the specialization
    /// </summary>
    [JsonPropertyName("indexName")]
    public string? IndexName { get; set; } = string.Empty;

    /// <summary>
    /// Enable/Disable flag of the specialization.
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool isActive { get; set; } = true;

    /// <summary>
    /// Deployment of the specialization
    /// </summary>
    [JsonPropertyName("deployment")]
    public string? Deployment { get; set; } = string.Empty;
}
