// Copyright (c) Quartech. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Storage;
namespace CopilotChat.WebApi.Plugins.Chat.Ext;

/// <summary>
/// Chat extension class to support Azure search indexes for bot response.
/// </summary>
public class QAzureOpenAIChatExtension
{
    /// <summary>
    /// Default specialization key.
    /// </summary>
    public string DefaultSpecialization { get; } = "general";

    /// <summary>
    /// Name of the key which carries the specialization
    /// </summary>
    public string ContextKey { get; } = "specialization";

    /// <summary>
    /// Chat Extension Azure OpenAI options
    /// </summary>
    private readonly QAzureOpenAIChatOptions _qAzureOpenAIChatOptions;

    /// <summary>
    /// Specialization data Service.
    /// </summary>
    private readonly QSpecializationService _qSpecializationService;

    public QAzureOpenAIChatExtension(QAzureOpenAIChatOptions qAzureOpenAIChatOptions, SpecializationRepository specializationSourceRepository)
    {
        this._qAzureOpenAIChatOptions = qAzureOpenAIChatOptions;
        this._qSpecializationService = new QSpecializationService(specializationSourceRepository);
    }
    public bool isEnabled(string? specializationId)
    {
        if (this._qAzureOpenAIChatOptions.Enabled && specializationId != this.DefaultSpecialization)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Extension method to support passing Azure Search options for chatCompletions.
    /// </summary>
    public async Task<AzureChatExtensionsOptions?> GetAzureChatExtensionsOptions(string specializationId)
    {
        Specialization specialization = await this._qSpecializationService.GetSpecializationAsync(specializationId);

        if (specialization != null && specialization.IndexName != null)
        {
            QSpecializationIndex? qSpecializationIndex = this.GetSpecializationIndexByName(specialization.IndexName);
            if (qSpecializationIndex == null)
            {
                return null;
            }
            var azureConfig = this._qAzureOpenAIChatOptions.AzureConfig;
            return new AzureChatExtensionsOptions()
            {
                Extensions =
                {
                    new AzureSearchChatExtensionConfiguration()
                    {
                        Filter = null,
                        IndexName  = specialization.IndexName,
                        SearchEndpoint= azureConfig.Endpoint,
                        Strictness = specialization.Strictness,
                        FieldMappingOptions = new AzureSearchIndexFieldMappingOptions {
                            UrlFieldName = qSpecializationIndex.FieldMapping?.UrlFieldName,
                            TitleFieldName = qSpecializationIndex.FieldMapping?.TitleFieldName,
                            FilepathFieldName = qSpecializationIndex.FieldMapping?.FilepathFieldName,
                        },
                        SemanticConfiguration = qSpecializationIndex.SemanticConfiguration,
                        QueryType = new AzureSearchQueryType(qSpecializationIndex.QueryType),
                        ShouldRestrictResultScope = qSpecializationIndex!.RestrictResultScope,
                        RoleInformation = specialization.RoleInformation,
                        DocumentCount = specialization.DocumentCount,
                        Authentication = new OnYourDataApiKeyAuthenticationOptions (azureConfig!.APIKey),
                        VectorizationSource = new OnYourDataEndpointVectorizationSource (
                           azureConfig.VectorizationSource!.Endpoint,
                           new OnYourDataApiKeyAuthenticationOptions (azureConfig.VectorizationSource!.APIKey))
                    }
                }
            };
        }
        return null;
    }

    /// <summary>
    /// Retrieve the Azure configuration.
    /// </summary>
    public AzureConfig AzureConfig => this._qAzureOpenAIChatOptions.AzureConfig;

    /// <summary>
    /// Retrieve all configured specialization indexess.
    /// </summary>
    public List<string> GetAllSpecializationIndexNames()
    {
        var indexNames = new List<string>();
        foreach (QSpecializationIndex _qSpecializationIndex in this._qAzureOpenAIChatOptions.SpecializationIndexes)
        {
            indexNames.Add(_qSpecializationIndex.IndexName);
        }
        return indexNames;
    }

    /// <summary>
    /// Retrieve the specialization Index based on Index name.
    /// </summary>
    public QSpecializationIndex? GetSpecializationIndexByName(string indexName)
    {
        foreach (QSpecializationIndex _qSpecializationIndex in this._qAzureOpenAIChatOptions.SpecializationIndexes)
        {
            if (_qSpecializationIndex.IndexName == indexName)
            {
                return _qSpecializationIndex;
            }
        }
        return null;
    }
}
