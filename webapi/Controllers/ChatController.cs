// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Plugins.Chat;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Storage;
using CopilotChat.WebApi.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Functions.OpenAPI.Authentication;
using Microsoft.SemanticKernel.Functions.OpenAPI.Extensions;
using Microsoft.SemanticKernel.Functions.OpenAPI.OpenAI;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Plugins.MsGraph;
using Microsoft.SemanticKernel.Plugins.MsGraph.Connectors;
using Microsoft.SemanticKernel.Plugins.MsGraph.Connectors.Client;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// Controller responsible for handling chat messages and responses.
/// </summary>
[ApiController]
public class ChatController : ControllerBase, IDisposable
{
    private readonly ILogger<ChatController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly List<IDisposable> _disposables;
    private readonly ITelemetryService _telemetryService;
    private readonly ServiceOptions _serviceOptions;
    private readonly PlannerOptions _plannerOptions;
    private readonly IDictionary<string, Plugin> _plugins;

    private const string ChatPluginName = nameof(ChatPlugin);
    private const string ChatFunctionName = "Chat";
    private const string ProcessPlanFunctionName = "ProcessPlan";
    private const string GeneratingResponseClientCall = "ReceiveBotResponseStatus";

    public ChatController(
        ILogger<ChatController> logger,
        IHttpClientFactory httpClientFactory,
        ITelemetryService telemetryService,
        IOptions<ServiceOptions> serviceOptions,
        IOptions<PlannerOptions> plannerOptions,
        IDictionary<string, Plugin> plugins)
    {
        this._logger = logger;
        this._httpClientFactory = httpClientFactory;
        this._telemetryService = telemetryService;
        this._disposables = new List<IDisposable>();
        this._serviceOptions = serviceOptions.Value;
        this._plannerOptions = plannerOptions.Value;
        this._plugins = plugins;
    }

    /// <summary>
    /// Invokes the chat function to get a response from the bot.
    /// </summary>
    /// <param name="kernel">Semantic kernel obtained through dependency injection.</param>
    /// <param name="messageRelayHubContext">Message Hub that performs the real time relay service.</param>
    /// <param name="planner">Planner to use to create function sequences.</param>
    /// <param name="askConverter">Converter to use for converting Asks.</param>
    /// <param name="chatSessionRepository">Repository of chat sessions.</param>
    /// <param name="chatParticipantRepository">Repository of chat participants.</param>
    /// <param name="authInfo">Auth info for the current request.</param>
    /// <param name="ask">Prompt along with its parameters.</param>
    /// <param name="chatId">Chat ID.</param>
    /// <returns>Results containing the response from the model.</returns>
    [Route("chats/{chatId:guid}/messages")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> ChatAsync(
        [FromServices] IKernel kernel,
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromServices] CopilotChatPlanner planner,
        [FromServices] ChatSessionRepository chatSessionRepository,
        [FromServices] ChatParticipantRepository chatParticipantRepository,
        [FromServices] IAuthInfo authInfo,
        [FromBody] Ask ask,
        [FromRoute] Guid chatId)
    {
        this._logger.LogDebug("Chat message received.");

        return await this.HandleRequest(ChatFunctionName, kernel, messageRelayHubContext, planner, chatSessionRepository, chatParticipantRepository, authInfo, ask, chatId.ToString());
    }

    /// <summary>
    /// Invokes the chat function to process and/or execute plan.
    /// </summary>
    /// <param name="kernel">Semantic kernel obtained through dependency injection.</param>
    /// <param name="messageRelayHubContext">Message Hub that performs the real time relay service.</param>
    /// <param name="planner">Planner to use to create function sequences.</param>
    /// <param name="askConverter">Converter to use for converting Asks.</param>
    /// <param name="chatSessionRepository">Repository of chat sessions.</param>
    /// <param name="chatParticipantRepository">Repository of chat participants.</param>
    /// <param name="authInfo">Auth info for the current request.</param>
    /// <param name="ask">Prompt along with its parameters.</param>
    /// <param name="chatId">Chat ID.</param>
    /// <returns>Results containing the response from the model.</returns>
    [Route("chats/{chatId:guid}/plan")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> ProcessPlanAsync(
        [FromServices] IKernel kernel,
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromServices] CopilotChatPlanner planner,
        [FromServices] ChatSessionRepository chatSessionRepository,
        [FromServices] ChatParticipantRepository chatParticipantRepository,
        [FromServices] IAuthInfo authInfo,
        [FromBody] ExecutePlanParameters ask,
        [FromRoute] Guid chatId)
    {
        this._logger.LogDebug("plan request received.");

        return await this.HandleRequest(ProcessPlanFunctionName, kernel, messageRelayHubContext, planner, chatSessionRepository, chatParticipantRepository, authInfo, ask, chatId.ToString());
    }

