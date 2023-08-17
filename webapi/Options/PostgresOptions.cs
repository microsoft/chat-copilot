// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace CopilotChat.WebApi.Options;

/// <summary>
/// Configuration settings for connecting to Postgres.
/// </summary>
public class PostgresOptions
{
    /// <summary>
    /// Gets or sets the Postgres connection string.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vector size.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int VectorSize { get; set; }
}
