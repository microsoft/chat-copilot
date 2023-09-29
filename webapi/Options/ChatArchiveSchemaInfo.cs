// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace CopilotChat.WebApi.Options;

/// <summary>
/// Information on schema used to serialize chat archives.
/// </summary>
public record ChatArchiveSchemaInfo
{
    /// <summary>
    /// The name of the schema.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string Name { get; init; } = "CopilotChat";

    /// <summary>
    /// The version of the schema.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Version { get; init; } = 1;
}
