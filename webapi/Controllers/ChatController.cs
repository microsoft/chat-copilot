// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Skills.ChatSkills;
using CopilotChat.WebApi.Storage;
using CopilotChat.WebApi.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Skills.MsGraph;
using Microsoft.SemanticKernel.Skills.MsGraph.Connectors;
using Microsoft.SemanticKernel.Skills.MsGraph.Connectors.Client;
using Microsoft.SemanticKernel.Skills.OpenAPI.Authentication;
using Microsoft.SemanticKernel.Skills.OpenAPI.Extensions;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// Controller responsible for handling chat messages and responses.
/// </summary>
[ApiController]
public class ChatController : ControllerBase, IDisposable
{
    private readonly ILogger<ChatController> _logger;
    private readonly List<IDisposable> _disposables;
    private readonly ITelemetryService _telemetryService;
    private readonly ServiceOptions _serviceOptions;
    private readonly PlannerOptions _plannerOptions;

    private const string ChatSkillName = "ChatSkill";
    private const string ChatFunctionName = "Chat";
    private const string GeneratingResponseClientCall = "ReceiveBotResponseStatus";

    public ChatController(ILogger<ChatController> logger, ITelemetryService telemetryService, IOptions<ServiceOptions> serviceOptions, IOptions<PlannerOptions> plannerOptions)
    {
        this._logger = logger;
        this._telemetryService = telemetryService;
        this._disposables = new List<IDisposable>();
        this._serviceOptions = serviceOptions.Value;
        this._plannerOptions = plannerOptions.Value;
    }

