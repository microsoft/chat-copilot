// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace CopilotChat.WebApi.Options;

public class MySqlOptions
{
    [Required, NotEmptyOrWhitespace]
    public string Database { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string ConnectionString { get; set; } = string.Empty;
}
