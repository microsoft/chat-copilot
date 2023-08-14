// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Information about a pass through stepwise planner.
/// </summary>
public class StepwiseThoughtProcess
{
    /// <summary>
    /// Steps taken execution stat.
    /// </summary>
    [JsonPropertyName("stepsTaken")]
    public string StepsTaken { get; set; } = string.Empty;

    /// <summary>
    /// Time taken to fulfil the goal.
    /// Format: hh:mm:ss
    /// </summary>
    [JsonPropertyName("timeTaken")]
    public string TimeTaken { get; set; } = string.Empty;

    /// <summary>
    /// Skills used execution stat.
    /// </summary>
    [JsonPropertyName("skillsUsed")]
    public string SkillsUsed { get; set; } = string.Empty;

    /// <summary>
    /// Planner type.
    /// </summary>
    [JsonPropertyName("plannerType")]
    public PlanType PlannerType { get; set; } = PlanType.Stepwise;

    public StepwiseThoughtProcess(string stepsTaken, string timeTaken, string skillsUsed)
    {
        this.StepsTaken = stepsTaken;
        this.TimeTaken = timeTaken;
        this.SkillsUsed = skillsUsed;
    }
}