    /// <summary>
    /// Invokes the chat skill to get a response from the bot.
    /// </summary>
    /// <param name="kernel">Semantic kernel obtained through dependency injection.</param>
    /// <param name="messageRelayHubContext">Message Hub that performs the real time relay service.</param>
    /// <param name="planner">Planner to use to create function sequences.</param>
    /// <param name="askConverter">Converter to use for converting Asks.</param>
    /// <param name="chatSessionRepository">Repository of chat sessions.</param>
    /// <param name="chatParticipantRepository">Repository of chat participants.</param>
    /// <param name="authInfo">Auth info for the current request.</param>
    /// <param name="ask">Prompt along with its parameters.</param>
    /// <returns>Results containing the response from the model.</returns>
    [Route("chat")]
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
        [FromServices] AskConverter askConverter,
        [FromServices] ChatSessionRepository chatSessionRepository,
        [FromServices] ChatParticipantRepository chatParticipantRepository,
        [FromServices] IAuthInfo authInfo,
        [FromBody] Ask ask)
    {
        this._logger.LogDebug("Chat request received.");

        // Verify that the chat exists and that the user has access to it.
        const string ChatIdKey = "chatId";
        var chatIdFromContext = ask.Variables.FirstOrDefault(x => x.Key == ChatIdKey);
        if (chatIdFromContext.Key is ChatIdKey)
        {
            var chatId = chatIdFromContext.Value;
            var chat = await chatSessionRepository.FindByIdAsync(chatId);
            if (chat == null)
            {
                return this.NotFound("Failed to find chat session for the chatId specified in variables.");
            }

            bool isUserInChat = await chatParticipantRepository.IsUserInChatAsync(authInfo.UserId, chatId);
            if (!isUserInChat)
            {
                return this.Forbid("User does not have access to the chatId specified in variables.");
            }
        }
        else
        {
            return this.BadRequest("ChatId not specified.");
        }

        // Put ask's variables in the context we will use.
        var contextVariables = askConverter.GetContextVariables(ask);

        // Register plugins that have been enabled
        var openApiSkillsAuthHeaders = this.GetPluginAuthHeaders(this.HttpContext.Request.Headers);
        await this.RegisterPlannerSkillsAsync(planner, openApiSkillsAuthHeaders, contextVariables);

        // Get the function to invoke
        ISKFunction? function = null;
        try
        {
            function = kernel.Skills.GetFunction(ChatSkillName, ChatFunctionName);
        }
        catch (SKException ex)
        {
            this._logger.LogError("Failed to find {0}/{1} on server: {2}", ChatSkillName, ChatFunctionName, ex);

            return this.NotFound($"Failed to find {ChatSkillName}/{ChatFunctionName} on server");
        }

        // Run the function.
        SKContext? result = null;
        try
        {
            using CancellationTokenSource? cts = this._serviceOptions.TimeoutLimitInS is not null
                // Create a cancellation token source with the timeout if specified
                ? new CancellationTokenSource(TimeSpan.FromSeconds((double)this._serviceOptions.TimeoutLimitInS))
                : null;

            result = await kernel.RunAsync(function!, contextVariables, cts?.Token ?? default);
        }
        finally
        {
            this._telemetryService.TrackSkillFunction(ChatSkillName, ChatFunctionName, (!result?.ErrorOccurred) ?? false);
        }

        if (result.ErrorOccurred)
        {
            if (result.LastException is OperationCanceledException || result.LastException?.InnerException is OperationCanceledException)
            {
                // Log the timeout and return a 504 response
                this._logger.LogError("The chat operation timed out.");
                return this.StatusCode(StatusCodes.Status504GatewayTimeout, "The chat operation timed out.");
            }

            var errorMessage = result.LastException!.Message.IsNullOrEmpty() ? result.LastException!.InnerException?.Message : result.LastException!.Message;
            return this.BadRequest(errorMessage);
        }

        AskResult chatSkillAskResult = new()
        {
            Value = result.Result,
            Variables = result.Variables.Select(
                v => new KeyValuePair<string, string>(v.Key, v.Value))
        };

        // Broadcast AskResult to all users
        if (ask.Variables.Where(v => v.Key == "chatId").Any())
        {
            var chatId = ask.Variables.Where(v => v.Key == "chatId").First().Value;
            await messageRelayHubContext.Clients.Group(chatId).SendAsync(GeneratingResponseClientCall, chatId, null);
        }

        return this.Ok(chatSkillAskResult);
    }

    /// <summary>
    /// Parse plugin auth values from request headers.
    /// </summary>
    private Dictionary<string, string> GetPluginAuthHeaders(IHeaderDictionary headers)
    {
        // Create a regex to match the headers
        var regex = new Regex("x-sk-copilot-(.*)-auth", RegexOptions.IgnoreCase);

        // Create a dictionary to store the matched headers and values
        var openApiSkillsAuthHeaders = new Dictionary<string, string>();

        // Loop through the request headers and add the matched ones to the dictionary
        foreach (var header in headers)
        {
            var match = regex.Match(header.Key);
            if (match.Success)
            {
                // Use the first capture group as the key and the header value as the value
                openApiSkillsAuthHeaders.Add(match.Groups[1].Value.ToUpperInvariant(), header.Value);
            }
        }

        return openApiSkillsAuthHeaders;
    }

    /// <summary>
    /// Register skills with the planner's kernel.
    /// </summary>
    private async Task RegisterPlannerSkillsAsync(CopilotChatPlanner planner, Dictionary<string, string> openApiSkillsAuthHeaders, ContextVariables variables)
    {
        // Register authenticated skills with the planner's kernel only if the request includes an auth header for the skill.

        // Klarna Shopping
        if (openApiSkillsAuthHeaders.TryGetValue("KLARNA", out string? KlarnaAuthHeader))
        {
            this._logger.LogInformation("Registering Klarna plugin");

            // Register the Klarna shopping ChatGPT plugin with the planner's kernel. There is no authentication required for this plugin.
            await planner.Kernel.ImportAIPluginAsync("KlarnaShoppingPlugin", new Uri("https://www.klarna.com/.well-known/ai-plugin.json"), new OpenApiSkillExecutionParameters());
        }

        // GitHub
        if (openApiSkillsAuthHeaders.TryGetValue("GITHUB", out string? GithubAuthHeader))
        {
            this._logger.LogInformation("Enabling GitHub plugin.");
            BearerAuthenticationProvider authenticationProvider = new(() => Task.FromResult(GithubAuthHeader));
            await planner.Kernel.ImportAIPluginAsync(
                skillName: "GitHubPlugin",
                filePath: Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Skills", "OpenApiPlugins/GitHubPlugin/openapi.json"),
                new OpenApiSkillExecutionParameters
                {
                    AuthCallback = authenticationProvider.AuthenticateRequestAsync,
                });
        }

        // Jira
        if (openApiSkillsAuthHeaders.TryGetValue("JIRA", out string? JiraAuthHeader))
        {
            this._logger.LogInformation("Registering Jira plugin");
            var authenticationProvider = new BasicAuthenticationProvider(() => { return Task.FromResult(JiraAuthHeader); });
            var hasServerUrlOverride = variables.TryGetValue("jira-server-url", out string? serverUrlOverride);

            await planner.Kernel.ImportAIPluginAsync(
                skillName: "JiraPlugin",
                filePath: Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Skills", "OpenApiPlugins/JiraPlugin/openapi.json"),
                new OpenApiSkillExecutionParameters
                {
                    AuthCallback = authenticationProvider.AuthenticateRequestAsync,
                    ServerUrlOverride = hasServerUrlOverride ? new Uri(serverUrlOverride!) : null,
                });
        }

        // Microsoft Graph
        if (openApiSkillsAuthHeaders.TryGetValue("GRAPH", out string? GraphAuthHeader))
        {
            this._logger.LogInformation("Enabling Microsoft Graph skill(s).");
            BearerAuthenticationProvider authenticationProvider = new(() => Task.FromResult(GraphAuthHeader));
            GraphServiceClient graphServiceClient = this.CreateGraphServiceClient(authenticationProvider.AuthenticateRequestAsync);

            planner.Kernel.ImportSkill(new TaskListSkill(new MicrosoftToDoConnector(graphServiceClient)), "todo");
            planner.Kernel.ImportSkill(new CalendarSkill(new OutlookCalendarConnector(graphServiceClient)), "calendar");
            planner.Kernel.ImportSkill(new EmailSkill(new OutlookMailConnector(graphServiceClient)), "email");
        }

        if (variables.TryGetValue("customPlugins", out string? customPluginsString))
        {
            CustomPlugin[]? customPlugins = JsonSerializer.Deserialize<CustomPlugin[]>(customPluginsString);

            if (customPlugins != null)
            {
                foreach (CustomPlugin plugin in customPlugins)
                {
                    if (openApiSkillsAuthHeaders.TryGetValue(plugin.AuthHeaderTag.ToUpperInvariant(), out string? PluginAuthValue))
                    {
                        // Register the ChatGPT plugin with the planner's kernel.
                        this._logger.LogInformation("Enabling {0} plugin.", plugin.NameForHuman);

                        UriBuilder uriBuilder = new(plugin.ManifestDomain);
                        // Expected manifest path as defined by OpenAI: https://platform.openai.com/docs/plugins/getting-started/plugin-manifest
                        uriBuilder.Path = "/.well-known/ai-plugin.json";

                        // TODO: [Issue #44] Support other forms of auth. Currently, we only support user PAT or no auth.
                        var requiresAuth = !plugin.AuthType.Equals("none", StringComparison.OrdinalIgnoreCase);
                        BearerAuthenticationProvider authenticationProvider = new(() => Task.FromResult(PluginAuthValue));

                        HttpClient httpClient = new();
                        httpClient.Timeout = TimeSpan.FromSeconds(this._plannerOptions.PluginTimeoutLimitInS);

                        await planner.Kernel.ImportAIPluginAsync(
                            $"{plugin.NameForModel}Plugin",
                            uriBuilder.Uri,
                            new OpenApiSkillExecutionParameters
                            {
                                HttpClient = httpClient,
                                IgnoreNonCompliantErrors = true,
                                AuthCallback = requiresAuth ? authenticationProvider.AuthenticateRequestAsync : null
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
