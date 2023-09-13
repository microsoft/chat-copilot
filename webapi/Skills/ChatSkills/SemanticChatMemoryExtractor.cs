// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Extensions;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;

namespace CopilotChat.WebApi.Skills.ChatSkills;

/// <summary>
/// Helper class to extract and create semantic memory from chat history.
/// </summary>
internal static class SemanticChatMemoryExtractor
{
    /// <summary>
    /// Returns the name of the semantic text memory collection that stores chat semantic memory.
    /// </summary>
    /// <param name="chatId">Chat ID that is persistent and unique for the chat session.</param>
    /// <param name="memoryName">Name of the memory category</param>
    internal static string MemoryCollectionName(string chatId, string memoryName) => $"{chatId}-{memoryName}";

    /// <summary>
    /// Extract and save semantic memory.
    /// </summary>
    /// <param name="chatId">The Chat ID.</param>
    /// <param name="kernel">The semantic kernel.</param>
    /// <param name="context">The Semantic Kernel context.</param>
    /// <param name="options">The prompts options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    internal static async Task ExtractSemanticChatMemoryAsync(
        string chatId,
        IKernel kernel,
        SKContext context,
        PromptsOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        foreach (var memoryName in options.MemoryMap.Keys)
        {
            try
            {
                var semanticMemory = await ExtractCognitiveMemoryAsync(
                    memoryName,
                    kernel,
                    context,
                    options,
                    logger
                );
                foreach (var item in semanticMemory.Items)
                {
                    await CreateMemoryAsync(item, chatId, kernel.Memory, memoryName, options, logger, cancellationToken);
                }
            }
            catch (Exception ex) when (!ex.IsCriticalException())
            {
                // Skip semantic memory extraction for this item if it fails.
                // We cannot rely on the model to response with perfect Json each time.
                logger.LogInformation("Unable to extract semantic memory for {0}: {1}. Continuing...", memoryName, ex.Message);
                continue;
            }
        }
    }

    /// <summary>
    /// Extracts the semantic chat memory from the chat session.
    /// </summary>
    /// <param name="memoryName">Name of the memory category</param>
    /// <param name="kernel">The semantic kernel.</param>
    /// <param name="context">The SKContext</param>
    /// <param name="options">The prompts options.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>A SemanticChatMemory object.</returns>
    internal static async Task<SemanticChatMemory> ExtractCognitiveMemoryAsync(
        string memoryName,
        IKernel kernel,
        SKContext context,
        PromptsOptions options,
        ILogger logger)
    {
        if (!options.MemoryMap.TryGetValue(memoryName, out var memoryPrompt))
        {
            throw new ArgumentException($"Memory name {memoryName} is not supported.");
        }

        // Token limit for chat history
        var tokenLimit = options.CompletionTokenLimit;
        var remainingToken =
            tokenLimit -
            options.ResponseTokenLimit -
            TokenUtilities.TokenCount(memoryPrompt); ;

        var memoryExtractionContext = context.Clone();
        memoryExtractionContext.Variables.Set("tokenLimit", remainingToken.ToString(new NumberFormatInfo()));
        memoryExtractionContext.Variables.Set("memoryName", memoryName);
        memoryExtractionContext.Variables.Set("format", options.MemoryFormat);
        memoryExtractionContext.Variables.Set("knowledgeCutoff", options.KnowledgeCutoffDate);

        var completionFunction = kernel.CreateSemanticFunction(memoryPrompt);
        var result = await completionFunction.InvokeAsync(
            context: memoryExtractionContext,
            settings: CreateMemoryExtractionSettings(options)
        );

        // Get token usage from ChatCompletion result and add to context
        // Since there are multiple memory types, total token usage is calculated by cumulating the token usage of each memory type.
        TokenUtilities.GetFunctionTokenUsage(result, context, logger, $"SystemCognitive_{memoryName}");

        SemanticChatMemory memory = SemanticChatMemory.FromJson(result.ToString());
        return memory;
    }

    /// <summary>
    /// Create a memory item in the memory collection.
    /// If there is already a memory item that has a high similarity score with the new item, it will be skipped.
    /// </summary>
    /// <param name="item">A SemanticChatMemoryItem instance</param>
    /// <param name="chatId">The ID of the chat the memories belong to</param>
    /// <param name="ISemanticTextMemory">The semantic memory instance</param>
    /// <param name="memoryName">Name of the memory</param>
    /// <param name="options">The prompts options.</param>
    /// <param name="logger">Logger</param>
    /// <param name="cancellationToken">Cancellation token</param>
    internal static async Task CreateMemoryAsync(
        SemanticChatMemoryItem item,
        string chatId,
        ISemanticTextMemory semanticTextMemory,
        string memoryName,
        PromptsOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var memoryCollectionName = SemanticChatMemoryExtractor.MemoryCollectionName(chatId, memoryName);

#pragma warning disable CA1031 // Each connector may throw different exception type
        try
        {
            // Search if there is already a memory item that has a high similarity score with the new item.
            var memories = await semanticTextMemory.SearchAsync(
                    collection: memoryCollectionName,
                    query: item.ToFormattedString(),
                    limit: 1,
                    minRelevanceScore: options.SemanticMemoryRelevanceUpper,
                    cancellationToken: cancellationToken
                )
                .ToListAsync()
                .ConfigureAwait(false);

            if (memories.Count == 0)
            {
                await semanticTextMemory.SaveInformationAsync(
                    collection: memoryCollectionName,
                    text: item.ToFormattedString(),
                    id: Guid.NewGuid().ToString(),
                    description: memoryName,
                    cancellationToken: cancellationToken
                );
            }
        }
        catch (Exception connectorException)
        {
            // A store exception might be thrown if the collection does not exist, depending on the memory store connector.
            logger.LogError(connectorException, "Cannot search collection {0}", memoryCollectionName);
        }
#pragma warning restore CA1031 // Each connector may throw different exception type
    }

    /// <summary>
    /// Create a completion settings object for chat response. Parameters are read from the PromptSettings class.
    /// </summary>
    private static CompleteRequestSettings CreateMemoryExtractionSettings(PromptsOptions options)
    {
        var completionSettings = new CompleteRequestSettings
        {
            MaxTokens = options.ResponseTokenLimit,
            Temperature = options.ResponseTemperature,
            TopP = options.ResponseTopP,
            FrequencyPenalty = options.ResponseFrequencyPenalty,
            PresencePenalty = options.ResponsePresencePenalty
        };

        return completionSettings;
    }
}
