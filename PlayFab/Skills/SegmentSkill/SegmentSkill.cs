// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Skills.OpenAPI.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlayFab.Skills.SegmentSkill;

/// <summary>
/// Create a segment for given input prompt / question.
/// </summary>
public sealed class SegmentSkill
{
    #region Static Data

    private static readonly HttpClient HttpClient = new();

    /// <summary>
    /// Boolean indicating whether or not to import swagger configuration from file or url.
    /// </summary>
    private static readonly bool UseLocalFile = true;

    #endregion

    #region Data Members

    private readonly IKernel _kernel;

    /// <summary>
    /// The title specific PlayFab API Endpoint for requests.
    /// </summary>
    private readonly string _titleApiEndpoint;

    /// <summary>
    /// The title's secret key which is used for authenticating against the endpoint.
    /// </summary>
    private readonly string _titleSecretKey;

    /// <summary>
    /// Endpoint for retrieving PlayFab's swagger configuration
    /// </summary>
    private readonly string _swaggerEndpoint;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="memory">The semantic memory containing relevant reports needed to solve the provided question</param>
    public SegmentSkill(IKernel kernel, string titleApiEndpoint, string titleSecretKey, string swaggerEndpoint)
    {
        this._kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        this._titleApiEndpoint = titleApiEndpoint ?? throw new ArgumentNullException(nameof(titleApiEndpoint));
        this._titleSecretKey = titleSecretKey ?? throw new ArgumentNullException(nameof(titleSecretKey));
        this._swaggerEndpoint = swaggerEndpoint ?? throw new ArgumentNullException(nameof(swaggerEndpoint));
    }

    #endregion

    /// <summary>
    /// Create segment function for given input prompt / question.
    /// </summary>
    /// <param name="inputPrompt">Input prompt for create segment skill.</param>
    /// <returns>Status of segment creation.</returns>
    [SKFunction, Description("Creates a segment as specified by the users input prompt or question.")]
    public async Task<string> CreateSegment(
        [Description("The original user input.")]
        string input)
    {
        SKContext context = this._kernel.CreateNewContext();
        string miniJson = await this.GetMinifiedOpenApiJson();

        string FunctionDefinition = @"
You are an AI assistant for generating PlayFab input payload for given api. You have access to the full OpenAPI 3.0.1 specification.

Api Spec:
{{$apiSpec}}

The CreateSegment operation in PlayFab Admin API requires a CreateSegmentRequest payload input.
For FirstLoginFilter and LastLoginFilter, if the input value is days, convert value into minutes.
Segment model name should be meaningful name from the input question.
Don't provide any description about the answer. Only provide json payload content.
Don't provide notes like below.
Note: 30 days converted to minutes is 43200

Example:
Question: Create a segment for the players first logged in date greater than 2023-05-01?
Answer: 
{
  ""SegmentModel"": {
    ""Name"": ""FirstLoggedInPlayers"",
    ""SegmentOrDefinitions"": [
      {
        ""SegmentAndDefinitions"": [
          {
            ""FirstLoginDateFilter"": {
              ""LogInDate"": ""2023-05-01T00:00:00Z"",
              ""Comparison"": ""GreaterThan""
            }
          }
        ]
      }
    ]
  }
}

Question:
{{$input}}"
.Replace("{{$apiSpec}}", miniJson, StringComparison.OrdinalIgnoreCase);

        ISKFunction playfabJsonFunction = this._kernel.CreateSemanticFunction(FunctionDefinition, temperature: 0.1, topP: 0.1);
        SKContext result = await playfabJsonFunction.InvokeAsync(input);
        string payload = result.Result.Substring(result.Result.IndexOf('{'), result.Result.LastIndexOf('}') - result.Result.IndexOf('{') + 1);
        List<Segment> segments = await this.GetSegments();

        JObject segmentModelObject = JObject.Parse(payload);
        string segmentName = segmentModelObject.SelectToken("$.SegmentModel.Name").ToString();

        if(segments.Select(seg => seg.Name).Contains(segmentName))
        {
            segmentName = this.GenerateDistinctSegmentName(segmentName, segments);
            segmentModelObject["SegmentModel"]["Name"] = segmentName;
            payload = segmentModelObject.ToString();
        }

        Console.WriteLine(payload);

        // Step 2: Create a segment using above generated payload.
        ContextVariables contextVariables = new();
        contextVariables.Set("content_type", "application/json");
        contextVariables.Set("server_url", this._titleApiEndpoint);
        contextVariables.Set("content_type", "application/json");
        contextVariables.Set("payload", payload);

        IDictionary<string, ISKFunction> playfabApiSkills = await this.GetPlayFabSkill();
        SKContext result2 = await this._kernel.RunAsync(contextVariables, playfabApiSkills["CreateSegment"]);

        string formattedContent = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(result2.Result), Formatting.Indented);
        Console.WriteLine("\nCreateSegment playfabApiSkills response: \n{0}", formattedContent);

