// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning;
using PlayFab.Examples.Common.Configuration;
using PlayFab.Examples.Common.Logging;
using PlayFab.Skills.SegmentSkill;

namespace PlayFab.Examples.Example02_Generative;

public static class Example02_GenerativeSegments
{
    public static async Task RunAsync()
    {
        var goals = new string[]
            {
                "Create a segment for the players who not logged in the last 30 days?",
                "Create a segment for the players first logged in date greater than 2023-08-01?",
                "Create a segment for the players last logged in date less than 2023-05-01?",
                "Create a segment for the players located in the Egypt?",
                "Create a segment for the players in china and grant them 10 VC virtual currency?",
                "Create a segment for the players in china who first logged in the last 30 days and grant them 10 virtual currency?",
                "Create a segment for the players located in the Egypt with entered segment action of email notification with email template id of 32EA0620DC453040?", // With entered segment action                
            };

        foreach (string prompt in goals)
        {
            try
            {
                await CreateSegmentExample(prompt);
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    private static async Task CreateSegmentExample(string goal)
    {
        // Create a segment skill
        Console.WriteLine("======== Action Planner ========");
        var kernel = new KernelBuilder()
            .WithLogger(ConsoleLogger.Logger)
            .WithAzureTextCompletionService(TestConfiguration.AzureOpenAI.DeploymentName, TestConfiguration.AzureOpenAI.Endpoint, TestConfiguration.AzureOpenAI.ApiKey)
            .Build();

        var kernelWithChat = new KernelBuilder().WithLogger(ConsoleLogger.Logger)
            .WithAzureChatCompletionService(TestConfiguration.AzureOpenAI.ChatDeploymentName, TestConfiguration.AzureOpenAI.Endpoint, TestConfiguration.AzureOpenAI.ApiKey, alsoAsTextCompletion: true, setAsDefault: true)
            .Build();

        kernel.ImportSkill(new SegmentSkill(kernelWithChat, TestConfiguration.PlayFab.Endpoint, TestConfiguration.PlayFab.TitleSecretKey, TestConfiguration.PlayFab.SwaggerEndpoint), "SegmentSkill");

        // Create an instance of ActionPlanner.
        // The ActionPlanner takes one goal and returns a single function to execute.
        var planner = new ActionPlanner(kernel);

        // We're going to ask the planner to find a function to achieve this goal.
        //var goal = "Create a segment with name NewPlayersSegment for the players first logged in date greater than 2023-08-01?";
        Console.WriteLine("Goal: " + goal);

        // The planner returns a plan, consisting of a single function
        // to execute and achieve the goal requested.
        var plan = await planner.CreatePlanAsync(goal);

        // Execute the full plan (which is a single function)
        SKContext result = await plan.InvokeAsync(kernel.CreateNewContext());

        // Show the result, which should match the given goal
        Console.WriteLine(result);
    }
}
