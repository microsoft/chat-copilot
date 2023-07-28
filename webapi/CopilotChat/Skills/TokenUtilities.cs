// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Microsoft.SemanticKernel.Orchestration;

namespace SemanticKernel.Service.CopilotChat.Skills;

/// <summary>
/// Utility methods for token management.
/// </summary>
public static class TokenUtilities
{
    /// <summary>
    /// Semantic dependencies of ChatSkill.
    ///  If you add a new semantic dependency, please add it here.
    /// </summary>
    public static readonly Dictionary<string, string> semanticFunctions = new()
    {
        // TODO: [Issue #2106] Calculate token usage for planner dependencies.
        { "SystemAudienceExtraction", "audienceExtraction" },
        { "SystemIntentExtraction", "userIntentExtraction" },
        { "SystemMetaPrompt", "metaPromptTemplate" },
        { "SystemCompletion", "responseCompletion"},
        { "SystemCognitive_WorkingMemory", "workingMemoryExtraction" },
        { "SystemCognitive_LongTermMemory", "longTermMemoryExtraction" }
    };

    /// <summary>
    /// Gets dictionary containing empty token usage totals.
    /// Use for responses that are hardcoded and/or do not have semantic (token) dependencies.
    /// </summary>
    internal static Dictionary<string, int> EmptyTokenUsages()
    {
        return semanticFunctions.Values.ToDictionary(v => v, v => 0, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets key used to identify function token usage in context variables.
    /// </summary>
    /// <param name="logger">The logger instance to use for logging errors.</param>
    /// <param name="functionName">Name of semantic function.</param>
    /// <returns>The key corresponding to the semantic function name, or null if the function name is unknown.</returns>
    internal static string? GetFunctionKey(ILogger logger, string? functionName)
    {
        if (functionName == null || !semanticFunctions.TryGetValue(functionName, out string? key))
        {
            logger.LogError("Unknown token dependency {0}. Please define function as semanticFunctions entry in TokenUtilities.cs", functionName);
            return null;
        };

        return $"{key}TokenUsage";
    }

    /// <summary>
    /// Gets the total token usage from a Chat or Text Completion result context and adds it as a variable to response context.
    /// </summary>
    /// <param name="result">Result context of chat completion</param>
    /// <param name="chatContext">Context maintained during response generation.</param>
    /// <param name="functionName">Name of the function that invoked the chat completion.</param>
    /// <returns> true if token usage is found in result context; otherwise, false.</returns>
    internal static void GetFunctionTokenUsage(SKContext result, SKContext chatContext, string? functionName = null)
    {
        var functionKey = GetFunctionKey(chatContext.Log, functionName);
        if (functionKey == null)
        {
            return;
        }

        if (result.ModelResults == null || result.ModelResults.Count == 0)
        {
            chatContext.Log.LogError("Unable to determine token usage for {0}", functionKey);
            return;
        }

        var tokenUsage = result.ModelResults.First().GetResult<ChatCompletions>().Usage.TotalTokens;
        chatContext.Variables.Set(functionKey!, tokenUsage.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Calculate the number of tokens in a string.
    /// </summary>
    internal static int TokenCount(string text) => GPT3Tokenizer.Encode(text).Count;
}
