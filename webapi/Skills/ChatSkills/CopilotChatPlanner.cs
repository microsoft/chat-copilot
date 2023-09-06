// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Orchestration;
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
    /// High level logger.
    /// </summary>
    private readonly ILogger _logger;

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
    private const string UnknownVariableFlag = "$???";

    /// <summary>
    /// Regex to match variable names from plan parameters.
    /// Valid variable names can contain letters, numbers, underscores, and dashes but can't start with a number.
    /// Matches: $variableName, $variable_name, $variable-name, $some_variable_Name, $variableName123, $variableName_123, $variableName-123
    /// Does not match: $123variableName, $100 $200
    /// </summary>
    private const string VariableRegex = @"\$([A-Za-z]+[_-]*[\w]+)";

    /// <summary>
    /// Supplemental text to add to the plan goal if PlannerOptions.Type is set to Stepwise.
    /// Helps the planner know when to bail out to request additional user input.
    /// </summary>
    private const string StepwisePlannerSupplement = "If you need more information to fulfill this request, return with a request for additional user input.";

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotChatPlanner"/> class.
    /// </summary>
    /// <param name="plannerKernel">The planner's kernel.</param>
    public CopilotChatPlanner(IKernel plannerKernel, PlannerOptions? plannerOptions, ILogger logger)
    {
        this.Kernel = plannerKernel;
        this._plannerOptions = plannerOptions;
        this._logger = logger;
    }

    /// <summary>
    /// Create a plan for a goal.
    /// </summary>
    /// <param name="goal">The goal to create a plan for.</param>
    /// <param name="logger">Logger from context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The plan.</returns>
    public async Task<Plan> CreatePlanAsync(string goal, ILogger logger, CancellationToken cancellationToken = default)
    {
        FunctionsView plannerFunctionsView = this.Kernel.Skills.GetFunctionsView(true, true);
        if (plannerFunctionsView.NativeFunctions.IsEmpty && plannerFunctionsView.SemanticFunctions.IsEmpty)
        {
            // No functions are available - return an empty plan.
            return new Plan(goal);
        }

        Plan plan;

        try
        {
            switch (this._plannerOptions?.Type)
            {
                case PlanType.Sequential:
                    plan = await new SequentialPlanner(
                        this.Kernel,
                        new SequentialPlannerConfig
                        {
                            RelevancyThreshold = this._plannerOptions?.RelevancyThreshold,
                            // Allow plan to be created with missing functions
                            AllowMissingFunctions = this._plannerOptions?.ErrorHandling.AllowMissingFunctions ?? false
                        }
                    ).CreatePlanAsync(goal, cancellationToken);
                    break;
                default:
                    plan = await new ActionPlanner(this.Kernel).CreatePlanAsync(goal, cancellationToken);
                    break;
            }
        }
        catch (SKException)
        {
            // No relevant functions are available - return an empty plan.
            return new Plan(goal);
        }

        return this._plannerOptions!.ErrorHandling.AllowMissingFunctions ? this.SanitizePlan(plan, plannerFunctionsView, logger) : plan;
    }

    /// <summary>
    /// Run the stepwise planner.
    /// </summary>
    /// <param name="goal">The goal containing user intent and ask context.</param>
    /// <param name="context">The context to run the plan in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<SKContext> RunStepwisePlannerAsync(string goal, SKContext context, CancellationToken cancellationToken = default)
    {
        var config = new Microsoft.SemanticKernel.Planning.Stepwise.StepwisePlannerConfig()
        {
            MaxTokens = this._plannerOptions?.StepwisePlannerConfig.MaxTokens ?? 2048,
            MaxIterations = this._plannerOptions?.StepwisePlannerConfig.MaxIterations ?? 15,
            MinIterationTimeMs = this._plannerOptions?.StepwisePlannerConfig.MinIterationTimeMs ?? 1500
        };

        Stopwatch sw = new();
        sw.Start();

        try
        {
            var plan = new StepwisePlanner(
                this.Kernel,
                config
            ).CreatePlan(string.Join("\n", goal, StepwisePlannerSupplement));
            var result = await plan.InvokeAsync(context, cancellationToken: cancellationToken);

            sw.Stop();
            result.Variables.Set("timeTaken", sw.Elapsed.ToString());
            return result;
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Error running stepwise planner");
            throw;
        }
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
                Regex variableRegEx = new(VariableRegex, RegexOptions.Singleline);

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
                                        : UnknownVariableFlag;
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
