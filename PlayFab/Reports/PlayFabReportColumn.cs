// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace PlayFab.Reports;

public class PlayFabReportColumn
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string? SourceName { get; internal set; }

    [JsonIgnore]
    public Func<string, string>? SourceParser { get; set; }
}
