// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Microsoft.SemanticKernel.Orchestration;
using ChatCompletionContextMessages = Microsoft.SemanticKernel.AI.ChatCompletion.ChatHistory;

namespace CopilotChat.WebApi.Skills;

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
    /// <param name="result">Result context from chat model</param>
    /// <param name="chatContext">Context maintained during response generation.</param>
    /// <param name="logger">The logger instance to use for logging errors.</param>
    /// <param name="functionName">Name of the function that invoked the chat completion.</param>
    /// <returns> true if token usage is found in result context; otherwise, false.</returns>
    internal static void GetFunctionTokenUsage(SKContext result, SKContext chatContext, ILogger logger, string? functionName = null)
    {
        var functionKey = GetFunctionKey(logger, functionName);
        if (functionKey == null)
        {
            return;
        }

        if (result.ModelResults == null || result.ModelResults.Count == 0)
        {
            logger.LogError("Unable to determine token usage for {0}", functionKey);
            return;
        }

        var tokenUsage = result.ModelResults.First().GetResult<ChatModelResult>().Usage.TotalTokens;
        chatContext.Variables.Set(functionKey!, tokenUsage.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Calculate the number of tokens in a string.
    /// </summary>
    /// <param name="text">The string to calculate the number of tokens in.</param>
    internal static int TokenCount(string text) => GPT3Tokenizer.Encode(text).Count;

    /// <summary>
    /// Rough token costing of ChatHistory's message object.
    /// Follows the syntax defined by Azure OpenAI's ChatMessage object: https://learn.microsoft.com/en-us/azure/ai-services/openai/reference#chatmessage
    /// e.g., "message": {"role":"assistant","content":"Yes }
    /// </summary>
    /// <param name="authorRole">Author role of the message.</param>
    /// <param name="content">Content of the message.</param>
    internal static int GetContextMessageTokenCount(AuthorRole authorRole, string content)
    {
        var tokenCount = authorRole == AuthorRole.System ? TokenCount("\n") : 0;
        return tokenCount + TokenCount($"role:{authorRole.Label}") + TokenCount($"content:{content}");
    }

    /// <summary>
    /// Rough token costing of ChatCompletionContextMessages object.
    /// </summary>
    /// <param name="chatHistory">ChatCompletionContextMessages object to calculate the number of tokens of.</param>
    internal static int GetContextMessagesTokenCount(ChatCompletionContextMessages chatHistory)
    {
        var tokenCount = 0;
        foreach (var message in chatHistory.Messages)
        {
            tokenCount += GetContextMessageTokenCount(message.Role, message.Content);
        }

        return tokenCount;
    }
}
