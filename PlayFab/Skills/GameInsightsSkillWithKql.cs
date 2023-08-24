// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Kusto.Data.Exceptions;
using Kusto.Data.Linq;
using Microsoft.Azure.Cosmos;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using PlayFab.Adx;
using PlayFab.Reports;

namespace PlayFab.Skills;

/// <summary>
/// A semantic skill that knows to get answers for game related questions by analyzing their game-data
/// </summary>
public class GameInsightsSkillWithKql
{
    #region Static Data
    /// <summary>
    /// The system prompt for a chat that creates python scripts to solve analytic problems
    /// </summary>
    private static readonly string CreateKqlSystemPrompt = @"
You're an AI chatbot that helps users turn a natural language question into Kusto Query Language (KQL).
You can use one or more of the suggested Input-Tables to answer the user's question in your suggested kql answer.

Today Date: {{$date.today}}

[Input Tables Start]
{{$tableDescriptions}}
[Input Tables End]
";

    #endregion

    #region Data Members
    /// <summary>
    /// The semantic memory containing relevant reports needed to solve the provided question
    /// </summary>
    private readonly ISemanticTextMemory _memory;

    /// <summary>
    /// An open AI client
    /// </summary>
    private readonly OpenAIClient _openAIClient;

    /// <summary>
    /// The name of the Azure Open AI chat deployment
    /// </summary>
    private readonly string _azureOpenAIChatDeploymentName;

    /// <summary>
    /// The azure data explorer client
    /// </summary>
    private readonly IAdxClient _adxClient;
    #endregion

    #region Constructor
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="memory">The semantic memory containing relevant reports needed to solve the provided question</param>
    /// <param name="azureOpenAIEndpoint">The endpoint of the Azure Open AI chat</param>
    /// <param name="azureOpenAIKey">The key of the Azure Open AI chat</param>
    /// <param name="azureOpenAIChatDeploymentName">The name of the Azure Open AI chat deployment</param>
    public GameInsightsSkillWithKql(
        ISemanticTextMemory memory,
        string azureOpenAIEndpoint,
        string azureOpenAIKey,
        string azureOpenAIChatDeploymentName,
        IAdxClient adxClient)
    {
        this._memory = memory ?? throw new ArgumentNullException(nameof(memory));
        this._openAIClient = new(
            new Uri(azureOpenAIEndpoint),
            new AzureKeyCredential(azureOpenAIKey));
        this._azureOpenAIChatDeploymentName = azureOpenAIChatDeploymentName;
        this._adxClient = adxClient ?? throw new ArgumentNullException(nameof(adxClient));
    }
    #endregion

    #region Public Methods

