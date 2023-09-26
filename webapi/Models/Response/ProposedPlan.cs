// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.Planning;

namespace CopilotChat.WebApi.Models.Response;

// Type of Plan
public enum PlanType
{
    Action, // single-step
    Sequential, // multi-step
    Stepwise, // MRKL style planning
}

// State of Plan
public enum PlanState
{
    NoOp, // Plan has not received any user input
    Approved,
    Rejected,
    Derived, // Plan has been derived from a previous plan; used when user wants to re-run a plan.
}

/// <summary>
/// Information about a single proposed plan.
/// </summary>
public class ProposedPlan
{
    /// <summary>
    /// Plan object to be approved, rejected, or executed.
    /// </summary>
    [JsonPropertyName("proposedPlan")]
    public Plan Plan { get; set; }

    /// <summary>
    /// Indicates whether plan is Action (single-step) or Sequential (multi-step).
    /// </summary>
    [JsonPropertyName("type")]
    public PlanType Type { get; set; }

    /// <summary>
    /// State of plan
    /// </summary>
    [JsonPropertyName("state")]
    public PlanState State { get; set; }

    /// <summary>
    /// User intent that serves as goal of plan.
    /// </summary>
    [JsonPropertyName("userIntent")]
    public string UserIntent { get; set; }

    /// <summary>
    /// Original user input that prompted this plan.
    /// </summary>
    [JsonPropertyName("originalUserInput")]
    public string OriginalUserInput { get; set; }

    /// <summary>
    /// Id tracking bot message of plan in chat history when it was first generated.
    /// </summary>
    [JsonPropertyName("generatedPlanMessageId")]
    public string? GeneratedPlanMessageId { get; set; } = null;

    /// <summary>
    /// Create a new proposed plan.
    /// </summary>
    /// <param name="plan">Proposed plan object</param>
    public ProposedPlan(Plan plan, PlanType type, PlanState state, string userIntent, string originalUserInput, string? generatedPlanMessageId = null)
    {
        this.Plan = plan;
        this.Type = type;
        this.State = state;
        this.UserIntent = userIntent;
        this.OriginalUserInput = originalUserInput;
        this.GeneratedPlanMessageId = generatedPlanMessageId;
    }
}
