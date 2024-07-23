///<summary>
/// This class is reserved for extending the Azure OpenAI Bot responses.
///</summary>
using Azure.AI.OpenAI;
namespace CopilotChat.WebApi.Plugins.Chat.Ext;

public class QAzureOpenAIChatExtension
{
    private readonly string defaultKey = "general";

    private readonly QAzureOpenAIChatOptions _qAzureOpenAIChatOptions;

    public QAzureOpenAIChatExtension(QAzureOpenAIChatOptions qAzureOpenAIChatOptions)
    {
        this._qAzureOpenAIChatOptions = qAzureOpenAIChatOptions;
    }
    public bool isEnabled(string? specializationKey)
    {
        if (this._qAzureOpenAIChatOptions.Enabled && specializationKey != this.defaultKey)
        {
            return true;
        }
        return false;
    }
    public string getDefault()
    {
        return this.defaultKey;
    }

    /// <summary>
    /// Extension method to support passing Azure Search options for chatCompletions.
    /// </summary>
    public AzureChatExtensionsOptions? GetAzureChatExtensionsOptions(string specializationKey)
    {
        QSpecialization? qsearchCompletionOption = this.getSpecialization(specializationKey);
        if (qsearchCompletionOption != null)
        {
            return new AzureChatExtensionsOptions()
            {
                Extensions =
                {
                    new AzureSearchChatExtensionConfiguration()
                    {
                        Filter = null,
                        IndexName  = qsearchCompletionOption!.IndexName,
                        SearchEndpoint=qsearchCompletionOption!.Endpoint,
                        Strictness = qsearchCompletionOption!.Strictness,
                        FieldMappingOptions = new AzureSearchIndexFieldMappingOptions {
                            UrlFieldName = qsearchCompletionOption!.FieldMapping?.UrlFieldName,
                            TitleFieldName = qsearchCompletionOption!.FieldMapping?.TitleFieldName,
                            FilepathFieldName = qsearchCompletionOption!.FieldMapping?.FilepathFieldName,
                        },
                        SemanticConfiguration = qsearchCompletionOption!.SemanticConfiguration,
                        QueryType = new AzureSearchQueryType(qsearchCompletionOption!.QueryType),
                        ShouldRestrictResultScope = qsearchCompletionOption!.RestrictResultScope,
                        RoleInformation = qsearchCompletionOption!.RoleInformation,
                        DocumentCount = qsearchCompletionOption!.DocumentCount,
                        Authentication = new OnYourDataApiKeyAuthenticationOptions (qsearchCompletionOption!.APIKey),
                        VectorizationSource = new OnYourDataEndpointVectorizationSource (
                           qsearchCompletionOption!.VectorizationSource!.Endpoint,
                           new OnYourDataApiKeyAuthenticationOptions (qsearchCompletionOption!.VectorizationSource!.APIKey))
                    }
                }
            };
        }
        return null;
    }
    public string? getRoleInformation(string specializationKey)
    {
        if (this.isEnabled(specializationKey))
        {
            foreach (QSpecialization _qSpecialization in this._qAzureOpenAIChatOptions.Specializations)
            {
                if (_qSpecialization.Key == specializationKey)
                {
                    return _qSpecialization.RoleInformation;
                }
            }
        }
        return null;
    }

    private QSpecialization? getSpecialization(string specializationKey)
    {
        foreach (QSpecialization _qSpecialization in this._qAzureOpenAIChatOptions.Specializations)
        {
            if (_qSpecialization.Key == specializationKey)
            {
                return _qSpecialization;
            }
        }
        return null;
    }
}
