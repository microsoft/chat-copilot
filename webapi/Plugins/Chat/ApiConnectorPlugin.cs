using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.Plugins.MsGraph.Diagnostics;
using Microsoft.SemanticKernel.Plugins.MsGraph.Models;
using Microsoft.Graph;
using Microsoft.SemanticKernel;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;


//
// Summary:
//     Task list plugin (e.g. Microsoft To-Do)
public sealed class ApiConnectorPlugin
{
    //
    // Summary:
    //     Microsoft.SemanticKernel.Orchestration.ContextVariables parameter names.
    public static class Parameters
    {
        //
        // Summary:
        //     Task reminder as DateTimeOffset.
        public const string ApiUrl = "apiUrl";

        //
        // Summary:
        //     Whether to include completed tasks.
        public const string ApiMethod = "Get";
    }

    private readonly string _bearerToken;

    private readonly ILogger _logger;

    //
    // Summary:
    //     Initializes a new instance of the Microsoft.SemanticKernel.Plugins.MsGraph.TaskListPlugin
    //     class.
    //
    // Parameters:
    //   connector:
    //     Task list connector.
    //
    //   loggerFactory:
    //     The Microsoft.Extensions.Logging.ILoggerFactory to use for logging. If null,
    //     no logging will be performed.
    public ApiConnectorPlugin(string bearerToken, ILoggerFactory? loggerFactory = null)
    {
        if (bearerToken == null)
        {
            throw new ArgumentNullException("Bearer token not found.");
        }

        this._bearerToken = bearerToken;

        ILogger logger;
        if (loggerFactory == null)
        {
            ILogger instance = NullLogger.Instance;
            logger = instance;
        }
        else
        {
            logger = loggerFactory.CreateLogger(typeof(ApiConnectorPlugin));
        }

        this._logger = logger;
    }

    //
    // Summary:
    //     Get tasks from the default task list.
    [SKFunction]
    [Description("Call Graph API endpoint with odata queries and the graph scopes based on user input")]
    public async Task<string> CallGraphApiTasksAsync([Description("Url of the API with OData query to call")] string apiURL, [Description("Comma separated value string with the graph scopes needed to call the graph API")] string graphScopes, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (string.IsNullOrEmpty(apiURL))
        {
            throw new ArgumentNullException("apiURL was not provided");
        }

        //check if scopes are not null
        if (string.IsNullOrEmpty(graphScopes))
        {
            throw new ArgumentNullException("graphScopes was not provided");
        }

        //THIS CODE IS FOR THE POC ONLY AND WILL BE REPLACED BY THE GRAPH SERVICE CLIENT  
        HttpClient client = new HttpClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "https://login.microsoftonline.com/070c2354-e90f-42bb-a620-185a7cbc8f19/oauth2/v2.0/token");

        var keyValues = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                new KeyValuePair<string, string>("client_id", "384525a6-b975-4add-b74b-a001955c9426"),
                new KeyValuePair<string, string>("client_secret", "N~s8Q~DcTWLcbDzA35v9Sn2S3SE5IM6fcVfg4aLg"),
                new KeyValuePair<string, string>("assertion", this._bearerToken),
                new KeyValuePair<string, string>("scope", graphScopes),
                new KeyValuePair<string, string>("requested_token_use", "on_behalf_of")
            };

        request.Content = new FormUrlEncodedContent(keyValues);

        var response = await client.SendAsync(request);
        var responseContent = string.Empty;

        if (response.IsSuccessStatusCode)
        {
            responseContent = await response.Content.ReadAsStringAsync();

        }
        else
        {
            throw new Exception($"Failed to get token: {response.StatusCode}");
        }

        string accessToken = string.Empty;
        using (JsonDocument doc = JsonDocument.Parse(responseContent))
        {
            JsonElement root = doc.RootElement;
            accessToken = root.GetProperty("access_token").GetString();
          
        }

        
        //use graphserviceclient to get data from the api Url 
        // var result = new QueryOption("$filter", "startswith(title, 'Task')");
        // var response = await _graphServiceClient.HttpProvider.SendAsync(new HttpRequestMessage(HttpMethod.Get, apiURL), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        // if (response.StatusCode != HttpStatusCode.OK)
        // {
        //     throw new InvalidOperationException($"Failed to get tasks from the API. Status code: {response.StatusCode}");
        // }

        // TaskManagementTaskList taskManagementTaskList = await _connector.GetDefaultTaskListAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        // if (taskManagementTaskList == null)
        // {
        //     throw new InvalidOperationException("No default task list found.");
        // }

        // return JsonSerializer.Serialize(await _connector.GetTasksAsync(taskManagementTaskList.Id, result, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
        //return a fixed string
        return "This is a fixed string";
    }
}