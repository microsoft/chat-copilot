// Copyright (c) Quartech. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CopilotChat.WebApi.Plugins.Chat.Ext;

/// <summary>
/// This class is a representation of Azure AI Chat options.
/// </summary>
public class QAzureOpenAIChatOptions
{
    public const string PropertyName = "QAzureOpenAIChatConfig";

    public string DefaultSpecializationDescription { get; set; } = "";
    public string DefaultSpecializationImage { get; set; } = "";

    [Required]
    public IList<QSpecializationIndex> SpecializationIndexes { get; set; } = new List<QSpecializationIndex>();

    public bool Enabled { get; set; } = false;
    public AzureConfig AzureConfig { get; set; } = new AzureConfig();
}

/// <summary>
/// Normalized representation of Azure configuration.
/// </summary>
public class AzureConfig
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Uri? Endpoint { get; set; } = null;
#pragma warning restore CS8618
    public string APIKey { get; set; } = string.Empty;
    public VectorizationSourceOption VectorizationSource { get; set; } = new VectorizationSourceOption();
}

/// <summary>
/// Normalized representation of specialization Index
/// </summary>
public class QSpecializationIndex
{
    public string IndexName { get; set; } = string.Empty;
    public string QueryType { get; set; } = string.Empty;
    public string SemanticConfiguration { get; set; } = "default";
    public bool RestrictResultScope { get; set; } = true;
    public FieldMappingOption? FieldMapping { get; set; } = new FieldMappingOption();
    public IList<string> GroupMemberships { get; set; } = new List<string>();
}

/// <summary>
/// Normalized representation of Field Mapping option
/// </summary>
public class FieldMappingOption
{
#pragma warning disable CA1056 // URI-like properties should not be strings
    public string UrlFieldName { get; set; } = "url";
#pragma warning restore CA1056 // URI-like properties should not be strings
    public string TitleFieldName { get; set; } = "title";
    public string FilepathFieldName { get; set; } = "filepath";
}

/// <summary>
/// Normalized representation of Vectorization source.
/// </summary>
public class VectorizationSourceOption
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Uri Endpoint { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public string APIKey { get; set; } = string.Empty;
}
