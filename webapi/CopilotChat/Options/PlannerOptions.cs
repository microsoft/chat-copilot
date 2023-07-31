// Copyright (c) Microsoft. All rights reserved.
using System.ComponentModel.DataAnnotations;
using SemanticKernel.Service.CopilotChat.Models;

namespace SemanticKernel.Service.CopilotChat.Options;

/// <summary>
/// Configuration options for the planner.
/// </summary>
public class PlannerOptions
{
    public const string PropertyName = "Planner";

    /// <summary>
    /// Define if the planner must be Sequential or not.
    /// </summary>
    [Required]
    public PlanType Type { get; set; } = PlanType.Action;

    /// <summary>
    /// Whether to retry plan creation if LLM returned response that doesn't contain valid plan (invalid XML or JSON).
    /// </summary>
    public bool AllowRetriesOnInvalidPlans { get; set; } = false;

    /// <summary>
    /// The minimum relevancy score for a function to be considered during plan creation
    /// when using SequentialPlanner
    /// </summary>
    [Range(0, 1.0)]
    public double? RelevancyThreshold { get; set; } = 0;
}
