// Copyright (c) Quartech. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Response definition for Specialization
/// </summary>
public class QSpecializationResponse
{
    /// <summary>
    /// Id of the specialization
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Key of the specialization
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

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
    /// IndexName of the specialization
    /// </summary>
    [JsonPropertyName("indexName")]
    public string IndexName { get; set; } = string.Empty;

    /// <summary>
    /// Image FilePath of the specialization.
    /// </summary>
    [JsonPropertyName("imageFilePath")]
    public string ImageFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Enable/Disable flag of the specialization.
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool isActive { get; set; } = true;

    /// <summary>
    /// List of group memberships for the user.
    /// </summary>
    public IList<string> GroupMemberships { get; set; } = new List<string>();

    /// <summary>
    /// Creates new instance from SpecializationSource.
    /// </summary>
    public QSpecializationResponse(SpecializationSource specializationSource)
    {
        this.Id = specializationSource.Id;
        this.Key = specializationSource.Key;
        this.Name = specializationSource.Name;
        this.Description = specializationSource.Description;
        this.RoleInformation = specializationSource.RoleInformation;
        this.IndexName = specializationSource.IndexName;
        this.ImageFilePath = specializationSource.ImageFilePath;
        this.isActive = specializationSource.IsActive;
    }

    /// <summary>
    /// Creates new instance from default specialization dictionary.
    /// </summary>
    public QSpecializationResponse(Dictionary<string, string> specializationProps)
    {
        this.Id = specializationProps["id"];
        this.Key = specializationProps["key"];
        this.Name = specializationProps["name"];
        this.Description = specializationProps["description"];
        this.RoleInformation = specializationProps["roleInformation"];
        this.ImageFilePath = specializationProps["imageFilePath"];
        this.isActive = true;
    }
}
