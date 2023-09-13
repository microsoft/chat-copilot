// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.SkillDefinition;

namespace CopilotChat.WebApi.Skills.ChatSkills;

/// <summary>
/// This skill provides the functions to query the document memory.
/// </summary>
public class DocumentMemorySkill
{
    /// <summary>
    /// High level logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Prompt settings.
    /// </summary>
    private readonly PromptsOptions _promptOptions;

    /// <summary>
    /// Configuration settings for importing documents to memory.
    /// </summary>
    private readonly DocumentMemoryOptions _documentImportOptions;

    /// <summary>
    /// Create a new instance of DocumentMemorySkill.
    /// </summary>
    public DocumentMemorySkill(
        IOptions<PromptsOptions> promptOptions,
        IOptions<DocumentMemoryOptions> documentImportOptions,
        ILogger logger)
    {
        this._logger = logger;
        this._promptOptions = promptOptions.Value;
        this._documentImportOptions = documentImportOptions.Value;
    }

    /// <summary>
    /// Query the document memory collection for documents that match the query.
    /// </summary>
    /// <param name="query">Query to match.</param>
    /// <param name="context">The SkContext.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [SKFunction, Description("Query documents in the memory given a user message")]
    public async Task<string> QueryDocumentsAsync(
        [Description("Query to match.")] string query,
        [Description("ID of the chat that owns the documents")] string chatId,
        [Description("Maximum number of tokens")] int tokenLimit,
        ISemanticTextMemory textMemory,
        CancellationToken cancellationToken = default)
    {
        var remainingToken = tokenLimit;

        // Search for relevant document snippets.
        string[] documentCollections = new string[]
        {
            this._documentImportOptions.ChatDocumentCollectionNamePrefix + chatId,
            this._documentImportOptions.GlobalDocumentCollectionName
        };

        List<MemoryQueryResult> relevantMemories = new();
        foreach (var documentCollection in documentCollections)
        {
#pragma warning disable CA1031 // Each connector may throw different exception type
            try
            {
                var results = textMemory.SearchAsync(
                    documentCollection,
                    query,
                    limit: 100,
                    minRelevanceScore: this._promptOptions.DocumentMemoryMinRelevance,
                    cancellationToken: cancellationToken);
                await foreach (var memory in results)
                {
                    relevantMemories.Add(memory);
                }
            }
            catch (Exception connectorException)
            {
                // A store exception might be thrown if the collection does not exist, depending on the memory store connector.
                this._logger.LogError(connectorException, "Cannot search collection {0}", documentCollection);
            }
#pragma warning restore CA1031 // Each connector may throw different exception type
        }

        relevantMemories = relevantMemories.OrderByDescending(m => m.Relevance).ToList();

        // Concatenate the relevant document snippets.
        string documentsText = string.Empty;
        foreach (var memory in relevantMemories)
        {
            var tokenCount = TokenUtilities.TokenCount(memory.Metadata.Text);
            if (remainingToken - tokenCount > 0)
            {
                documentsText += $"\n\nSnippet from {memory.Metadata.Description}: {memory.Metadata.Text}";
                remainingToken -= tokenCount;
            }
            else
            {
                break;
            }
        }

        if (string.IsNullOrEmpty(documentsText))
        {
            // No relevant documents found
            return string.Empty;
        }

        return $"User has also shared some document snippets:\n{documentsText}";
    }
}
