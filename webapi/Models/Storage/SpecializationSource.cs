// Copyright (c) Quartech. All rights reserved.

using System;
using System.Text.Json.Serialization;
using CopilotChat.WebApi.Storage;

namespace CopilotChat.WebApi.Models.Storage;

/// <summary>
/// Information about the specialization source
/// </summary>
public class SpecializationSource : IStorageEntity
{
    /// <summary>
    /// ID that is persistent and unique.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Short unique representation of specialization.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Name of the specialization.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description of the specialization.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Role Information
    /// </summary>
    public string RoleInformation { get; set; }

    /// <summary>
    /// Index Name
    /// </summary>
    public string IndexName { get; set; }

    /// <summary>
    /// Image URL for pictorial description of specialization or logo etc.
    /// </summary>
    public string ImageFilePath { get; set; }

    /// <summary>
    /// The partition key for the specialization session.
    /// </summary>
    [JsonIgnore]
    public string Partition => this.Id;

    /// <summary>
    /// On/oFF switch for the specializations.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// UserId of admin user
    /// </summary>
    public string CreatedBy { get; set; } = "";

    /// <summary>
    /// UserId of admin user
    /// </summary>
    public string UpdatedBy { get; set; } = "";

    /// <summary>
    /// Timestamp of action
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// Timestamp of action
    /// </summary>>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Timestamp of action
    /// </summary>>
    public int Strictness { get; set; } = 3;

    /// <summary>
    /// Timestamp of action
    /// </summary>>
    public int DocumentCount { get; set; } = 20;

    public SpecializationSource(string Key, string Name, string Description, string RoleInformation, string IndexName, string ImageFilePath)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Key = Key;
        this.Name = Name;
        this.Description = Description;
        this.RoleInformation = RoleInformation;
        this.IndexName = IndexName;
        this.ImageFilePath = ImageFilePath;
        this.CreatedOn = DateTimeOffset.Now;
        this.IsActive = true;
    }
}