    /// <summary>
    /// Invokes given function of ChatPlugin.
    /// </summary>
    /// <param name="functionName">Name of the ChatPlugin function to invoke.</param>
    /// <param name="kernel">Semantic kernel obtained through dependency injection.</param>
    /// <param name="messageRelayHubContext">Message Hub that performs the real time relay service.</param>
    /// <param name="planner">Planner to use to create function sequences.</param>
    /// <param name="askConverter">Converter to use for converting Asks.</param>
    /// <param name="chatSessionRepository">Repository of chat sessions.</param>
    /// <param name="chatParticipantRepository">Repository of chat participants.</param>
    /// <param name="authInfo">Auth info for the current request.</param>
    /// <param name="ask">Prompt along with its parameters.</param>
    /// <param name="chatId"Chat ID.</>
    /// <returns>Results containing the response from the model.</returns>
    private async Task<IActionResult> HandleRequest(
       string functionName,
       IKernel kernel,
       IHubContext<MessageRelayHub> messageRelayHubContext,
       CopilotChatPlanner planner,
       ChatSessionRepository chatSessionRepository,
       ChatParticipantRepository chatParticipantRepository,
       IAuthInfo authInfo,
       Ask ask,
       string chatId)
    {
        // Put ask's variables in the context we will use.
        var contextVariables = GetContextVariables(ask, authInfo, chatId);

        // Verify that the chat exists and that the user has access to it.
        ChatSession? chat = null;
        if (!(await chatSessionRepository.TryFindByIdAsync(chatId, callback: c => chat = c)))
        {
            return this.NotFound("Failed to find chat session for the chatId specified in variables.");
        }

        if (!(await chatParticipantRepository.IsUserInChatAsync(authInfo.UserId, chatId)))
        {
            return this.Forbid("User does not have access to the chatId specified in variables.");
        }

        // Register plugins that have been enabled
        var openApiPluginAuthHeaders = this.GetPluginAuthHeaders(this.HttpContext.Request.Headers);
        await this.RegisterPlannerFunctionsAsync(planner, openApiPluginAuthHeaders, contextVariables);

        // Register hosted plugins that have been enabled
        await this.RegisterPlannerHostedFunctionsUsedAsync(planner, chat!.EnabledPlugins);

        // Get the function to invoke
        ISKFunction? function = null;
        try
        {
            function = kernel.Functions.GetFunction(ChatPluginName, functionName);
        }
        catch (SKException ex)
        {
            this._logger.LogError("Failed to find {PluginName}/{FunctionName} on server: {Exception}", ChatPluginName, functionName, ex);
            return this.NotFound($"Failed to find {ChatPluginName}/{functionName} on server");
        }

        // Run the function.
        KernelResult? result = null;
        try
        {
            using CancellationTokenSource? cts = this._serviceOptions.TimeoutLimitInS is not null
                // Create a cancellation token source with the timeout if specified
                ? new CancellationTokenSource(TimeSpan.FromSeconds((double)this._serviceOptions.TimeoutLimitInS))
                : null;

            result = await kernel.RunAsync(function!, contextVariables, cts?.Token ?? default);
            this._telemetryService.TrackPluginFunction(ChatPluginName, functionName, true);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException || ex.InnerException is OperationCanceledException)
            {
                // Log the timeout and return a 504 response
                this._logger.LogError("The {FunctionName} operation timed out.", functionName);
                return this.StatusCode(StatusCodes.Status504GatewayTimeout, $"The chat {functionName} timed out.");
            }

            this._telemetryService.TrackPluginFunction(ChatPluginName, functionName, false);
            throw ex;
        }