    [SKFunction, SKName("GetAnswerForGameQuestion"), Description("Answers questions about game's data and its players around engagement, usage, time spent and game analytics")]
    public async Task<string> GetAnswerForGameQuestionAsync(
        [Description("The question related to the provided inline data.")]
        string question,
        SKContext context)
    {
        StringBuilder tableDescriptions = new();
        IAsyncEnumerable<MemoryQueryResult> memories = this._memory.SearchAsync("TitleID-Reports", question, limit: 1, minRelevanceScore: 0.7);
        int idx = 1;

        List<PlayFabReport> playFabReports = new();
        StringBuilder pythonScriptScriptActualPrefix = new();
        await foreach (MemoryQueryResult memory in memories)
        {
            PlayFabReport playFabReport = JsonSerializer.Deserialize<PlayFabReport>(memory.Metadata.AdditionalMetadata);
            playFabReports.Add(playFabReport);

            tableDescriptions.AppendLine($"[KQL Table {idx++}:]");
            tableDescriptions.AppendLine("Table Name: " + playFabReport.ReportName);
            tableDescriptions.AppendLine("Columns: ");
            tableDescriptions.AppendLine(playFabReport.GetDetailedDescription());
            tableDescriptions.AppendLine();
        }

        if (playFabReports.Count == 0)
        {
            return "I don't have enough data to answer this question.";
        }

        var chatCompletion = new ChatCompletionsOptions()
        {
            Messages =
                {
                    new ChatMessage(
                        ChatRole.System,
                        CreateKqlSystemPrompt
                            .Replace("{{$tableDescriptions}}", tableDescriptions.ToString())
                            .Replace("{{$date.today}}", "2023-02-01" /*DateTime.UtcNow.ToString("yyyy/MM/dd")*/)),
                new ChatMessage(
                    ChatRole.User,
                    $"Respond with a step-by-step approach and the final KQL query (separate the query with ```). Translate this request to KQL: # {question} #")
                },
            Temperature = 0.0f,
            MaxTokens = 8000,
            NucleusSamplingFactor = 0.5f,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
        };

        int retry = 0;
        while (retry++ < 3)
        {
            Azure.Response<ChatCompletions> response = await this._openAIClient.GetChatCompletionsAsync(
                deploymentOrModelName: this._azureOpenAIChatDeploymentName, chatCompletion);

            string rawResponse = response.Value.Choices.Single().Message.Content;
            string kql = ExtractKqlScript(rawResponse);

            // Replace "sum(MAU) as FranceMAU" to "FranceMAU=sum(MAU)"
            string safeKql = Regex.Replace(kql, @"(\S+)\s+as\s+(\w+)", "$2=$1", RegexOptions.Multiline);

            // Limit query to relevant data
            foreach (var playfabReport in playFabReports)
            {
                safeKql = safeKql.Replace(playfabReport.ReportName, playfabReport.KqlSafeData);
            }

            // Run the Adx query
            try
            {
                AdxTableResult[] kqlResult = await _adxClient.ExecuteQueryAsync(safeKql, CancellationToken.None);
                string output = @$"
Result:
{ConvertToCsv(kqlResult)}

Query used:
{kql}

Chain of thought:
{ExtractExplanation(rawResponse)}
";
                return output;
            }
            catch (KustoBadRequestException ex)
            {
                if ((ex is SemanticException semanticException) && semanticException.Message.Contains("Top-level expressions must return tabular results"))
                {
                    chatCompletion.Messages.Add(new ChatMessage(ChatRole.Assistant, kql));
                    chatCompletion.Messages.Add(new ChatMessage(
                        ChatRole.User,
                        "Add print at the last line of the query in order to fix this error" + Environment.NewLine + ex.Message));
                    continue;
                }

                if (retry == 2)
                {
                    chatCompletion.Messages.Add(new ChatMessage(
                        ChatRole.User,
                        $"Can you suggest a complete new KQL query approach to solve the question (separate the query with ```). Translate this request to KQL with a new logic: # {question} #"));
                    continue;
                }

                // If there are errors in the script, try to fix them
                chatCompletion.Messages.Add(new ChatMessage(ChatRole.Assistant, kql));
                chatCompletion.Messages.Add(new ChatMessage(
                    ChatRole.User,
                    $"The following error/s occured: {ex.Message}.\n Modify the parts of the query that relate to the errors"));
            }
        }

        return "Couldn't get an answer";

    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Extract the KQL script from the given input text
    /// Example:
    /// 
    /// Input:
    /// In order to do that you can run this query
    /// ```
    /// Table | where...
    /// '''
    /// This should give you the result you wanted
    ///
    /// Output: Table | where...
    /// </summary>
    /// <param name="inputText">The input containing the input kql query</param>
    /// <returns>The kql query</returns>
    private static string ExtractKqlScript(string inputText)
    {
        // Extract Python script using regular expressions
        string[] patterns = new string[] { @"```(?:\s*)kql(.*?)```", @"```(.*?)```" };

        foreach (var pattern in patterns)
        {
            MatchCollection matches = Regex.Matches(inputText, pattern, RegexOptions.Singleline);
            if (matches.Count == 0)
            {
                continue;
            }

            Match? lastMatch = matches.LastOrDefault();
            if (lastMatch != null)
            {
                string kqlCode = lastMatch.Groups[1].Value.Trim();
                return kqlCode;
            }
        }

        return inputText;
    }

    /// <summary>
    /// Parses the OpenAI response by commenting out the non-query lines and removing the ``` delimiters.
    /// Example:
    /// 
    /// Input:
    /// In order to do that you can run this query
    /// ```
    /// Table | where...
    /// '''
    /// This should give you the result you wanted
    ///
    /// Output:
    /// In order to do that you can run this query
    /// This should give you the result you wanted
    /// </summary>
    /// <param name='response'>
    /// Represents the response by OpenAI.
    /// </param>
    /// <returns>A string representing a step-by-step explanation of the generated KQL, with comments (//) prefacing each line for display in ADX iframe </returns>
    public static string ExtractExplanation(string response)
    {
        if (!response.Contains("```"))
        {
            return string.Empty;
        }

        List<string> responseLines = response.Split('\n').ToList();
        List<string> documentationLines = new();
        bool isCode = false;
        foreach (string responseLine in responseLines)
        {
            if (responseLine.Contains("```"))
            {
                isCode = !isCode;
            }
            else if (!isCode && !responseLine.Contains("final KQL query"))
            {
                documentationLines.Add(responseLine);
            }
        }

        return string.Join('\n', documentationLines);
    }

    /// <summary>
    /// Convert the given tables to CSV format
    /// </summary>
    /// <param name="tables">The tables to format as CSV</param>
    /// <returns>The formatted text</returns>
    public static string ConvertToCsv(AdxTableResult[] tables)
    {
        var csvBuilder = new StringBuilder();

        if (tables != null && tables.Any())
        {
            foreach (AdxTableResult table in tables)
            {
                IReadOnlyList<string> headers = table.Headers;

                // Append header row
                csvBuilder.AppendLine(string.Join(",", headers));

                foreach (AdxRowResult row in table)
                {
                    var rowValues = new List<string>();

                    foreach (string? header in headers)
                    {
                        var value = row.ContainsKey(header) ? row[header]?.ToString() : "";
                        rowValues.Add($"\"{value}\"");
                    }

                    csvBuilder.AppendLine(string.Join(",", rowValues));
                }
            }

            csvBuilder.AppendLine();
        }

        return csvBuilder.ToString();
    }

    #endregion
}
