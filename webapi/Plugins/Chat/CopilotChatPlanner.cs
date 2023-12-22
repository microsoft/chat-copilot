// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning.Handlebars;

namespace CopilotChat.WebApi.Plugins.Chat;

/// <summary>
/// A lightweight wrapper around a planner to allow for curating which functions are available to it.
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
    public Kernel Kernel { get; }

    /// <summary>
    /// Options for the planner.
    /// </summary>
    private readonly PlannerOptions? _plannerOptions;

    /// <summary>
    /// Gets the pptions for the planner.
    /// </summary>
    public PlannerOptions? PlannerOptions => this._plannerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotChatPlanner"/> class.
    /// </summary>
    /// <param name="plannerKernel">The planner's kernel.</param>
    public CopilotChatPlanner(Kernel plannerKernel, PlannerOptions? plannerOptions, ILogger logger)
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
    public async Task<HandlebarsPlan> CreatePlanAsync(string goal, ILogger logger, CancellationToken cancellationToken = default)
    {
        var plannerFunctionsMetadata = this.Kernel.Plugins.GetFunctionsMetadata();
        if (plannerFunctionsMetadata.IsNullOrEmpty())
        {
            // No functions are available - return an empty plan.
            return new HandlebarsPlan(goal);
        }

        HandlebarsPlan plan;

        try
        {
            var config = new HandlebarsPlannerOptions()
            {
                MaxTokens = this._plannerOptions?.HandlebarsPlannerConfig.MaxTokens ?? 2048,
            };
            plan = await new HandlebarsPlanner().CreatePlanAsync(this.Kernel, goal, cancellationToken);
        }
        catch (KernelException)
        {
            // No relevant functions are available - return an empty plan.
            return new HandlebarsPlan(goal);
        }

        return this._plannerOptions!.ErrorHandling.AllowMissingFunctions ? this.SanitizePlan(plan, plannerFunctionsMetadata, logger) : plan;
    }

    /// <summary>
    /// Scrubs plan of functions not available in planner's kernel
    /// and flags any effected input dependencies with '$???' to prompt for user input.
    /// <param name="plan">Proposed plan object to sanitize.</param>
    /// <param name="availableFunctions">The functions available in the planner's kernel.</param>
    /// <param name="logger">Logger from context.</param>
    /// </summary>
    private HandlebarsPlan SanitizePlan(HandlebarsPlan plan, IEnumerable<KernelFunctionMetadata> availableFunctions, ILogger logger)
    { // TODO: [Issue #2256] Re-evaluate this logic once we have a better understanding of how to handle missing functions
        List<HandlebarsPlan> sanitizedSteps = new();
        List<string> availableOutputs = new();
        List<string> unavailableOutputs = new();

        // foreach (var step in plan.Steps)
        // {
        //     // Check if function exists in planner's kernel
        //     if (this.Kernel.Plugins.TryGetFunction(step.PluginName, step.Name, out var function))
        //     {
        //         availableOutputs.AddRange(step.Outputs);

        //         // Regex to match variable names
        //         Regex variableRegEx = new(VariableRegex, RegexOptions.Singleline);

        //         // Check for any inputs that may have dependencies from removed steps
        //         foreach (var input in step.Parameters)
        //         {
        //             // Check if input contains a variable
        //             Match inputVariableMatch = variableRegEx.Match(input.Value);
        //             if (inputVariableMatch.Success)
        //             {
        //                 foreach (Capture match in inputVariableMatch.Groups[1].Captures)
        //                 {
        //                     var inputVariableValue = match.Value;
        //                     if (!availableOutputs.Any(output => string.Equals(output, inputVariableValue, StringComparison.OrdinalIgnoreCase)))
        //                     {
        //                         var overrideValue =
        //                             // Use previous step's output if no direct dependency on unavailable functions' outputs
        //                             // Else use designated constant for unknowns to prompt for user input
        //                             string.Equals("INPUT", input.Key, StringComparison.OrdinalIgnoreCase)
        //                             && inputVariableMatch.Groups[1].Captures.Count == 1
        //                             && !unavailableOutputs.Any(output => string.Equals(output, inputVariableValue, StringComparison.OrdinalIgnoreCase))
        //                                 ? "$PLAN.RESULT" // TODO: [Issue #2256] Extract constants from HandlebarsPlan class, requires change on kernel team
        //                                 : UnknownVariableFlag;
        //                         step.Parameters.Set(input.Key, Regex.Replace(input.Value, variableRegEx.ToString(), overrideValue));
        //                     }
        //                 }
        //             }
        //         }
        //         sanitizedSteps.Add(step);
        //     }
        //     else
        //     {
        //         logger.LogWarning("Function {0} not found in planner's kernel. Removing step from plan.", step.Description);
        //         unavailableOutputs.AddRange(step.Outputs);
        //     }
        // }

        // HandlebarsPlan sanitizedPlan = new(plan.Description, sanitizedSteps.ToArray<HandlebarsPlan>());

        // // Merge any parameters back into new plan object
        // foreach (var parameter in plan.Parameters)
        // {
        //     sanitizedPlan.Parameters[parameter.Key] = parameter.Value;
        // }

        return plan;
    }
}
