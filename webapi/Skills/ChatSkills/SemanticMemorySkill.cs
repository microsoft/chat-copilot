// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CopilotChat.WebApi.Extensions;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticMemory;

namespace CopilotChat.WebApi.Skills.ChatSkills;

/// <summary>
/// This skill provides the functions to query semantic memory.
/// </summary>
public class SemanticMemorySkill
{
    private readonly PromptsOptions _promptOptions;

    private readonly ChatSessionRepository _chatSessionRepository;

    private readonly ISemanticMemoryClient _memoryClient;

    /// <summary>
    /// High level logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Create a new instance of SemanticChatMemorySkill.
    /// </summary>
    public SemanticMemorySkill(
        IOptions<PromptsOptions> promptOptions,
        ChatSessionRepository chatSessionRepository,
        ISemanticMemoryClient memoryClient,
        ILogger logger)
    {
        this._promptOptions = promptOptions.Value;
        this._chatSessionRepository = chatSessionRepository;
        this._memoryClient = memoryClient;
        this._logger = logger;
    }

    /// <summary>
    /// Query relevant memories based on the query.
    /// </summary>
    /// <returns>A string containing the relevant memories.</returns>
    [SKFunction, Description("Query chat memories")]
    public async Task<string> QueryMemoriesAsync(
        [Description("Query to match.")] string query,
        [Description("Chat ID to query history from")] string chatId,
        [Description("Maximum number of tokens")] int tokenLimit)
    {
        ChatSession? chatSession = null;
        if (!await this._chatSessionRepository.TryFindByIdAsync(chatId, v => chatSession = v))
        {
            throw new ArgumentException($"Chat session {chatId} not found.");
        }

        var remainingToken = tokenLimit;

        // Search for relevant memories.
        List<(Citation Citation, Citation.Partition Memory)> relevantMemories = new();
        foreach (var memoryName in this._promptOptions.MemoryMap.Keys.Append(this._promptOptions.DocumentMemoryName))
        {
            await SearchMemoryAsync(memoryName).ConfigureAwait(false);
        }

        var builderMemory = new StringBuilder();

        if (relevantMemories.Count > 0)
        {
            var memoryMap = ProcessMemories();

            foreach (var memoryName in this._promptOptions.MemoryMap.Keys)
            {
                FormatMemories(memoryName);
            }

            FormatSnippets();

            void FormatMemories(string memoryName)
            {
                if (!memoryMap.TryGetValue(memoryName, out var memories))
                {
                    return;
                }

                foreach (var memory in memories)
                {
                    var memoryText = $"[{memoryName}] {memory}\n";
                    builderMemory.Append(memoryText);
                }
            }

            void FormatSnippets()
            {
                if (!memoryMap.TryGetValue(this._promptOptions.DocumentMemoryName, out var memories))
                {
                    return;
                }

                foreach ((var memory, var citation) in memories)
                {
                    var memoryText = $"Source name: {citation.SourceName}\nContent:\n[CONTENT START]\n{memory}\n[CONTENT END]";
                    builderMemory.Append(memoryText);
                }
            }
        }

        var memoryText = builderMemory.ToString();

        if (string.IsNullOrWhiteSpace(memoryText))
        {
            // No relevant memories found
            return string.Empty;
        }

        return $"Relevant memories:\n{memoryText}";

        async Task SearchMemoryAsync(string memoryName)
        {
            try
            {
                var searchResult =
                    await this._memoryClient.SearchMemoryAsync(
                        this._promptOptions.MemoryIndexName,
                        query,
                        this.CalculateRelevanceThreshold(memoryName, chatSession!.MemoryBalance),
                        chatId,
                        memoryName)
                    .ConfigureAwait(false);

                foreach (var result in searchResult.Results.SelectMany(c => c.Partitions.Select(p => (c, p))))
                {
                    relevantMemories.Add(result);
                }
            }
            catch (SKException connectorException)
            {
                // A store exception might be thrown if the collection does not exist, depending on the memory store connector.
                this._logger.LogError(connectorException, "Cannot search collection {0}", this._promptOptions.MemoryIndexName);
            }
        }

        IDictionary<string, List<(string, Citation)>> ProcessMemories()
        {
            var memoryMap = new Dictionary<string, List<(string, Citation)>>(StringComparer.OrdinalIgnoreCase);

            foreach (var result in relevantMemories.OrderByDescending(m => m.Memory.Relevance))
            {
                var tokenCount = TokenUtilities.TokenCount(result.Memory.Text);
                if (remainingToken - tokenCount > 0)
                {
                    var memoryName = result.Citation.Tags[ISemanticMemoryClientExtensions.TagMemory].Single()!;
                    if (!memoryMap.TryGetValue(memoryName, out var memories))
                    {
                        memories = new List<(string, Citation)>();
                        memoryMap.Add(memoryName, memories);
                    }

                    memories.Add((result.Memory.Text, result.Citation));

                    remainingToken -= tokenCount;
                }
                else
                {
                    break;
                }
            }

            return memoryMap;
        }
    }

    #region Private

    /// <summary>
    /// Calculates the relevance threshold for the memory.
    /// The relevance threshold is a function of the memory balance.
    /// The memory balance is a value between 0 and 1, where 0 means maximum focus on
    /// working term memory (by minimizing the relevance threshold for working memory
    /// and maximizing the relevance threshold for long term memory), and 1 means
    /// maximum focus on long term memory (by minimizing the relevance threshold for
    /// long term memory and maximizing the relevance threshold for working memory).
    /// The memory balance controls two 1st degree polynomials defined by the lower
    /// and upper bounds, one for long term memory and one for working memory.
    /// The relevance threshold is the value of the polynomial at the memory balance.
    /// </summary>
    /// <param name="memoryName">The name of the memory.</param>
    /// <param name="memoryBalance">The balance between long term memory and working term memory.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown when the memory name is invalid.</exception>
    private float CalculateRelevanceThreshold(string memoryName, float memoryBalance)
    {
        var upper = this._promptOptions.SemanticMemoryRelevanceUpper;
        var lower = this._promptOptions.SemanticMemoryRelevanceLower;

        if (memoryBalance < 0.0 || memoryBalance > 1.0)
        {
            throw new ArgumentException($"Invalid memory balance: {memoryBalance}");
        }

        if (memoryName == this._promptOptions.LongTermMemoryName)
        {
            return (lower - upper) * memoryBalance + upper;
        }
        else if (memoryName == this._promptOptions.WorkingMemoryName)
        {
            return (upper - lower) * memoryBalance + lower;
        }
        else if (memoryName == this._promptOptions.DocumentMemoryName)
        {
            return this._promptOptions.DocumentMemoryMinRelevance;
        }
        else
        {
            throw new ArgumentException($"Invalid memory name: {memoryName}");
        }
    }

    # endregion
}
