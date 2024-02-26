// Copyright (c) Microsoft. All rights reserved.
using System.ComponentModel.DataAnnotations;
using CopilotChat.WebApi.Models.Response;
using Microsoft.SemanticKernel.Planners;

namespace CopilotChat.WebApi.Options;

/// <summary>
/// Configuration options for the planner.
/// </summary>
public class PlannerOptions
{
    /// <summary>
    /// Options to handle planner errors.
    /// </summary>
    public class ErrorOptions
    {
        /// <summary>
        /// Whether to allow retries on planner errors.
        /// </summary>
        public bool AllowRetries { get; set; } = true;

        // <summary>
        // Whether to allow missing functions in the sequential plan on creation. If set to true, the
        // plan will be created with missing functions as no-op steps. If set to false (default),
        // the plan creation will fail if any functions are missing.
        // </summary>
        public bool AllowMissingFunctions { get; set; } = true;

        /// <summary>
        /// Max retries allowed.
        /// </summary>
        [Range(1, 5)]
        public int MaxRetriesAllowed { get; set; } = 3;
    }

    /// <summary>
    /// Options to authenticate plugins supporting OBO Auth.
    /// </summary>
    public class OboOptions
    {
        /// <summary>
        /// The authority to use for OBO Auth.
        /// </summary>
        public string? Authority { get; set; }

        /// <summary>
        /// The Tenant Id to use for OBO Auth.
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// The Client Id to use for OBO Auth.
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// The Client Secret to use for OBO Auth.
        /// </summary>
        public string? ClientSecret { get; set; }
    }

    public const string PropertyName = "Planner";

    /// <summary>
    /// The model name used by the planner.
    /// </summary>
    [Required]
    public string Model { get; set; } = string.Empty;

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
    /// The maximum number of seconds to wait for a response from a plugin.
    /// If this is not set, timeout limit will be 100s, which is the default timeout setting for HttpClient.
    /// </summary>
    [Range(0, int.MaxValue)]
    public double PluginTimeoutLimitInS { get; set; } = 100;

    /// <summary>
    /// Options on how to handle planner errors.
    /// </summary>
    public ErrorOptions ErrorHandling { get; set; } = new ErrorOptions();

    /// <summary>
    /// Optional flag to indicate whether to use the planner result as the bot response.
    /// </summary>
    [RequiredOnPropertyValue(nameof(Type), PlanType.Stepwise)]
    public bool UseStepwiseResultAsBotResponse { get; set; } = false;

    /// <summary>
    /// The configuration for the stepwise planner.
    /// </summary>
    [RequiredOnPropertyValue(nameof(Type), PlanType.Stepwise)]
    public StepwisePlannerConfig StepwisePlannerConfig { get; set; } = new StepwisePlannerConfig();

    /// <summary>
    /// The OBO configuration for plugins that support OBO Auth.
    /// </summary>
    public OboOptions? OnBehalfOfAuth { get; set; }
}
