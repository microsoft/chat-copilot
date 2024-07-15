using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;


namespace CopilotChat.WebApi.Plugins.Chat.Ext;

public class QAzureOpenAIChatExtension
{

    /// <summary>
    /// A logger instance to log events.
    /// </summary>
    private ILogger _logger;

    private QAzureOpenAIChatOptions _qAzureOpenAIChatOptions;

    public QAzureOpenAIChatExtension(ILogger logger, QAzureOpenAIChatOptions qAzureOpenAIChatOptions)
    {
        this._logger = logger;
        this._qAzureOpenAIChatOptions = qAzureOpenAIChatOptions;
    }

    public bool isEnabled()
    {
        return this._qAzureOpenAIChatOptions.Enabled;
    }

    /// <summary>
    /// Extension method to support passing Azure Search options for chatCompletions.
    /// </summary>
    public AzureChatExtensionsOptions GetAzureChatExtensionsOptions()
    {

        this._logger.LogDebug("Enabling QAzureChatExtensionsOptions.");
        AISearchChatCompletionOption? qsearchCompletionOption = this._qAzureOpenAIChatOptions.AISearchChatCompletion;
        return new AzureChatExtensionsOptions()
        {
            Extensions =
                {
                    new AzureSearchChatExtensionConfiguration()
                    {
                        Filter = null,
                        IndexName  = qsearchCompletionOption?.IndexName,
                        SearchEndpoint=qsearchCompletionOption?.Endpoint,
                        Strictness = qsearchCompletionOption?.Strictness,
                        FieldMappingOptions = new AzureSearchIndexFieldMappingOptions {
                            UrlFieldName = qsearchCompletionOption?.FieldMapping?.UrlFieldName,
                            TitleFieldName = qsearchCompletionOption?.FieldMapping?.TitleFieldName,
                            FilepathFieldName = qsearchCompletionOption?.FieldMapping?.FilepathFieldName,
                        },
                        SemanticConfiguration = qsearchCompletionOption?.SemanticConfiguration,
                        QueryType = new AzureSearchQueryType(qsearchCompletionOption?.QueryType),
                        ShouldRestrictResultScope = qsearchCompletionOption?.RestrictResultScope,
                        RoleInformation = qsearchCompletionOption?.RoleInformation,
                        DocumentCount = qsearchCompletionOption?.DocumentCount,
                        Authentication = new OnYourDataApiKeyAuthenticationOptions (qsearchCompletionOption?.APIKey),
                        VectorizationSource = new OnYourDataEndpointVectorizationSource (
                           qsearchCompletionOption?.VectorizationSource?.Endpoint,
                           new OnYourDataApiKeyAuthenticationOptions (qsearchCompletionOption?.VectorizationSource?.APIKey))
                    }
                }

        };
    }
}
