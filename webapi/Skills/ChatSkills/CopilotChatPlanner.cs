// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Sequential;
using Microsoft.SemanticKernel.SkillDefinition;

namespace CopilotChat.WebApi.Skills.ChatSkills;

/// <summary>
/// A lightweight wrapper around a planner to allow for curating which skills are available to it.
/// </summary>
public class CopilotChatPlanner
{
    /// <summary>
    /// The planner's kernel.
    /// </summary>
    public IKernel Kernel { get; }

    /// <summary>
    /// Options for the planner.
    /// </summary>
    private readonly PlannerOptions? _plannerOptions;

    /// <summary>
    /// Gets the pptions for the planner.
    /// </summary>
    public PlannerOptions? PlannerOptions => this._plannerOptions;

    /// <summary>
    /// Flag to indicate that a variable is unknown and needs to be filled in by the user.
    /// This is used to flag any inputs that had dependencies from removed steps.
    /// </summary>
    private const string UNKNOWN_VARIABLE_FLAG = "$???";

    /// <summary>
    /// Regex to match variable names from plan parameters.
    /// Valid variable names can contain letters, numbers, underscores, and dashes but can't start with a number.
    /// Matches: $variableName, $variable_name, $variable-name, $some_variable_Name, $variableName123, $variableName_123, $variableName-123
    /// Does not match: $123variableName, $100 $200
    /// </summary>
    private const string VARIABLE_REGEX = @"\$([A-Za-z]+[_-]*[\w]+)";

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotChatPlanner"/> class.
    /// </summary>
    /// <param name="plannerKernel">The planner's kernel.</param>
    public CopilotChatPlanner(IKernel plannerKernel, PlannerOptions? plannerOptions)
    {
        this.Kernel = plannerKernel;
        this._plannerOptions = plannerOptions;
    }

    /// <summary>
    /// Create a plan for a goal.
    /// </summary>
    /// <param name="goal">The goal to create a plan for.</param>
    /// <param name="logger">Logger from context.</param>
    /// <returns>The plan.</returns>
    public async Task<Plan> CreatePlanAsync(string goal, ILogger logger)
    {
        FunctionsView plannerFunctionsView = this.Kernel.Skills.GetFunctionsView(true, true);
        if (plannerFunctionsView.NativeFunctions.IsEmpty && plannerFunctionsView.SemanticFunctions.IsEmpty)
        {
            // No functions are available - return an empty plan.
            return new Plan(goal);
        }

        Plan plan = this._plannerOptions?.Type == PlanType.Sequential
               ? await new SequentialPlanner(
                    this.Kernel,
                    new SequentialPlannerConfig
                    {
                        RelevancyThreshold = this._plannerOptions?.RelevancyThreshold,
                        // Allow plan to be created with missing functions
                        AllowMissingFunctions = this._plannerOptions?.MissingFunctionError.AllowRetries ?? false
                    }
                ).CreatePlanAsync(goal)
               : await new ActionPlanner(this.Kernel).CreatePlanAsync(goal);

        return this._plannerOptions!.MissingFunctionError.AllowRetries ? this.SanitizePlan(plan, plannerFunctionsView, logger) : plan;
    }

    #region Private

    /// <summary>
    /// Scrubs plan of functions not available in planner's kernel
    /// and flags any effected input dependencies with '$???' to prompt for user input.
    /// <param name="plan">Proposed plan object to sanitize.</param>
    /// <param name="availableFunctions">The functions available in the planner's kernel.</param>
    /// <param name="logger">Logger from context.</param>
    /// </summary>
    private Plan SanitizePlan(Plan plan, FunctionsView availableFunctions, ILogger logger)
    { // TODO: [Issue #2256] Re-evaluate this logic once we have a better understanding of how to handle missing functions
        List<Plan> sanitizedSteps = new();
        List<string> availableOutputs = new();
        List<string> unavailableOutputs = new();

        foreach (var step in plan.Steps)
        {
            // Check if function exists in planner's kernel
            if (this.Kernel.Skills.TryGetFunction(step.SkillName, step.Name, out var function))
            {
                availableOutputs.AddRange(step.Outputs);

                // Regex to match variable names
                Regex variableRegEx = new(VARIABLE_REGEX, RegexOptions.Singleline);

                // Check for any inputs that may have dependencies from removed steps
                foreach (var input in step.Parameters)
                {
                    // Check if input contains a variable
                    Match inputVariableMatch = variableRegEx.Match(input.Value);
                    if (inputVariableMatch.Success)
                    {
                        foreach (Capture match in inputVariableMatch.Groups[1].Captures)
                        {
                            var inputVariableValue = match.Value;
                            if (!availableOutputs.Any(output => string.Equals(output, inputVariableValue, StringComparison.OrdinalIgnoreCase)))
                            {
                                var overrideValue =
                                    // Use previous step's output if no direct dependency on unavailable functions' outputs
                                    // Else use designated constant for unknowns to prompt for user input
                                    string.Equals("INPUT", input.Key, StringComparison.OrdinalIgnoreCase)
                                    && inputVariableMatch.Groups[1].Captures.Count == 1
                                    && !unavailableOutputs.Any(output => string.Equals(output, inputVariableValue, StringComparison.OrdinalIgnoreCase))
                                        ? "$PLAN.RESULT" // TODO: [Issue #2256] Extract constants from Plan class, requires change on kernel team
                                        : UNKNOWN_VARIABLE_FLAG;
                                step.Parameters.Set(input.Key, Regex.Replace(input.Value, variableRegEx.ToString(), overrideValue));
                            }
                        }
                    }
                }
                sanitizedSteps.Add(step);
            }
            else
            {
                logger.LogWarning("Function {0} not found in planner's kernel. Removing step from plan.", step.Description);
                unavailableOutputs.AddRange(step.Outputs);
            }
        }

        Plan sanitizedPlan = new(plan.Description, sanitizedSteps.ToArray<Plan>());

        // Merge any parameters back into new plan object
        sanitizedPlan.Parameters.Update(plan.Parameters);

        return sanitizedPlan;
    }

    #endregion
}
