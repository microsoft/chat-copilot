// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Orchestration;

namespace SemanticKernel.Service.CopilotChat.Models;

/// <summary>
/// Information about token usage used to generate bot response.
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// Semantic functions required to generate bot response prompt.
    /// </summary>
    public static readonly Dictionary<string, string> semanticDependencies = new()
    {
        { "ExtractAudienceAsync", "audienceExtraction" },
        { "ExtractUserIntentAsync", "userIntentExtraction" },
        { "AcquireExternalInformationAsync", "planner" },
        { "ExtractCognitiveMemoryAsync", "memoryExtraction" }
    };

    /// <summary>
    /// Total token usage of prompt chat completion.
    /// </summary>
    [JsonPropertyName("prompt")]
    public int PromptTotal { get; set; }

    /// <summary>
    /// Total token usage across all semantic dependencies used to generate prompt.
    /// </summary>
    [JsonPropertyName("dependency")]
    public int DependencyTotal { get; set; }

    public TokenUsage(int promptToken, int dependencyTotal)
    {
        this.PromptTotal = promptToken;
        this.DependencyTotal = dependencyTotal;
    }

    internal static bool TryGetFunctionKey(SKContext chatContext, string? functionName, out string? functionKey)
    {
        if (functionName == null || !semanticDependencies.TryGetValue(functionName, out string? key))
        {
            chatContext.Log.LogError("Unknown token dependency {0}. Please define function as semanticDependencies entry in TokenUsage.cs", functionName);
            functionKey = null;
            return false;
        };

        functionKey = $"{key}TokenUsage";
        return true;
    }

    /// <summary>
    /// Get the total token usage from ChatCompletion result context and adds it as a variable to response context.
    /// </summary>
    /// <param name="result">Result context of chat completion</param>
    /// <param name="chatContext">Context maintained during response generation.</param>
    /// <param name="functionName">Name of the function that invoked the chat completion.</param>
    /// <param name="includePrevious">Flag to indicate whether current token usage should be additive.</param>
    internal static void CalculateChatCompletionTokenUsage(SKContext result, SKContext chatContext, string? functionName = null, bool includePrevious = false)
    {
        if (!TryGetFunctionKey(chatContext, functionName, out string? functionKey))
        {
            return;
        }

        if (result.ModelResults == null || result.ModelResults.Count == 0)
        {
            chatContext.Log.LogError("Unable to determine token usage for {0}", functionKey);
            return;
        }

        int previousSum = includePrevious && chatContext.Variables.TryGetValue(functionKey!, out string? cumulativeTokenUsage) ? int.Parse(cumulativeTokenUsage, CultureInfo.CurrentCulture) : 0;

        var tokenUsage = result.ModelResults.First().GetResult<ChatCompletions>().Usage.TotalTokens;
        chatContext.Variables.Set(functionKey!, (tokenUsage + previousSum).ToString(CultureInfo.InvariantCulture));
    }
}
