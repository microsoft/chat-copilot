// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Extensions;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticMemory;

namespace CopilotChat.WebApi.Skills.ChatSkills;

/// <summary>
/// Helper class to extract and create semantic memory from chat history.
/// </summary>
internal static class SemanticChatMemoryExtractor
{
    /// <summary>
    /// Extract and save semantic memory.
    /// </summary>
    /// <param name="chatId">The Chat ID.</param>
    /// <param name="kernel">The semantic kernel.</param>
    /// <param name="context">The Semantic Kernel context.</param>
    /// <param name="options">The prompts options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task ExtractSemanticChatMemoryAsync(
        string chatId,
        ISemanticMemoryClient memoryClient,
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
                var semanticMemory = await ExtractCognitiveMemoryAsync(memoryName, logger);
                foreach (var item in semanticMemory.Items)
                {
                    await CreateMemoryAsync(memoryName, item.ToFormattedString());
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

        /// <summary>
        /// Extracts the semantic chat memory from the chat session.
        /// </summary>
        async Task<SemanticChatMemory> ExtractCognitiveMemoryAsync(string memoryName, ILogger logger)
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
                TokenUtilities.TokenCount(memoryPrompt);

            var memoryExtractionContext = context.Clone();
            memoryExtractionContext.Variables.Set("tokenLimit", remainingToken.ToString(new NumberFormatInfo()));
            memoryExtractionContext.Variables.Set("memoryName", memoryName);
            memoryExtractionContext.Variables.Set("format", options.MemoryFormat);
            memoryExtractionContext.Variables.Set("knowledgeCutoff", options.KnowledgeCutoffDate);

            var completionFunction = kernel.CreateSemanticFunction(memoryPrompt);
            var result = await completionFunction.InvokeAsync(
                memoryExtractionContext,
                options.ToCompletionSettings(),
                cancellationToken);

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
        async Task CreateMemoryAsync(string memoryName, string memory)
        {
            try
            {
                // Search if there is already a memory item that has a high similarity score with the new item.
                var searchResult =
                    await memoryClient.SearchMemoryAsync(
                        options.MemoryIndexName,
                        memory,
                        options.SemanticMemoryRelevanceUpper,
                        resultCount: 1,
                        chatId,
                        memoryName,
                        cancellationToken);

                if (searchResult.Results.Count == 0)
                {
                    await memoryClient.StoreMemoryAsync(options.MemoryIndexName, chatId, memoryName, memory, cancellationToken: cancellationToken);
                }
            }
            catch (Exception exception) when (!exception.IsCriticalException())
            {
                // A store exception might be thrown if the collection does not exist, depending on the memory store connector.
                logger.LogError(exception, "Unexpected failure searching {0}", options.MemoryIndexName);
            }
        }
    }

    /// <summary>
    /// Create a completion settings object for chat response. Parameters are read from the PromptSettings class.
    /// </summary>
    private static CompleteRequestSettings ToCompletionSettings(this PromptsOptions options)
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
