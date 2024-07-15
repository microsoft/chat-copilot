using System;
using System.Collections.Generic;

namespace CopilotChat.WebApi.Plugins.Chat.Ext;

public class QAzureOpenAIChatOptions
{
    public const string PropertyName = "QAzureOpenAIChatConfig";

    public AISearchChatCompletionOption? AISearchChatCompletion { get; set; }

    public bool Enabled { get; set; } = false;
}
public class AISearchChatCompletionOption
{
    public string IndexName { get; set; } = string.Empty;
    public Uri? Endpoint { get; set; } = null;
    public string APIKey { get; set; } = string.Empty;
    public string QueryType { get; set; } = string.Empty;
    public string SemanticConfiguration { get; set; } = string.Empty;
    public bool RestrictResultScope { get; set; } = true;
    public FieldMappingOption? FieldMapping { get; set; } = new FieldMappingOption();
    public int Strictness { get; set; }
    public int DocumentCount { get; set; }
    public string RoleInformation { get; set; } = string.Empty;

    public VectorizationSourceOption? VectorizationSource { get; set; }
}

public class FieldMappingOption
{
    public string UrlFieldName { get; set; } = string.Empty;
    public string TitleFieldName { get; set; } = string.Empty;
    public string FilepathFieldName { get; set; } = string.Empty;    

}

public class VectorizationSourceOption
{
    public Uri Endpoint { get; set; } = null;
    public string APIKey { get; set; } = string.Empty;
}