        AskResult chatAskResult = new()
        {
            Value = result.GetValue<string>() ?? string.Empty,
            Variables = contextVariables.Select(v => new KeyValuePair<string, string>(v.Key, v.Value))
        };

        // Broadcast AskResult to all users
        await messageRelayHubContext.Clients.Group(chatId).SendAsync(GeneratingResponseClientCall, chatId, null);

        return this.Ok(chatAskResult);
    }

    /// <summary>
    /// Parse plugin auth values from request headers.
    /// </summary>
    private Dictionary<string, string> GetPluginAuthHeaders(IHeaderDictionary headers)
    {
        // Create a regex to match the headers
        var regex = new Regex("x-sk-copilot-(.*)-auth", RegexOptions.IgnoreCase);

        // Create a dictionary to store the matched headers and values
        var authHeaders = new Dictionary<string, string>();

        // Loop through the request headers and add the matched ones to the dictionary
        foreach (var header in headers)
        {
            var match = regex.Match(header.Key);
            if (match.Success)
            {
                // Use the first capture group as the key and the header value as the value
                authHeaders.Add(match.Groups[1].Value.ToUpperInvariant(), header.Value!);
            }
        }

        return authHeaders;
    }

    /// <summary>
    /// Register functions with the planner's kernel.
    /// </summary>
    private async Task RegisterPlannerFunctionsAsync(CopilotChatPlanner planner, Dictionary<string, string> authHeaders, ContextVariables variables)
    {
        // Register authenticated functions with the planner's kernel only if the request includes an auth header for the plugin.

        // GitHub
        if (authHeaders.TryGetValue("GITHUB", out string? GithubAuthHeader))
        {
            this._logger.LogInformation("Enabling GitHub plugin.");
            BearerAuthenticationProvider authenticationProvider = new(() => Task.FromResult(GithubAuthHeader));
            await planner.Kernel.ImportOpenApiPluginFunctionsAsync(
                pluginName: "GitHubPlugin",
                filePath: Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Plugins", "OpenApi/GitHubPlugin/openapi.json"),
                new OpenApiFunctionExecutionParameters
                {
                    AuthCallback = authenticationProvider.AuthenticateRequestAsync,
                });
        }

        // Jira
        if (authHeaders.TryGetValue("JIRA", out string? JiraAuthHeader))
        {
            this._logger.LogInformation("Registering Jira plugin");
            var authenticationProvider = new BasicAuthenticationProvider(() => { return Task.FromResult(JiraAuthHeader); });
            var hasServerUrlOverride = variables.TryGetValue("jira-server-url", out string? serverUrlOverride);

            await planner.Kernel.ImportOpenApiPluginFunctionsAsync(
                pluginName: "JiraPlugin",
                filePath: Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Plugins", "OpenApi/JiraPlugin/openapi.json"),
                new OpenApiFunctionExecutionParameters
                {
                    AuthCallback = authenticationProvider.AuthenticateRequestAsync,
                    ServerUrlOverride = hasServerUrlOverride ? new Uri(serverUrlOverride!) : null,
                });
        }

        // Microsoft Graph
        if (authHeaders.TryGetValue("GRAPH", out string? GraphAuthHeader))
        {
            this._logger.LogInformation("Enabling Microsoft Graph plugin(s).");
            BearerAuthenticationProvider authenticationProvider = new(() => Task.FromResult(GraphAuthHeader));
            GraphServiceClient graphServiceClient = this.CreateGraphServiceClient(authenticationProvider.AuthenticateRequestAsync);

            planner.Kernel.ImportFunctions(new TaskListPlugin(new MicrosoftToDoConnector(graphServiceClient)), "todo");
            planner.Kernel.ImportFunctions(new CalendarPlugin(new OutlookCalendarConnector(graphServiceClient)), "calendar");
            planner.Kernel.ImportFunctions(new EmailPlugin(new OutlookMailConnector(graphServiceClient)), "email");
        }

        if (variables.TryGetValue("customPlugins", out string? customPluginsString))
        {
            CustomPlugin[]? customPlugins = JsonSerializer.Deserialize<CustomPlugin[]>(customPluginsString);

            if (customPlugins != null)
            {
                foreach (CustomPlugin plugin in customPlugins)
                {
                    if (authHeaders.TryGetValue(plugin.AuthHeaderTag.ToUpperInvariant(), out string? PluginAuthValue))
                    {
                        // Register the ChatGPT plugin with the planner's kernel.
                        this._logger.LogInformation("Enabling {0} plugin.", plugin.NameForHuman);

                        // TODO: [Issue #44] Support other forms of auth. Currently, we only support user PAT or no auth.
                        var requiresAuth = !plugin.AuthType.Equals("none", StringComparison.OrdinalIgnoreCase);
                        OpenAIAuthenticateRequestAsyncCallback authCallback = (request, _, _) =>
                        {
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", PluginAuthValue);

                            return Task.CompletedTask;
                        };

                        await planner.Kernel.ImportOpenAIPluginFunctionsAsync(
                            $"{plugin.NameForModel}Plugin",
                            PluginUtils.GetPluginManifestUri(plugin.ManifestDomain),
                            new OpenAIFunctionExecutionParameters
                            {
                                HttpClient = this._httpClientFactory.CreateClient("Plugin"),
                                IgnoreNonCompliantErrors = true,
                                AuthCallback = requiresAuth ? authCallback : null
                            });
                    }
                }
            }
            else
            {
                this._logger.LogDebug("Failed to deserialize custom plugin details: {0}", customPluginsString);
            }
        }
    }

    /// <summary>
    /// Create a Microsoft Graph service client.
    /// </summary>
    /// <param name="authenticateRequestAsyncDelegate">The delegate to authenticate the request.</param>
    private GraphServiceClient CreateGraphServiceClient(AuthenticateRequestAsyncDelegate authenticateRequestAsyncDelegate)
    {
        MsGraphClientLoggingHandler graphLoggingHandler = new(this._logger);
        this._disposables.Add(graphLoggingHandler);

        IList<DelegatingHandler> graphMiddlewareHandlers =
            GraphClientFactory.CreateDefaultHandlers(new DelegateAuthenticationProvider(authenticateRequestAsyncDelegate));
        graphMiddlewareHandlers.Add(graphLoggingHandler);

        HttpClient graphHttpClient = GraphClientFactory.Create(graphMiddlewareHandlers);
        this._disposables.Add(graphHttpClient);

        GraphServiceClient graphServiceClient = new(graphHttpClient);
        return graphServiceClient;
    }

    private async Task RegisterPlannerHostedFunctionsUsedAsync(CopilotChatPlanner planner, HashSet<string> enabledPlugins)
    {
        foreach (string enabledPlugin in enabledPlugins)
        {
            if (this._plugins.TryGetValue(enabledPlugin, out Plugin? plugin))
            {
                this._logger.LogDebug("Enabling hosted plugin {0}.", plugin.Name);

                OpenAIAuthenticateRequestAsyncCallback authCallback = (request, _, _) =>
                {
                    request.Headers.Add("X-Functions-Key", plugin.Key);

                    return Task.CompletedTask;
                };

                // Register the ChatGPT plugin with the planner's kernel.
                await planner.Kernel.ImportOpenAIPluginFunctionsAsync(
                    PluginUtils.SanitizePluginName(plugin.Name),
                    PluginUtils.GetPluginManifestUri(plugin.ManifestDomain),
                    new OpenAIFunctionExecutionParameters
                    {
                        HttpClient = this._httpClientFactory.CreateClient("Plugin"),
                        IgnoreNonCompliantErrors = true,
                        AuthCallback = authCallback
                    });
            }
            else
            {
                this._logger.LogWarning("Failed to find plugin {0}.", enabledPlugin);
            }
        }

        return;
    }

    private static ContextVariables GetContextVariables(Ask ask, IAuthInfo authInfo, string chatId)
    {
        const string UserIdKey = "userId";
        const string UserNameKey = "userName";
        const string ChatIdKey = "chatId";

        var contextVariables = new ContextVariables(ask.Input);
        foreach (var variable in ask.Variables)
        {
            contextVariables.Set(variable.Key, variable.Value);
        }

        contextVariables.Set(UserIdKey, authInfo.UserId);
        contextVariables.Set(UserNameKey, authInfo.Name);
        contextVariables.Set(ChatIdKey, chatId);

        return contextVariables;
    }

    /// <summary>
    /// Dispose of the object.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (IDisposable disposable in this._disposables)
            {
                disposable.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
