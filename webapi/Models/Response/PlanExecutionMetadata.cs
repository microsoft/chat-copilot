// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// Metadata about plan execution.
/// </summary>
public class PlanExecutionMetadata
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
    /// Functions used execution stat.
    /// </summary>
    [JsonPropertyName("functionsUsed")]
    public string FunctionsUsed { get; set; } = string.Empty;

    /// <summary>
    /// Planner type.
    /// </summary>
    [JsonPropertyName("plannerType")]
    public PlanType PlannerType { get; set; } = PlanType.Stepwise;

    /// <summary>
    /// Raw result of the planner.
    /// </summary>
    [JsonIgnore]
    public string RawResult { get; set; } = string.Empty;

    public PlanExecutionMetadata(string stepsTaken, string timeTaken, string functionsUsed, string rawResult)
    {
        this.StepsTaken = stepsTaken;
        this.TimeTaken = timeTaken;
        this.FunctionsUsed = functionsUsed;
        this.RawResult = rawResult;
    }
}
