// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CopilotChat.WebApi.Models.Request;

namespace CopilotChat.WebApi.Options;

/// <summary>
/// Configuration options for the chat
/// </summary>
public class PromptsOptions
{
    public const string PropertyName = "Prompts";

    /// <summary>
    /// Token limit of the chat model.
    /// </summary>
    /// <remarks>https://platform.openai.com/docs/models/overview for token limits.</remarks>
    [Required, Range(0, int.MaxValue)] public int CompletionTokenLimit { get; set; }

    /// <summary>
    /// The token count left for the model to generate text after the prompt.
    /// </summary>
    [Required, Range(0, int.MaxValue)] public int ResponseTokenLimit { get; set; }

    /// <summary>
    /// Weight of memories in the contextual part of the final prompt.
    /// Contextual prompt excludes all the system commands and user intent.
    /// </summary>
    internal double MemoriesResponseContextWeight { get; } = 0.6;

    /// <summary>
    /// Weight of information returned from planner (i.e., responses from OpenAPI skills).
    /// Contextual prompt excludes all the system commands and user intent.
    /// </summary>
    internal double ExternalInformationContextWeight { get; } = 0.3;

    /// <summary>
    /// Upper bound of the relevancy score of a semantic memory to be included in the final prompt.
    /// The actual relevancy score is determined by the memory balance.
    /// </summary>
    internal float SemanticMemoryRelevanceUpper { get; } = 0.9F;

    /// <summary>
    /// Lower bound of the relevancy score of a semantic memory to be included in the final prompt.
    /// The actual relevancy score is determined by the memory balance.
    /// </summary>
    internal float SemanticMemoryRelevanceLower { get; } = 0.6F;

    /// <summary>
    /// Minimum relevance of a document memory to be included in the final prompt.
    /// The higher the value, the answer will be more relevant to the user intent.
    /// </summary>
    internal float DocumentMemoryMinRelevance { get; } = 0.8F;

    // System
    [Required, NotEmptyOrWhitespace] public string KnowledgeCutoffDate { get; set; } = string.Empty;
    [Required, NotEmptyOrWhitespace] public string InitialBotMessage { get; set; } = string.Empty;
    [Required, NotEmptyOrWhitespace] public string SystemDescription { get; set; } = string.Empty;
    [Required, NotEmptyOrWhitespace] public string SystemResponse { get; set; } = string.Empty;

    /// <summary>
    /// Context bot message for meta prompt when using external information acquired from a plan.
    /// </summary>
    [Required, NotEmptyOrWhitespace] public string ProposedPlanBotMessage { get; set; } = string.Empty;

    /// <summary>
    /// Supplement to help guide model in using data.
    /// </summary>
    [Required, NotEmptyOrWhitespace] public string PlanResultsDescription { get; set; } = string.Empty;

    /// <summary>
    /// Supplement to help guide model in using a response from StepwisePlanner.
    /// </summary>
    [Required, NotEmptyOrWhitespace] public string StepwisePlannerSupplement { get; set; } = string.Empty;

    internal string[] SystemAudiencePromptComponents => new string[]
    {
        this.SystemAudience,
        "{{ChatSkill.ExtractChatHistory}}",
        this.SystemAudienceContinuation
    };

    internal string SystemAudienceExtraction => string.Join("\n", this.SystemAudiencePromptComponents);

    internal string[] SystemIntentPromptComponents => new string[]
    {
        this.SystemDescription,
        this.SystemIntent,
        "{{ChatSkill.ExtractChatHistory}}",
        this.SystemIntentContinuation
    };

    internal string SystemIntentExtraction => string.Join("\n", this.SystemIntentPromptComponents);

    // Intent extraction
    [Required, NotEmptyOrWhitespace] public string SystemIntent { get; set; } = string.Empty;
    [Required, NotEmptyOrWhitespace] public string SystemIntentContinuation { get; set; } = string.Empty;

    // Audience extraction
    [Required, NotEmptyOrWhitespace] public string SystemAudience { get; set; } = string.Empty;
    [Required, NotEmptyOrWhitespace] public string SystemAudienceContinuation { get; set; } = string.Empty;

