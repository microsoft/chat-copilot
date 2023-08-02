// Copyright (c) Microsoft. All rights reserved.
using System.ComponentModel.DataAnnotations;
using SemanticKernel.Service.CopilotChat.Models;

namespace SemanticKernel.Service.CopilotChat.Options;

// <summary>
// Configuration options for the planner.
// </summary>
public class PlannerOptions
{
    public const string PropertyName = "Planner";

    // <summary>
    // Define if the planner must be Sequential or not.
    // </summary>
    [Required]
    public PlanType Type { get; set; } = PlanType.Action;

    // <summary>
    // The minimum relevancy score for a function to be considered during plan creation
    // when using SequentialPlanner
    // </summary>
    [Range(0, 1.0)]
    public double? RelevancyThreshold { get; set; } = 0;

    // <summary>
    // Whether to allow missing functions in the plan on creation then sanitize output. Functions are considered missing if they're not available in the planner's kernel's context.
    // If set to true, the plan will be created with missing functions as no-op steps that are filtered from the final proposed plan.
    // If this is set to false, the plan creation will fail if any functions are missing.
    // </summary>
    public bool SkipOnMissingFunctionsError { get; set; } = true;

    // <summary>
    // Whether to retry plan creation if LLM returned response that doesn't contain valid plan (e.g., invalid XML or JSON, contains missing function, etc.).
    // </summary>
    public bool AllowRetriesOnInvalidPlan { get; set; } = true;
}
