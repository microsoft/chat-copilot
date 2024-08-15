// Copyright (c) Quartech. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CopilotChat.WebApi.Plugins.Chat.Ext;

public class QAzureOpenAIChatOptions
{
    public const string PropertyName = "QAzureOpenAIChatConfig";

    [Required]
    public IList<QSpecialization> Specializations { get; set; } = new List<QSpecialization>();

    public bool Enabled { get; set; } = false;
}

public class QSpecialization
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageFilepath { get; set; } = string.Empty;
    public string RoleInformation { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public Uri? Endpoint { get; set; } = null;
    public string APIKey { get; set; } = string.Empty;
    public string QueryType { get; set; } = string.Empty;
    public string SemanticConfiguration { get; set; } = string.Empty;
    public bool RestrictResultScope { get; set; } = true;
    public IList<string> GroupMemberships { get; set; } = new List<string>();
    public FieldMappingOption? FieldMapping { get; set; } = new FieldMappingOption();
    public int Strictness { get; set; }
    public int DocumentCount { get; set; }
    public VectorizationSourceOption? VectorizationSource { get; set; }
}

public class FieldMappingOption
{
#pragma warning disable CA1056 // URI-like properties should not be strings
    public string UrlFieldName { get; set; } = string.Empty;
#pragma warning restore CA1056 // URI-like properties should not be strings
    public string TitleFieldName { get; set; } = string.Empty;
    public string FilepathFieldName { get; set; } = string.Empty;
}

public class VectorizationSourceOption
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Uri Endpoint { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public string APIKey { get; set; } = string.Empty;
}
