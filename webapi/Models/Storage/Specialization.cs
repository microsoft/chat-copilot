// Copyright (c) Quartech. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CopilotChat.WebApi.Storage;

namespace CopilotChat.WebApi.Models.Storage;

/// <summary>
/// Information about the specialization
/// </summary>
public class Specialization : IStorageEntity
{
    /// <summary>
    /// ID that is persistent and unique.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Short representation of specialization.
    /// </summary>
    public string Label { get; set; }

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
    /// List of group memberships for the user.
    /// </summary>
    public IList<string> GroupMemberships { get; set; } = new List<string>();

    /// <summary>
    /// Index Name
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// Deployment
    /// </summary>
    public string? Deployment { get; set; }

    /// <summary>
    /// Image URL for pictorial description of specialization.
    /// </summary>
    public string ImageFilePath { get; set; }

    /// <summary>
    /// Icon URL for pictorial description of specialization.
    /// </summary>
    public string IconFilePath { get; set; }

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

    public Specialization(
        string Label,
        string Name,
        string Description,
        string RoleInformation,
        string? IndexName,
        string? Deployment,
        string ImageFilePath,
        string IconFilePath,
        IList<string> GroupMemberships
    )
    {
        this.Id = Guid.NewGuid().ToString();
        this.Label = Label;
        this.Name = Name;
        this.Description = Description;
        this.RoleInformation = RoleInformation;
        this.IndexName = IndexName;
        this.Deployment = Deployment;
        this.ImageFilePath = ImageFilePath;
        this.IconFilePath = IconFilePath;
        this.GroupMemberships = GroupMemberships;
        this.CreatedOn = DateTimeOffset.Now;
        this.IsActive = true;
    }
}
