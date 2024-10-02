// Copyright (c) Quartech. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Storage;

namespace CopilotChat.WebApi.Plugins.Chat.Ext;

using System;
using System.Linq;

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

    public QAzureOpenAIChatExtension(
        QAzureOpenAIChatOptions qAzureOpenAIChatOptions,
        SpecializationRepository specializationSourceRepository
    )
    {
        this._qAzureOpenAIChatOptions = qAzureOpenAIChatOptions;
        this._qSpecializationService = new QSpecializationService(
            specializationSourceRepository,
            qAzureOpenAIChatOptions
        );
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
            QAzureOpenAIChatOptions.QSpecializationIndex? qSpecializationIndex = this.GetSpecializationIndexByName(
                specialization.IndexName
            );
            if (qSpecializationIndex == null)
            {
                return null;
            }
            var aiSearchDeploymentConnection =
                this._qAzureOpenAIChatOptions.AISearchDeploymentConnections.FirstOrDefault(c =>
                    c.Name == qSpecializationIndex.AISearchDeploymentConnection
                );
            var openAIDeploymentConnection = this._qAzureOpenAIChatOptions.OpenAIDeploymentConnections.FirstOrDefault(
                c => c.Name == qSpecializationIndex.OpenAIDeploymentConnection
            );
            if (openAIDeploymentConnection.Endpoint == null || openAIDeploymentConnection.APIKey == null)
            {
                throw new InvalidOperationException("OpenAI Deployment Connection or its endpoint  is missing.");
            }
            var EmbeddingEndpoint = this.GenerateEmbeddingEndpoint(
                openAIDeploymentConnection.Endpoint,
                qSpecializationIndex
            );
            if (aiSearchDeploymentConnection == null || openAIDeploymentConnection == null)
            {
                throw new InvalidOperationException(
                    "Configuration error: AI Search Deployment Connection or OpenAI Deployment Connection is missing."
                );
            }
            return new AzureChatExtensionsOptions()
            {
                Extensions =
                {
                    new AzureSearchChatExtensionConfiguration()
                    {
                        Filter = null,
                        IndexName = specialization.IndexName,
                        SearchEndpoint = aiSearchDeploymentConnection.Endpoint,
                        Strictness = specialization.Strictness,
                        FieldMappingOptions = new AzureSearchIndexFieldMappingOptions
                        {
                            UrlFieldName = qSpecializationIndex.FieldMapping?.UrlFieldName,
                            TitleFieldName = qSpecializationIndex.FieldMapping?.TitleFieldName,
                            FilepathFieldName = qSpecializationIndex.FieldMapping?.FilepathFieldName,
                        },
                        SemanticConfiguration = qSpecializationIndex.SemanticConfiguration,
                        QueryType = new AzureSearchQueryType(qSpecializationIndex.QueryType),
                        ShouldRestrictResultScope = specialization.RestrictResultScope,
                        RoleInformation = specialization.RoleInformation,
                        DocumentCount = specialization.DocumentCount,
                        Authentication = new OnYourDataApiKeyAuthenticationOptions(aiSearchDeploymentConnection.APIKey),
                        VectorizationSource = new OnYourDataEndpointVectorizationSource(
                            EmbeddingEndpoint,
                            new OnYourDataApiKeyAuthenticationOptions(openAIDeploymentConnection.APIKey)
                        ),
                    },
                },
            };
        }
        return null;
    }

    /// <summary>
    /// Retrieve all configured specialization indexess.
    /// </summary>
    public List<string> GetAllSpecializationIndexNames()
    {
        var indexNames = new List<string>();
        foreach (
            QAzureOpenAIChatOptions.QSpecializationIndex _qSpecializationIndex in this._qAzureOpenAIChatOptions.SpecializationIndexes
        )
        {
            indexNames.Add(_qSpecializationIndex.IndexName);
        }
        return indexNames;
    }

    /// <summary>
    /// Retrieve the specialization Index based on Index name.
    /// </summary>
    public QAzureOpenAIChatOptions.QSpecializationIndex? GetSpecializationIndexByName(string indexName)
    {
        foreach (
            QAzureOpenAIChatOptions.QSpecializationIndex _qSpecializationIndex in this._qAzureOpenAIChatOptions.SpecializationIndexes
        )
        {
            if (_qSpecializationIndex.IndexName == indexName)
            {
                return _qSpecializationIndex;
            }
        }
        return null;
    }

    public Uri? GenerateEmbeddingEndpoint(
        Uri connectionEndpoint,
        QAzureOpenAIChatOptions.QSpecializationIndex qSpecializationIndex
    )
    {
        return new Uri(
            connectionEndpoint,
            $"/openai/deployments/{qSpecializationIndex.EmbeddingDeployment}/embeddings?api-version=2023-05-15"
        );
    }

    public QAzureOpenAIChatOptions.AISearchDeploymentConnection? GetAISearchDeploymentConnection(string connectionName)
    {
        return this._qAzureOpenAIChatOptions.AISearchDeploymentConnections.FirstOrDefault(connection =>
            connection.Name == connectionName
        );
    }

    public (string? ApiKey, string? Endpoint) GetAISearchDeploymentConnectionDetails(string indexName)
    {
        var specializationIndex = this.GetSpecializationIndexByName(indexName);
        if (specializationIndex == null)
        {
            return (null, null);
        }
        var aiSearchDeploymentConnection = this.GetAISearchDeploymentConnection(
            specializationIndex.AISearchDeploymentConnection
        );
        return (aiSearchDeploymentConnection?.APIKey, aiSearchDeploymentConnection?.Endpoint.ToString());
    }

    /// <summary>
    /// Retrieve all chat completion deployments from the available OpenAI deployment connections.
    /// </summary>
    public List<string> GetAllChatCompletionDeployments()
    {
        var chatCompletionDeployments = new List<string>();
        foreach (
            QAzureOpenAIChatOptions.OpenAIDeploymentConnection connection in this._qAzureOpenAIChatOptions.OpenAIDeploymentConnections
        )
        {
            foreach (var deployment in connection.ChatCompletionDeployments)
            {
                chatCompletionDeployments.Add($"{deployment}");
            }
        }
        return chatCompletionDeployments;
    }

    /// <summary>
    /// Get the default chat completion deployment.
    /// </summary>
#pragma warning disable CA1024
    public string GetDefaultChatCompletionDeployment()
    {
        return this._qAzureOpenAIChatOptions.DefaultModel;
    }
}
