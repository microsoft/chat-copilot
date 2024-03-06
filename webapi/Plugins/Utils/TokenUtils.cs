// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CopilotChat.WebApi.Plugins.Utils;

/// <summary>
/// Utility methods for token management.
/// </summary>
public static class TokenUtils
{
    private static SharpToken.GptEncoding tokenizer = SharpToken.GptEncoding.GetEncoding("cl100k_base");

    /// <summary>
    /// Semantic dependencies of ChatPlugin.
    ///  If you add a new semantic dependency, please add it here.
    /// </summary>
    internal static readonly Dictionary<string, string> semanticFunctions = new()
    {
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
        return semanticFunctions.Values.ToDictionary(v => v, v => 0);
    }

    /// <summary>
    /// Gets key used to identify function token usage in context variables.
    /// </summary>
    /// <param name="functionName">Name of semantic function.</param>
    /// <returns>The key corresponding to the semantic function name, or null if the function name is unknown.</returns>
    internal static string GetFunctionKey(string? functionName)
    {
        if (functionName == null || !semanticFunctions.TryGetValue(functionName, out string? key))
        {
            throw new KeyNotFoundException($"Unknown token dependency {functionName}. Please define function as semanticFunctions entry in TokenUtils.cs");
        };

        return $"{key}TokenUsage";
    }

    /// <summary>
    /// Gets the total token usage from a Chat or Text Completion result context and adds it as a variable to response context.
    /// </summary>
    /// <param name="result">Result context from chat model</param>
    /// <param name="logger">The logger instance to use for logging errors.</param></param>
    /// <returns>String representation of number of tokens used by function (or null on error)</returns>
    internal static string? GetFunctionTokenUsage(FunctionResult result, ILogger logger)
    {
        if (result.Metadata is null ||
            !result.Metadata.TryGetValue("Usage", out object? usageObject) || usageObject is null)
        {
            logger.LogError("No usage metadata provided");

            return null;
        }

        var tokenUsage = 0;
        try
        {
            var jsonObject = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(usageObject));
            tokenUsage = jsonObject.GetProperty("TotalTokens").GetInt32();
        }
        catch (KeyNotFoundException)
        {
            logger.LogError("Usage details not found in model result.");

            return null;
        }

        return tokenUsage.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Calculate the number of tokens in a string using custom SharpToken token counter implementation with cl100k_base encoding.
    /// </summary>
    /// <param name="text">The string to calculate the number of tokens in.</param>
    internal static int TokenCount(string text)
    {
        var tokens = tokenizer.Encode(text);
        return tokens.Count;
    }

    /// <summary>
    /// Rough token costing of ChatHistory's message object.
    /// Follows the syntax defined by Azure OpenAI's ChatMessage object: https://learn.microsoft.com/en-us/azure/ai-services/openai/reference#chatmessage
    /// e.g., "message": {"role":"assistant","content":"Yes" }
    /// </summary>
    /// <param name="authorRole">Author role of the message.</param>
    /// <param name="content">Content of the message.</param>
    internal static int GetContextMessageTokenCount(AuthorRole authorRole, string? content)
    {
        return TokenCount($"role:{authorRole.Label}") + TokenCount($"content:{content}\n");
    }

    /// <summary>
    /// Rough token costing of ChatHistory object.
    /// </summary>
    /// <param name="chatHistory">ChatHistory object to calculate the number of tokens of.</param>
    internal static int GetContextMessagesTokenCount(ChatHistory chatHistory)
    {
        var tokenCount = 0;
        foreach (var message in chatHistory)
        {
            tokenCount += GetContextMessageTokenCount(message.Role, message.Content);
        }

        return tokenCount;
    }
}