    // Memory storage
    [Required, NotEmptyOrWhitespace] public string MemoryIndexName { get; set; } = string.Empty;

    // Document memory
    [Required, NotEmptyOrWhitespace] public string DocumentMemoryName { get; set; } = string.Empty;

    // Memory extraction
    [Required, NotEmptyOrWhitespace] public string SystemCognitive { get; set; } = string.Empty;
    [Required, NotEmptyOrWhitespace] public string MemoryFormat { get; set; } = string.Empty;
    [Required, NotEmptyOrWhitespace] public string MemoryAntiHallucination { get; set; } = string.Empty;
    [Required, NotEmptyOrWhitespace] public string MemoryContinuation { get; set; } = string.Empty;

    // Long-term memory
    [Required, NotEmptyOrWhitespace] public string LongTermMemoryName { get; set; } = string.Empty;
    [Required, NotEmptyOrWhitespace] public string LongTermMemoryExtraction { get; set; } = string.Empty;

    internal string[] LongTermMemoryPromptComponents => new string[]
    {
        this.SystemCognitive,
        $"{this.LongTermMemoryName} Description:\n{this.LongTermMemoryExtraction}",
        this.MemoryAntiHallucination,
        $"Chat Description:\n{this.SystemDescription}",
        "{{ChatSkill.ExtractChatHistory}}",
        this.MemoryContinuation
    };

    internal string LongTermMemory => string.Join("\n", this.LongTermMemoryPromptComponents);

    // Working memory
    [Required, NotEmptyOrWhitespace] public string WorkingMemoryName { get; set; } = string.Empty;
    [Required, NotEmptyOrWhitespace] public string WorkingMemoryExtraction { get; set; } = string.Empty;

    internal string[] WorkingMemoryPromptComponents => new string[]
    {
        this.SystemCognitive,
        $"{this.WorkingMemoryName} Description:\n{this.WorkingMemoryExtraction}",
        this.MemoryAntiHallucination,
        $"Chat Description:\n{this.SystemDescription}",
        "{{ChatSkill.ExtractChatHistory}}",
        this.MemoryContinuation
    };

    internal string WorkingMemory => string.Join("\n", this.WorkingMemoryPromptComponents);

    // Memory map
    internal IDictionary<string, string> MemoryMap => new Dictionary<string, string>()
    {
        { this.LongTermMemoryName, this.LongTermMemory },
        { this.WorkingMemoryName, this.WorkingMemory }
    };

    // Chat commands
    internal string[] SystemPersonaComponents => new string[]
    {
        this.SystemDescription,
        this.SystemResponse,
    };

    internal string SystemPersona => string.Join("\n\n", this.SystemPersonaComponents);

    internal double ResponseTemperature { get; } = 0.7;
    internal double ResponseTopP { get; } = 1;
    internal double ResponsePresencePenalty { get; } = 0.5;
    internal double ResponseFrequencyPenalty { get; } = 0.5;

    internal double IntentTemperature { get; } = 0.7;
    internal double IntentTopP { get; } = 1;
    internal double IntentPresencePenalty { get; } = 0.5;
    internal double IntentFrequencyPenalty { get; } = 0.5;

    /// <summary>
    /// Copy the options in case they need to be modified per chat.
    /// </summary>
    /// <returns>A shallow copy of the options.</returns>
    internal PromptsOptions Copy() => (PromptsOptions)this.MemberwiseClone();

    /// <summary>
    /// Tries to retrieve the memoryContainerName associated with the specified memory type.
    /// </summary>
    internal bool TryGetMemoryContainerName(string memoryType, out string memoryContainerName)
    {
        memoryContainerName = "";
        if (!Enum.TryParse<SemanticMemoryType>(memoryType, true, out SemanticMemoryType semanticMemoryType))
        {
            return false;
        }

        switch (semanticMemoryType)
        {
            case SemanticMemoryType.LongTermMemory:
                memoryContainerName = this.LongTermMemoryName;
                return true;

            case SemanticMemoryType.WorkingMemory:
                memoryContainerName = this.WorkingMemoryName;
                return true;

            default: return false;
        }
    }
}