        return $"Segment created successfully";
    }

    /// <summary>
    /// Minimizing json by removing spaces and new lines.
    /// </summary>
    /// <returns>Minimized json content.</returns>
    /// <exception cref="FileNotFoundException">File not found exception.</exception>
    private Task<string> GetMinifiedOpenApiJson()
    {
        string playfabApiFile = GetPlayFabAPIFilePath();

        var playfabOpenAPIContent = File.ReadAllText(playfabApiFile);
        return Task.FromResult(JsonConvert.SerializeObject(JsonConvert.DeserializeObject(playfabOpenAPIContent), Formatting.None));
    }

    /// <summary>
    /// Get open api json file path.
    /// </summary>
    /// <returns>Open api json file path.</returns>
    /// <exception cref="FileNotFoundException">File not found exception.</exception>
    private static string GetPlayFabAPIFilePath()
    {
        var playfabApiFile = @"../PlayFab/Skills/SegmentSkill/openapi.json";

        if (!File.Exists(playfabApiFile))
        {
            playfabApiFile = @"./Skills/SegmentSkill/openapi.json";

            if (!File.Exists(playfabApiFile))
            {
                throw new FileNotFoundException($"Invalid URI. The specified path '{playfabApiFile}' does not exist.");
            }
        }

        return playfabApiFile;
    }

    /// <summary>
    /// Get current title segments.
    /// </summary>
    /// <returns>List of segments.</returns>
    private async Task<List<Segment>> GetSegments()
    {
        ContextVariables contextVariables = new();
        contextVariables.Set("content_type", "application/json");
        contextVariables.Set("server_url", this._titleApiEndpoint);
        contextVariables.Set("content_type", "application/json");
        contextVariables.Set("payload", "{ \"SegmentIds\": [] }");

        using HttpClient httpClient = new();
        IDictionary<string, ISKFunction> playfabApiSkills = await this.GetPlayFabSkill();
        SKContext result = await this._kernel.RunAsync(contextVariables, playfabApiSkills["GetSegments"]);

        string formattedContent = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(result.Result), Formatting.Indented);
        JObject segmentsDataObject = JObject.Parse(formattedContent);
        string? content = ((Newtonsoft.Json.Linq.JValue)segmentsDataObject.GetValue("content")).Value.ToString();
        JObject segmentsDataObject2 = JObject.Parse(content);
        string segmentsArrayContent = segmentsDataObject2.GetValue("data").SelectTokens("$.Segments").First().ToString();

        return System.Text.Json.JsonSerializer.Deserialize<List<Segment>>(segmentsArrayContent);
    }

    /// <summary>
    /// Generate distinct segment name.
    /// </summary>
    /// <param name="currentSegmentName">Current segment name.</param>
    /// <param name="segments">List of segments.</param>
    /// <returns>New segment name.</returns>
    private string GenerateDistinctSegmentName(string currentSegmentName, List<Segment> segments)
    {
        int increment = 1;
        string segmentName = string.Empty;

        do
        {
            segmentName = currentSegmentName + increment.ToString();
            increment++;
        } while (segments.Select(seg => seg.Name).Contains(segmentName));

        return segmentName;
    }

    private async Task<IDictionary<string, ISKFunction>> GetPlayFabSkill()
    {
        IDictionary<string, ISKFunction> playfabApiSkills;

        var titleSecretKeyProvider = new PlayFabAuthenticationProvider(() =>
        {
            return Task.FromResult(this._titleSecretKey);
        });

        string skillName = "PlayFabApiSkill";
        if (UseLocalFile)
        {
            //todo: remove hardcoding of path
            var playfabApiFile = GetPlayFabAPIFilePath();
            playfabApiSkills = await this._kernel.ImportAIPluginAsync(skillName, playfabApiFile, new OpenApiSkillExecutionParameters(HttpClient, authCallback: titleSecretKeyProvider.AuthenticateRequestAsync));
        }
        else
        {
            var playfabApiRawFileUrl = new Uri(this._swaggerEndpoint);
            playfabApiSkills = await this._kernel.ImportAIPluginAsync("PlayFabApiSkill", playfabApiRawFileUrl, new OpenApiSkillExecutionParameters(HttpClient, authCallback: titleSecretKeyProvider.AuthenticateRequestAsync));
        }

        return playfabApiSkills;
    }
}
