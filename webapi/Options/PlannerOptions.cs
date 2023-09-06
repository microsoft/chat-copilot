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
    /// Options to handle planner errors.
    /// </summary>
    public class PlannerErrorOptions
    {
        /// <summary>
        /// Whether to allow retries on planner errors.
        /// </summary>
        public bool AllowRetries { get; set; } = true;

        /// <summary>
        /// Whether to allow missing functions.
        /// </summary>
        public bool AllowMissingFunctions { get; set; } = true;

        /// <summary>
        /// Max retries allowed.
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
    /// Options on how to handle planner errors.
    /// </summary>
    public PlannerErrorOptions PlannerError { get; set; } = new PlannerErrorOptions();

    /// <summary>
    /// The configuration for the stepwise planner.
    /// </summary>
    [RequiredOnPropertyValue(nameof(Type), PlanType.Stepwise)]
    public StepwisePlannerConfig StepwisePlannerConfig { get; set; } = new StepwisePlannerConfig();
}
