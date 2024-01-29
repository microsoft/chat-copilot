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
  [Description("Call Graph API endpoint with odata queries based on user input")]
  public async Task<string> CallGraphApiTasksAsync([Description("Url of the API with OData query to call")] string apiURL, CancellationToken cancellationToken = default(CancellationToken))
  {
    if (string.IsNullOrEmpty(apiURL))
    {
      throw new ArgumentNullException("apiURL was not provided");
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