// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Reliability;
using PlayFab.Examples.Common.Configuration;
using PlayFab.Examples.Common.Logging;
using PlayFab.Reports;
using PlayFab.Skills;

namespace PlayFab.Examples.Example01.DataQnA;

public enum PlannerType
{
    Stepwise,
    ChatStepwise,
    SimpleAction
}

internal class Example01_DataQnA
{
    #region Public Methods
    /// <summary>
    /// Runs this excample
    /// </summary>
    /// <returns>The async task</returns>
    public static async Task RunAsync()
    {
        CancellationToken cancellationToken = CancellationToken.None;
        string[] questions = new string[]
        {
            "How can I increase my in-game item sales?",
            "Which country (excluding 'unknown') should I focus on the most to improve retention?",
            "What is my 2-day retention average? Was my 2-day retention in the last few days better or worse than that?",
            "How many players played my game yesterday?",
            "What is the average number of players I had last week excluding Friday and Monday?",
            "Is my game doing better in USA or in China?",
            "If the number of monthly active players in France increases by 30%, what would be the percentage increase to the overall monthly active players?",
            "At which specific times of the day were the highest and lowest numbers of purchases recorded? Please provide the actual sales figures for these particular time slots.",
            "Which three items had the highest total sales and which had the highest revenue generated?",
        };

        PlannerType[] planners = new[]
        {
            // PlannerType.Stepwise,
            // PlannerType.ChatStepwise,
            PlannerType.SimpleAction
        };

        // We're using volotile memory, so pre-load it with data
        IKernel kernel = GetKernel();
        await InitializeKernelMemoryAsync(kernel.Memory, TestConfiguration.PlayFab.TitleId, cancellationToken);
        InitializeKernelSkills(kernel);

        foreach (string question in questions)
        {
            foreach (PlannerType planner in planners)
            {
                await Console.Out.WriteLineAsync("--------------------------------------------------------------------------------------------------------------------");
                await Console.Out.WriteLineAsync("Planner: " + planner);
                await Console.Out.WriteLineAsync("Question: " + question);
                await Console.Out.WriteLineAsync("--------------------------------------------------------------------------------------------------------------------");

                try
                {
                    await RunWithQuestionAsync(kernel, question, PlannerType.SimpleAction, cancellationToken);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Run a question against the given kernel instance
    /// </summary>
    /// <param name="kernel">The semantic kernel to use</param>
    /// <param name="question">The question that needs to be answered</param>
    /// <param name="plannerType">The type of the planner to use</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The async task</returns>
    private static async Task RunWithQuestionAsync(IKernel kernel, string question, PlannerType plannerType, CancellationToken cancellationToken)
    {
        Plan plan;
        Stopwatch sw = Stopwatch.StartNew();
        if (plannerType == PlannerType.SimpleAction)
        {
            var planner = new ActionPlanner(kernel);
            plan = await planner.CreatePlanAsync(question, cancellationToken);
        }
        else if (plannerType == PlannerType.ChatStepwise)
        {
            throw new Exception("ChatStepwise planner not yet supported in SK");
            /*var plannerConfig = new Microsoft.SemanticKernel.Planning.Stepwise.StepwisePlannerConfig();
            plannerConfig.ExcludedFunctions.Add("TranslateMathProblem");
            plannerConfig.MinIterationTimeMs = 1500;
            plannerConfig.MaxTokens = 12000;

            ChatStepwisePlanner planner = new(kernel, plannerConfig);

            plan = planner.CreatePlan(question);*/
        }
        else if (plannerType == PlannerType.Stepwise)
        {
            var plannerConfig = new Microsoft.SemanticKernel.Planning.Stepwise.StepwisePlannerConfig();
            plannerConfig.ExcludedFunctions.Add("TranslateMathProblem");
            plannerConfig.MinIterationTimeMs = 1500;
            plannerConfig.MaxTokens = 1500;

            StepwisePlanner planner = new(kernel, plannerConfig);

            plan = planner.CreatePlan(question);
        }
        else
        {
            throw new NotSupportedException($"[{plannerType}] Planner type is not supported.");
        }

        SKContext result = await plan.InvokeAsync(kernel.CreateNewContext(), cancellationToken: cancellationToken);
        Console.WriteLine("Answer: ");
        Console.WriteLine(result.ToString());
        if (result.Variables.TryGetValue("stepCount", out string? stepCount))
        {
            Console.WriteLine("Steps Taken: " + stepCount);
        }

        if (result.Variables.TryGetValue("skillCount", out string? skillCount))
        {
            Console.WriteLine("Skills Used: " + skillCount);
        }

        Console.WriteLine("Time Taken: " + sw.Elapsed);
        Console.WriteLine("");
    }

    /// <summary>
    /// Builds a new semantic kernel instance
    /// </summary>
    /// <returns>A semantic kernel instance</returns>
    private static IKernel GetKernel()
    {
        var builder = new KernelBuilder();

        if (!string.IsNullOrEmpty(TestConfiguration.AzureOpenAI.ChatDeploymentName))
        {
            builder = builder.WithAzureChatCompletionService(
                TestConfiguration.AzureOpenAI.ChatDeploymentName,
                TestConfiguration.AzureOpenAI.Endpoint,
                TestConfiguration.AzureOpenAI.ApiKey,
                alsoAsTextCompletion: true,
                setAsDefault: true);
        }

        if (!string.IsNullOrEmpty(TestConfiguration.AzureOpenAI.DeploymentName))
        {
            builder = builder.WithAzureTextCompletionService(
                TestConfiguration.AzureOpenAI.DeploymentName,
                TestConfiguration.AzureOpenAI.Endpoint,
                TestConfiguration.AzureOpenAI.ApiKey);
        }

        var kernel = builder
            .WithLogger(ConsoleLogger.Logger)
            .WithAzureTextEmbeddingGenerationService(
                deploymentName: TestConfiguration.AzureOpenAI.EmbeddingDeploymentName,
                endpoint: TestConfiguration.AzureOpenAI.Endpoint,
                apiKey: TestConfiguration.AzureOpenAI.ApiKey)
            .WithMemoryStorage(new VolatileMemoryStore())
            .Configure(c => c.SetDefaultHttpRetryConfig(new HttpRetryConfig
            {
                MaxRetryCount = 3,
                UseExponentialBackoff = true,
                MinRetryDelay = TimeSpan.FromSeconds(3),
            }))
            .Build();

        return kernel;
    }

    /// <summary>
    /// Initialize the semantic kernel skills
    /// </summary>
    /// <param name="kernel"></param>
    private static void InitializeKernelSkills(IKernel kernel)
    {
        kernel.ImportSkill(
            new GameInsightsSkill(
                kernel.Memory,
                TestConfiguration.AzureOpenAI.Endpoint,
                TestConfiguration.AzureOpenAI.ApiKey,
                TestConfiguration.AzureOpenAI.ChatDeploymentName),
            "GameInsightsSkill");

        // Maybe with gpt4 we can add more skills and make them more granular. Planners are instable with Gpt3.5 and complex analytic stesps.
        // kernel.ImportSkill(new GameReportFetcherSkill(kernel.Memory), "GameReportFetcher");
        // kernel.ImportSkill(new LanguageCalculatorSkill(kernel), "advancedCalculator");
        // var bingConnector = new BingConnector(TestConfiguration.Bing.ApiKey);
        // var webSearchEngineSkill = new WebSearchEngineSkill(bingConnector);
        // kernel.ImportSkill(webSearchEngineSkill, "WebSearch");
        // kernel.ImportSkill(new SimpleCalculatorSkill(kernel), "basicCalculator");
        // kernel.ImportSkill(new TimeSkill(), "time");
    }

    /// <summary>
    /// Initialize the kernel memory (since we're using volotile memory)
    /// </summary>
    /// <param name="memory">The memory that should be initialized</param>
    /// <param name="titleId">The tile ID whose data should be loaded to memory</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    private static async Task InitializeKernelMemoryAsync(
        ISemanticTextMemory memory, string titleId, CancellationToken cancellationToken)
    {
        DateTime today = DateTime.UtcNow.Date;
        var reportDataAccess = new ReportDataAccess(
            TestConfiguration.PlayFab.ReportsCosmosDBEndpoint,
            TestConfiguration.PlayFab.ReportsCosmosDBKey,
            TestConfiguration.PlayFab.ReportsCosmosDBDatabaseName,
            TestConfiguration.PlayFab.ReportsCosmosDBContainerName);

        ReportDataManager reportDataManager = new(reportDataAccess);

        IList<PlayFabReport> playFabReports = await reportDataManager.GetPlayFabReportsAsync(titleId, cancellationToken);

        foreach (PlayFabReport report in playFabReports)
        {
            string reportText = report.GetDetailedDescription();
            await memory.SaveInformationAsync(
                collection: "TitleID-Reports",
                text: reportText,
                id: report.ReportName,
                additionalMetadata: JsonSerializer.Serialize(report),
                cancellationToken: cancellationToken);
        }
    }
    #endregion
}
