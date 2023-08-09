// Copyright (c) Microsoft. All rights reserved.
using System.ComponentModel.DataAnnotations;
using CopilotChat.WebApi.Models.Response;
using Microsoft.SemanticKernel.Planning.Stepwise;

namespace CopilotChat.WebApi.Options;

/// <summary>
/// Configuration options for the planner.
/// </summary>
public class PlannerOptions
{
    /// <summary>
    /// Whether to allow missing functions in plan on creation. If allowed, proposed plan will be sanitized of no-op functions.
    /// Functions are considered missing if they're not available in the planner's kernel's context.
    /// </summary>
    public class MissingFunctionErrorOptions
    {
        /// <summary>
        /// Flag to indicate if skips are allowed on MissingFunction error
        /// If set to true, the plan will be created with missing functions as no-op steps that are filtered from the final proposed plan.
        /// If this is set to false, the plan creation will fail if any functions are missing.
        /// </summary>
        public bool AllowRetries { get; set; } = true;

        /// <summary>
        /// Max retries allowed on MissingFunctionsError.
        /// </summary>
        [Range(1, 5)]
        public int MaxRetriesAllowed { get; set; } = 3;
    }

    public const string PropertyName = "Planner";

    /// <summary>
    /// The type of planner to used to create plan.
    /// </summary>
    [Required]
    public PlanType Type { get; set; } = PlanType.Action;

    /// <summary>
    /// The minimum relevancy score for a function to be considered during plan creation when using SequentialPlanner
    /// </summary>
    [Range(0, 1.0)]
    public double? RelevancyThreshold { get; set; } = 0;

    /// <summary>
    /// Options on how to handle missing functions in plan on creation.
    /// </summary>
    public MissingFunctionErrorOptions MissingFunctionError { get; set; } = new MissingFunctionErrorOptions();

    /// <summary>
    /// Whether to retry plan creation if LLM returned response that doesn't contain valid plan (e.g., invalid XML or JSON, contains missing function, etc.).
    /// </summary>
    public bool AllowRetriesOnInvalidPlan { get; set; } = true;

    /// <summary>
    /// The configuration for the stepwise planner.
    /// </summary>
    [RequiredOnPropertyValue(nameof(Type), PlanType.Stepwise)]
    public StepwisePlannerConfig StepwisePlannerConfig { get; set; } = new StepwisePlannerConfig();
}
