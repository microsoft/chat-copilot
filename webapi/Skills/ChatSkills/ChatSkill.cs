// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Skills.Utils;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.TemplateEngine.Prompt;
using Microsoft.SemanticMemory;
using ChatCompletionContextMessages = Microsoft.SemanticKernel.AI.ChatCompletion.ChatHistory;

namespace CopilotChat.WebApi.Skills.ChatSkills;

/// <summary>
/// ChatSkill offers a more coherent chat experience by using memories
/// to extract conversation history and user intentions.
/// </summary>
public class ChatSkill
{
    /// <summary>
    /// A kernel instance to create a completion function since each invocation
    /// of the <see cref="ChatAsync"/> function will generate a new prompt dynamically.
    /// </summary>
    private readonly IKernel _kernel;

    /// <summary>
    /// Client for the semantic-memory service.
    /// </summary>
    private readonly ISemanticMemoryClient _memoryClient;

    /// <summary>
    /// A logger instance to log events.
    /// </summary>
    private ILogger _logger;

    /// <summary>
    /// A repository to save and retrieve chat messages.
    /// </summary>
    private readonly ChatMessageRepository _chatMessageRepository;

    /// <summary>
    /// A repository to save and retrieve chat sessions.
    /// </summary>
    private readonly ChatSessionRepository _chatSessionRepository;

    /// <summary>
    /// A SignalR hub context to broadcast updates of the execution.
    /// </summary>
    private readonly IHubContext<MessageRelayHub> _messageRelayHubContext;

    /// <summary>
    /// Settings containing prompt texts.
    /// </summary>
    private readonly PromptsOptions _promptOptions;

    /// <summary>
    /// A semantic memory retriever instance to query semantic memories.
    /// </summary>
    private readonly SemanticMemoryRetriever _semanticMemoryRetriever;

    /// <summary>
    /// A skill instance to acquire external information.
    /// </summary>
    private readonly ExternalInformationSkill _externalInformationSkill;

    /// <summary>
    /// Azure content safety moderator.
    /// </summary>
    private readonly AzureContentSafety? _contentSafety = null;

    /// <summary>
    /// Create a new instance of <see cref="ChatSkill"/>.
    /// </summary>
    public ChatSkill(
        IKernel kernel,
        ISemanticMemoryClient memoryClient,
        ChatMessageRepository chatMessageRepository,
        ChatSessionRepository chatSessionRepository,
        IHubContext<MessageRelayHub> messageRelayHubContext,
        IOptions<PromptsOptions> promptOptions,
        IOptions<DocumentMemoryOptions> documentImportOptions,
        CopilotChatPlanner planner,
        ILogger logger,
        AzureContentSafety? contentSafety = null)
    {
        this._logger = logger;
        this._kernel = kernel;
        this._memoryClient = memoryClient;
        this._chatMessageRepository = chatMessageRepository;
        this._chatSessionRepository = chatSessionRepository;
        this._messageRelayHubContext = messageRelayHubContext;
        // Clone the prompt options to avoid modifying the original prompt options.
        this._promptOptions = promptOptions.Value.Copy();

        this._semanticMemoryRetriever = new SemanticMemoryRetriever(
            promptOptions,
            chatSessionRepository,
            memoryClient,
            logger);

        this._externalInformationSkill = new ExternalInformationSkill(
            promptOptions,
            planner,
            logger);
        this._contentSafety = contentSafety;
    }

    /// <summary>
    /// Extract user intent from the conversation history.
    /// </summary>
    /// <param name="context">The SKContext.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [SKFunction, Description("Extract user intent")]
    [SKParameter("chatId", "Chat ID to extract history from")]
    [SKParameter("audience", "The audience the chat bot is interacting with.")]
    public async Task<string> ExtractUserIntentAsync(SKContext context, CancellationToken cancellationToken = default)
    {
        var tokenLimit = this._promptOptions.CompletionTokenLimit;
        var historyTokenBudget =
            tokenLimit -
            this._promptOptions.ResponseTokenLimit -
            TokenUtils.TokenCount(string.Join("\n", new string[]
                {
                    this._promptOptions.SystemDescription,
                    this._promptOptions.SystemIntent,
                    this._promptOptions.SystemIntentContinuation
                })
            );

        // Clone the context to avoid modifying the original context variables.
        var intentExtractionContext = context.Clone();
        intentExtractionContext.Variables.Set("tokenLimit", historyTokenBudget.ToString(new NumberFormatInfo()));
        intentExtractionContext.Variables.Set("knowledgeCutoff", this._promptOptions.KnowledgeCutoffDate);

        var completionFunction = this._kernel.CreateSemanticFunction(
            this._promptOptions.SystemIntentExtraction,
            skillName: nameof(ChatSkill),
            description: "Complete the prompt.");

        var result = await completionFunction.InvokeAsync(
            intentExtractionContext,
            settings: this.CreateIntentCompletionSettings(),
            cancellationToken
        );

        // Get token usage from ChatCompletion result and add to context
        TokenUtils.GetFunctionTokenUsage(result, context, this._logger, "SystemIntentExtraction");

        return $"User intent: {result}";
    }

    /// <summary>
    /// Extract the list of participants from the conversation history.
    /// Note that only those who have spoken will be included.
    /// </summary>
    /// <param name="context">The SKContext.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [SKFunction, Description("Extract audience list")]
    [SKParameter("chatId", "Chat ID to extract history from")]
    public async Task<string> ExtractAudienceAsync(SKContext context, CancellationToken cancellationToken = default)
    {
        var tokenLimit = this._promptOptions.CompletionTokenLimit;
        var historyTokenBudget =
            tokenLimit -
            this._promptOptions.ResponseTokenLimit -
            TokenUtils.TokenCount(string.Join("\n", new string[]
                {
                    this._promptOptions.SystemAudience,
                    this._promptOptions.SystemAudienceContinuation,
                })
            );

        // Clone the context to avoid modifying the original context variables.
        var audienceExtractionContext = context.Clone();
        audienceExtractionContext.Variables.Set("tokenLimit", historyTokenBudget.ToString(new NumberFormatInfo()));

        var completionFunction = this._kernel.CreateSemanticFunction(
            this._promptOptions.SystemAudienceExtraction,
            skillName: nameof(ChatSkill),
            description: "Complete the prompt.");

        var result = await completionFunction.InvokeAsync(
            audienceExtractionContext,
            settings: this.CreateIntentCompletionSettings(),
            cancellationToken
        );

        // Get token usage from ChatCompletion result and add to context
        TokenUtils.GetFunctionTokenUsage(result, context, this._logger, "SystemAudienceExtraction");

        return $"List of participants: {result}";
    }

    /// <summary>
    /// Method that wraps GetAllowedChatHistoryAsync to get allotted history messages as one string.
    /// GetAllowedChatHistoryAsync optionally updates a ChatHistory object with the allotted messages,
    /// but the ChatHistory type is not supported when calling from a rendered prompt, so this wrapper bypasses the chatHistory parameter.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    [SKFunction, Description("Extract chat history")]
    public Task<string> ExtractChatHistory(
        [Description("Chat ID to extract history from")] string chatId,
        [Description("Maximum number of tokens")] int tokenLimit,
        CancellationToken cancellationToken = default)
    {
        return this.GetAllowedChatHistoryAsync(chatId, tokenLimit, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Extract chat history within token limit as a formatted string and optionally update the ChatCompletionContextMessages object with the allotted messages
    /// </summary>
    /// <param name="chatId">Chat ID to extract history from.</param>
    /// <param name="tokenLimit">Maximum number of tokens.</param>
    /// <param name="chatHistory">Optional ChatCompletionContextMessages object tracking allotted messages.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Chat history as a string.</returns>
    private async Task<string> GetAllowedChatHistoryAsync(
        string chatId,
        int tokenLimit,
        ChatCompletionContextMessages? chatHistory = null,
        CancellationToken cancellationToken = default)
    {
        var messages = await this._chatMessageRepository.FindByChatIdAsync(chatId);
        var sortedMessages = messages.OrderByDescending(m => m.Timestamp);

        ChatCompletionContextMessages allottedChatHistory = new();
        var remainingToken = tokenLimit;
        string historyText = string.Empty;

        foreach (var chatMessage in sortedMessages)
        {
            var formattedMessage = chatMessage.ToFormattedString();

            if (chatMessage.Type == ChatMessage.ChatMessageType.Document)
            {
                continue;
            }

            // Plan object is not meaningful content in generating bot response, so shorten to intent only to save on tokens
            if (chatMessage.Type == ChatMessage.ChatMessageType.Plan)
            {
                formattedMessage = "Bot proposed plan";

                // Try to extract the user intent for more context
                string pattern = @"User intent: (.*)(?=\.""})";
                Match match = Regex.Match(chatMessage.Content, pattern);
                if (match.Success)
                {
                    string userIntent = match.Groups[1].Value.Trim();
                    formattedMessage = $"Bot proposed plan to help fulfill goal: {userIntent}.";
                }

                formattedMessage = $"[{chatMessage.Timestamp.ToString("G", CultureInfo.CurrentCulture)}] {formattedMessage}";
            }

            var promptRole = chatMessage.AuthorRole == ChatMessage.AuthorRoles.Bot ? AuthorRole.System : AuthorRole.User;
            var tokenCount = chatHistory is not null ? TokenUtils.GetContextMessageTokenCount(promptRole, formattedMessage) : TokenUtils.TokenCount(formattedMessage);

            if (remainingToken - tokenCount >= 0)
            {
                historyText = $"{formattedMessage}\n{historyText}";
                if (chatMessage.AuthorRole == ChatMessage.AuthorRoles.Bot)
                {
                    // Message doesn't have to be formatted for bot. This helps with asserting a natural language response from the LLM (no date or author preamble).
                    var botMessage = chatMessage.Type == ChatMessage.ChatMessageType.Plan ? formattedMessage : chatMessage.Content;
                    allottedChatHistory.AddAssistantMessage(botMessage.Trim());
                }
                else
                {
                    // Omit user name if Auth is disabled.
                    var userMessage = PassThroughAuthenticationHandler.IsDefaultUser(chatMessage.UserId)
                            ? $"[{chatMessage.Timestamp.ToString("G", CultureInfo.CurrentCulture)}] {chatMessage.Content}"
                            : formattedMessage;
                    allottedChatHistory.AddUserMessage(userMessage.Trim());
                }

                remainingToken -= tokenCount;
            }
            else
            {
                break;
            }
        }

        allottedChatHistory.Reverse();
        chatHistory?.AddRange(allottedChatHistory);

        return $"Chat history:\n{historyText.Trim()}";
    }

    /// <summary>
    /// This is the entry point for getting a chat response. It manages the token limit, saves
    /// messages to memory, and fill in the necessary context variables for completing the
    /// prompt that will be rendered by the template engine.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    [SKFunction, Description("Get chat response")]
    public async Task<SKContext> ChatAsync(
        [Description("The new message")] string message,
        [Description("Unique and persistent identifier for the user")] string userId,
        [Description("Name of the user")] string userName,
        [Description("Unique and persistent identifier for the chat")] string chatId,
        [Description("Type of the message")] string messageType,
        SKContext context,
        CancellationToken cancellationToken = default)
    {
        // Set the system description in the prompt options
        await this.SetSystemDescriptionAsync(chatId, cancellationToken);

        // Save this new message to memory such that subsequent chat responses can use it
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Saving user message to chat history", cancellationToken);
        var newUserMessage = await this.SaveNewMessageAsync(message, userId, userName, chatId, messageType, cancellationToken);

        // Clone the context to avoid modifying the original context variables.
        var chatContext = context.Clone();
        chatContext.Variables.Set("knowledgeCutoff", this._promptOptions.KnowledgeCutoffDate);

        ChatMessage chatMessage = await this.GetChatResponseAsync(chatId, userId, chatContext, newUserMessage, cancellationToken);
        context.Variables.Update(chatMessage.Content);

        if (chatMessage.TokenUsage != null)
        {
            context.Variables.Set("tokenUsage", JsonSerializer.Serialize(chatMessage.TokenUsage));
        }
        else
        {
            this._logger.LogWarning("ChatSkill.ChatAsync token usage unknown. Ensure token management has been implemented correctly.");
        }

        return context;
    }

    /// <summary>
    /// This is the entry point for handling a plan, whether the user approves, cancels, or re-runs it.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    [SKFunction, Description("Process a plan")]
    public async Task<SKContext> ProcessPlanAsync(
        [Description("The new message")] string message,
        [Description("Unique and persistent identifier for the user")] string userId,
        [Description("Name of the user")] string userName,
        [Description("Unique and persistent identifier for the chat")] string chatId,
        [Description("Proposed plan object"), DefaultValue(null), SKName("proposedPlan")] string? planJson,
        SKContext context,
        CancellationToken cancellationToken = default)
    {
        // Ensure that plan exists in ask's context variables.
        // If a plan was returned at this point, that means it is a
        //      1. Proposed plan that was approved or cancelled, or
        //      2. Saved plan being re-run.
        ProposedPlan? deserializedPlan;
        if (string.IsNullOrWhiteSpace(planJson) || (deserializedPlan = JsonSerializer.Deserialize<ProposedPlan>(planJson)) == null)
        {
            throw new ArgumentException("Plan does not exist in request context. Ensure that a valid plan is saved in the ask's variables");
        }

        // Ensure plan is actionable by supported planners.
        if (!(deserializedPlan.Type == PlanType.Action || deserializedPlan.Type == PlanType.Sequential))
        {
            throw new ArgumentException($"Plan of type {deserializedPlan.Type} cannot be executed. Only Action or Sequential plans can be re-run at this time.");
        }

        // Set the system description in the prompt options.
        await this.SetSystemDescriptionAsync(chatId, cancellationToken);

        // Clone the context to avoid modifying the original context variables.
        var chatContext = context.Clone();
        chatContext.Variables.Set("knowledgeCutoff", this._promptOptions.KnowledgeCutoffDate);

        // Save this new message to memory such that subsequent chat responses can use it
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Saving user message to chat history", cancellationToken);
        var newUserMessage = await this.SaveNewMessageAsync(message, userId, userName, chatId, ChatMessage.ChatMessageType.Message.ToString(), cancellationToken);

        // If GeneratedPlanMessageId exists on plan object, update that message with new plan state.
        // This signals that this plan was freshly proposed by the model and already saved as a bot response message in chat history.
        if (!string.IsNullOrEmpty(deserializedPlan.GeneratedPlanMessageId))
        {
            await this.UpdateChatMessageContentAsync(planJson, deserializedPlan.GeneratedPlanMessageId, chatId, cancellationToken);
        }

        // If plan was derived from a previous plan, create a new bot message with the plan content and save to chat history.
        if (deserializedPlan.State == PlanState.Derived)
        {
            await this.SaveNewResponseAsync(
               planJson,
               deserializedPlan.Plan.Description,
               chatId,
               userId,
               cancellationToken
           );
        }

        ChatMessage chatMessage;
        if (deserializedPlan.State == PlanState.Rejected)
        {
            // Use a hardcoded response if user cancelled plan
            await this.UpdateBotResponseStatusOnClientAsync(chatId, "Cancelling plan", cancellationToken);
            chatMessage = await this.SaveNewResponseAsync(
                "I am sorry the plan did not meet your goals.",
                string.Empty,
                chatId,
                userId,
                cancellationToken,
                TokenUtils.EmptyTokenUsages()
            );
        }
        else if (deserializedPlan.State == PlanState.Approved || deserializedPlan.State == PlanState.Derived)
        {
            // Render system instruction components and create the meta-prompt template
            var systemInstructions = await AsyncUtils.SafeInvokeAsync(
                () => this.RenderSystemInstructions(chatId, chatContext, cancellationToken), nameof(RenderSystemInstructions));
            var chatCompletion = this._kernel.GetService<IChatCompletion>();
            var promptTemplate = chatCompletion.CreateNewChat(systemInstructions);
            string chatHistoryString = "";

            // Add original user input that prompted plan template
            promptTemplate.AddUserMessage(deserializedPlan.OriginalUserInput);
            chatHistoryString += "\n" + PromptUtils.FormatChatHistoryMessage(ChatMessage.AuthorRoles.User, deserializedPlan.OriginalUserInput);

            // Add bot message proposal as prompt context message
            chatContext.Variables.Set("planFunctions", this._externalInformationSkill.FormattedFunctionsString(deserializedPlan.Plan));
            var promptRenderer = new PromptTemplateEngine();
            var proposedPlanBotMessage = await promptRenderer.RenderAsync(
               this._promptOptions.ProposedPlanBotMessage,
                chatContext,
                cancellationToken);
            promptTemplate.AddAssistantMessage(proposedPlanBotMessage);
            chatHistoryString += "\n" + PromptUtils.FormatChatHistoryMessage(ChatMessage.AuthorRoles.Bot, proposedPlanBotMessage);

            // Add user approval message as prompt context message
            promptTemplate.AddUserMessage("Yes, proceed");
            chatHistoryString += "\n" + PromptUtils.FormatChatHistoryMessage(ChatMessage.AuthorRoles.User, "Yes, proceed");

            // Add user intent behind plan
            // TODO: [Issue #51] Consider regenerating user intent if plan was modified
            promptTemplate.AddSystemMessage(deserializedPlan.UserIntent);

            // Render system supplement to help guide model in using data.
            var promptSupplement = await promptRenderer.RenderAsync(
                this._promptOptions.PlanResultsDescription,
                chatContext,
                cancellationToken);

            // Calculate remaining token budget and execute plan
            await this.UpdateBotResponseStatusOnClientAsync(chatId, "Executing plan", cancellationToken);
            var remainingTokenBudget = this.GetChatContextTokenLimit(promptTemplate) - TokenUtils.GetContextMessageTokenCount(AuthorRole.System, promptSupplement);

            try
            {
                var planResult = await this.AcquireExternalInformationAsync(chatContext, deserializedPlan.UserIntent, remainingTokenBudget, cancellationToken, deserializedPlan.Plan);
                promptTemplate.AddSystemMessage(planResult);

                // Calculate token usage of prompt template
                chatContext.Variables.Set(TokenUtils.GetFunctionKey(this._logger, "SystemMetaPrompt")!, TokenUtils.GetContextMessagesTokenCount(promptTemplate).ToString(CultureInfo.CurrentCulture));

                // TODO: [Issue #150, sk#2106] Accommodate different planner contexts once core team finishes work to return prompt and token usage.
                var plannerDetails = new SemanticDependency<PlanExecutionMetadata>(planResult, null, deserializedPlan.Type.ToString());

                // Get bot response and stream to client
                var promptView = new BotResponsePrompt(systemInstructions, "", deserializedPlan.UserIntent, "", plannerDetails, chatHistoryString, promptTemplate);
                chatMessage = await this.HandleBotResponseAsync(chatId, userId, chatContext, promptView, cancellationToken);

                if (chatMessage.TokenUsage != null)
                {
                    context.Variables.Set("tokenUsage", JsonSerializer.Serialize(chatMessage.TokenUsage));
                }
                else
                {
                    this._logger.LogWarning("ChatSkill.ProcessPlan token usage unknown. Ensure token management has been implemented correctly.");
                }
            }
            catch (Exception ex)
            {
                // Use a hardcoded response if plan failed.
                // TODO: [Issue #150, sk#2106] Check planner token usage, if any, on failure
                chatMessage = await this.SaveNewResponseAsync(
                    $"Oops, I encountered an issue processing your plan. Please review the plan logic and access permissions required for the plan, then try running it again from the Plans tab.\n\nError details: {ex.Message}",
                    string.Empty,
                    chatId,
                    userId,
                    cancellationToken,
                    TokenUtils.EmptyTokenUsages()
                );

                throw new SKException("Failed to process plan.", ex);
            }

            context.Variables.Update(chatMessage.Content);
        }
        else
        {
            throw new ArgumentException($"Plan inactionable in current state: {deserializedPlan.State}");
        }

        return context;
    }

    #region Private

    /// <summary>
    /// Generate the necessary chat context to create a prompt then invoke the model to get a response.
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="chatContext">The SKContext.</param>
    /// <param name="userMessage">ChatMessage object representing new user message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created chat message containing the model-generated response.</returns>
    private async Task<ChatMessage> GetChatResponseAsync(string chatId, string userId, SKContext chatContext, ChatMessage userMessage, CancellationToken cancellationToken)
    {
        // Render system instruction components and create the meta-prompt template
        var systemInstructions = await AsyncUtils.SafeInvokeAsync(
            () => this.RenderSystemInstructions(chatId, chatContext, cancellationToken), nameof(RenderSystemInstructions));
        var chatCompletion = this._kernel.GetService<IChatCompletion>();
        var promptTemplate = chatCompletion.CreateNewChat(systemInstructions);

        // Bypass audience extraction if Auth is disabled
        var audience = string.Empty;
        if (!PassThroughAuthenticationHandler.IsDefaultUser(userId))
        {
            // Get the audience
            await this.UpdateBotResponseStatusOnClientAsync(chatId, "Extracting audience", cancellationToken);
            audience = await AsyncUtils.SafeInvokeAsync(
                () => this.GetAudienceAsync(chatContext, cancellationToken), nameof(GetAudienceAsync));
            promptTemplate.AddSystemMessage(audience);
        }

        // Extract user intent from the conversation history.
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Extracting user intent", cancellationToken);
        var userIntent = await AsyncUtils.SafeInvokeAsync(
            () => this.GetUserIntentAsync(chatContext, cancellationToken), nameof(GetUserIntentAsync));
        promptTemplate.AddSystemMessage(userIntent);

        // Calculate the remaining token budget.
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Calculating remaining token budget", cancellationToken);
        var remainingTokenBudget = this.GetChatContextTokenLimit(promptTemplate, userMessage.ToFormattedString());

        // Acquire external information from planner
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Acquiring external information from planner", cancellationToken);
        var externalInformationTokenLimit = (int)(remainingTokenBudget * this._promptOptions.ExternalInformationContextWeight);
        var planResult = await AsyncUtils.SafeInvokeAsync(
            () => this.AcquireExternalInformationAsync(chatContext, userIntent, externalInformationTokenLimit, cancellationToken: cancellationToken), nameof(AcquireExternalInformationAsync));

        // Extract additional details about stepwise planner execution in chat context
        var plannerDetails = new SemanticDependency<PlanExecutionMetadata>(
                this._externalInformationSkill.StepwiseThoughtProcess?.RawResult ?? planResult,
                this._externalInformationSkill.StepwiseThoughtProcess
            );

        // If plan is suggested, send back to user for approval before running
        var proposedPlan = this._externalInformationSkill.ProposedPlan;
        if (proposedPlan != null)
        {
            // Save a new response to the chat history with the proposed plan content
            return await this.SaveNewResponseAsync(
                JsonSerializer.Serialize<ProposedPlan>(proposedPlan),
                proposedPlan.Plan.Description,
                chatId,
                userId,
                cancellationToken,
                // TODO: [Issue #2106] Accommodate plan token usage differently
                this.GetTokenUsages(chatContext)
            );
        }

        // If plan result is to be used as bot response, save the Stepwise result as a new response to the chat history and return.
        if (this._externalInformationSkill.UseStepwiseResultAsBotResponse(planResult))
        {
            var promptDetails = new BotResponsePrompt("", "", userIntent, "", plannerDetails, "", new ChatHistory());
            return await this.HandleBotResponseAsync(chatId, userId, chatContext, promptDetails, cancellationToken, null, this._externalInformationSkill.StepwiseThoughtProcess!.RawResult);
        }

        // Query relevant semantic and document memories
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Extracting semantic and document memories", cancellationToken);
        var chatMemoriesTokenLimit = (int)(remainingTokenBudget * this._promptOptions.MemoriesResponseContextWeight);
        (var memoryText, var citationMap) = await this._semanticMemoryRetriever.QueryMemoriesAsync(userIntent, chatId, chatMemoriesTokenLimit);

        if (!string.IsNullOrWhiteSpace(memoryText))
        {
            promptTemplate.AddSystemMessage(memoryText);
        }

        // Fill in the chat history with remaining token budget.
        string chatHistory = string.Empty;
        var chatHistoryTokenBudget = remainingTokenBudget - TokenUtils.GetContextMessageTokenCount(AuthorRole.System, memoryText) - TokenUtils.GetContextMessageTokenCount(AuthorRole.System, planResult);

        // Append previous messages
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Extracting chat history", cancellationToken);
        chatHistory = await this.GetAllowedChatHistoryAsync(chatId, chatHistoryTokenBudget, promptTemplate, cancellationToken);

        // Append the plan result last, if exists, to imply precedence.
        if (!string.IsNullOrWhiteSpace(planResult))
        {
            promptTemplate.AddSystemMessage(planResult);
        }

        // Calculate token usage of prompt template
        chatContext.Variables.Set(TokenUtils.GetFunctionKey(this._logger, "SystemMetaPrompt")!, TokenUtils.GetContextMessagesTokenCount(promptTemplate).ToString(CultureInfo.CurrentCulture));

        // Stream the response to the client
        var promptView = new BotResponsePrompt(systemInstructions, audience, userIntent, memoryText, plannerDetails, chatHistory, promptTemplate);
        return await this.HandleBotResponseAsync(chatId, userId, chatContext, promptView, cancellationToken, citationMap.Values.AsEnumerable());
    }

    /// <summary>
    /// Helper function to render system instruction components.
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="context">The SKContext.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<string> RenderSystemInstructions(string chatId, SKContext context, CancellationToken cancellationToken)
    {
        // Render system instruction components
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Initializing prompt", cancellationToken);
        var promptRenderer = new PromptTemplateEngine();
        return await promptRenderer.RenderAsync(
            this._promptOptions.SystemPersona,
            context,
            cancellationToken);
    }

    /// <summary>
    /// Helper function to handle final steps of bot response generation, including streaming to client, generating semantic text memory, calculating final token usages, and saving to chat history.
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="chatContext">Chat context.</param>
    /// <param name="promptView">The prompt view.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<ChatMessage> HandleBotResponseAsync(
        string chatId,
        string userId,
        SKContext chatContext,
        BotResponsePrompt promptView,
        CancellationToken cancellationToken,
        IEnumerable<CitationSource>? citations = null,
        string? responseContent = null)
    {
        ChatMessage chatMessage;
        if (responseContent.IsNullOrEmpty())
        {
            // Get bot response and stream to client
            await this.UpdateBotResponseStatusOnClientAsync(chatId, "Generating bot response", cancellationToken);
            chatMessage = await AsyncUtils.SafeInvokeAsync(
                () => this.StreamResponseToClientAsync(chatId, userId, promptView, cancellationToken, citations), nameof(StreamResponseToClientAsync));
        }
        else
        {
            chatMessage = await this.CreateBotMessageOnClient(
                chatId,
                userId,
                JsonSerializer.Serialize(promptView),
                responseContent!,
                cancellationToken,
                citations
            );
        }

        // Save the message into chat history
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Saving message to chat history", cancellationToken);
        await this._chatMessageRepository.UpsertAsync(chatMessage!);

        // Extract semantic chat memory
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Generating semantic chat memory", cancellationToken);
        await AsyncUtils.SafeInvokeAsync(
            () => SemanticChatMemoryExtractor.ExtractSemanticChatMemoryAsync(
                chatId,
                this._memoryClient,
                this._kernel,
                chatContext,
                this._promptOptions,
                this._logger,
                cancellationToken), nameof(SemanticChatMemoryExtractor.ExtractSemanticChatMemoryAsync));

        // Calculate total token usage for dependency functions and prompt template
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Calculating token usage", cancellationToken);
        chatMessage!.TokenUsage = this.GetTokenUsages(chatContext, chatMessage.Content);

        // Update the message on client and in chat history with final completion token usage
        await this.UpdateMessageOnClient(chatMessage, cancellationToken);
        await this._chatMessageRepository.UpsertAsync(chatMessage);

        return chatMessage;
    }

    /// <summary>
    /// Helper function that creates the correct context variables to
    /// extract the audience from a conversation history.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<string> GetAudienceAsync(SKContext context, CancellationToken cancellationToken)
    {
        SKContext audienceContext = context.Clone();
        var audience = await this.ExtractAudienceAsync(audienceContext, cancellationToken);

        // Copy token usage into original chat context
        var functionKey = TokenUtils.GetFunctionKey(this._logger, "SystemAudienceExtraction")!;
        if (audienceContext.Variables.TryGetValue(functionKey, out string? tokenUsage))
        {
            context.Variables.Set(functionKey, tokenUsage);
        }

        return audience;
    }

    /// <summary>
    /// Helper function that creates the correct context variables to
    /// extract the user intent from the conversation history.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<string> GetUserIntentAsync(SKContext context, CancellationToken cancellationToken)
    {
        SKContext intentContext = context.Clone();
        string userIntent = await this.ExtractUserIntentAsync(intentContext, cancellationToken);

        // Copy token usage into original chat context
        var functionKey = TokenUtils.GetFunctionKey(this._logger, "SystemIntentExtraction")!;
        if (intentContext.Variables.TryGetValue(functionKey!, out string? tokenUsage))
        {
            context.Variables.Set(functionKey!, tokenUsage);
        }

        return userIntent;
    }

    /// <summary>
    /// Helper function that creates the correct context variables to acquire external information.
    /// </summary>
    /// <returns>The plan.</returns>
    /// <param name="context">The SKContext.</param>
    /// <param name="userIntent">The user intent.</param>
    /// <param name="tokenLimit">Maximum number of tokens.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<string> AcquireExternalInformationAsync(SKContext context, string userIntent, int tokenLimit, CancellationToken cancellationToken, Plan? plan = null)
    {
        SKContext planContext = context.Clone();
        planContext.Variables.Set("tokenLimit", tokenLimit.ToString(new NumberFormatInfo()));
        return plan is not null
            ? await this._externalInformationSkill.ExecutePlanAsync(planContext, plan, cancellationToken)
            : await this._externalInformationSkill.InvokePlannerAsync(userIntent, planContext, cancellationToken);
    }

    /// <summary>
    /// Save a new message to the chat history.
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="userId">The user ID</param>
    /// <param name="userName"></param>
    /// <param name="chatId">The chat ID</param>
    /// <param name="type">Type of the message</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<ChatMessage> SaveNewMessageAsync(string message, string userId, string userName, string chatId, string type, CancellationToken cancellationToken)
    {
        // Make sure the chat exists.
        if (!await this._chatSessionRepository.TryFindByIdAsync(chatId))
        {
            throw new ArgumentException("Chat session does not exist.");
        }

        var chatMessage = new ChatMessage(
            userId,
            userName,
            chatId,
            message,
            string.Empty,
            null,
            ChatMessage.AuthorRoles.User,
            // Default to a standard message if the `type` is not recognized
            Enum.TryParse(type, out ChatMessage.ChatMessageType typeAsEnum) && Enum.IsDefined(typeof(ChatMessage.ChatMessageType), typeAsEnum)
                ? typeAsEnum
                : ChatMessage.ChatMessageType.Message);

        await this._chatMessageRepository.CreateAsync(chatMessage);
        return chatMessage;
    }

    /// <summary>
    /// Save a new response to the chat history.
    /// </summary>
    /// <param name="response">Response from the chat.</param>
    /// <param name="prompt">Prompt used to generate the response.</param>
    /// <param name="chatId">The chat ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="tokenUsage">Total token usage of response completion</param>
    /// <param name="citations">Citations for the message</param>
    /// <returns>The created chat message.</returns>
    private async Task<ChatMessage> SaveNewResponseAsync(
        string response,
        string prompt,
        string chatId,
        string userId,
        CancellationToken cancellationToken,
        Dictionary<string, int>? tokenUsage = null,
        IEnumerable<CitationSource>? citations = null
       )
    {
        // Make sure the chat exists.
        if (!await this._chatSessionRepository.TryFindByIdAsync(chatId))
        {
            throw new ArgumentException("Chat session does not exist.");
        }

        // Save message to chat history
        var chatMessage = await this.CreateBotMessageOnClient(
            chatId,
            userId,
            prompt,
            response,
            cancellationToken,
            citations,
            tokenUsage
        );
        await this._chatMessageRepository.UpsertAsync(chatMessage);

        return chatMessage;
    }

    /// <summary>
    /// Updates previously saved response in the chat history.
    /// </summary>
    /// <param name="updatedResponse">Updated response from the chat.</param>
    /// <param name="messageId">The chat message ID.</param>
    /// <param name="chatId">The chat ID that's used as the partition Id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task UpdateChatMessageContentAsync(string updatedResponse, string messageId, string chatId, CancellationToken cancellationToken)
    {
        ChatMessage? chatMessage = null;
        if (!await this._chatMessageRepository.TryFindByIdAsync(messageId, chatId, callback: v => chatMessage = v))
        {
            throw new ArgumentException($"Chat message {messageId} does not exist.");
        }

        chatMessage!.Content = updatedResponse;
        await this._chatMessageRepository.UpsertAsync(chatMessage);
    }

    /// <summary>
    /// Create `ChatRequestSettings` for chat response. Parameters are read from the PromptSettings class.
    /// </summary>
    private ChatRequestSettings CreateChatRequestSettings()
    {
        return new ChatRequestSettings
        {
            MaxTokens = this._promptOptions.ResponseTokenLimit,
            Temperature = this._promptOptions.ResponseTemperature,
            TopP = this._promptOptions.ResponseTopP,
            FrequencyPenalty = this._promptOptions.ResponseFrequencyPenalty,
            PresencePenalty = this._promptOptions.ResponsePresencePenalty
        };
    }

    /// <summary>
    /// Create `CompleteRequestSettings` for intent response. Parameters are read from the PromptSettings class.
    /// </summary>
    private CompleteRequestSettings CreateIntentCompletionSettings()
    {
        return new CompleteRequestSettings
        {
            MaxTokens = this._promptOptions.ResponseTokenLimit,
            Temperature = this._promptOptions.IntentTemperature,
            TopP = this._promptOptions.IntentTopP,
            FrequencyPenalty = this._promptOptions.IntentFrequencyPenalty,
            PresencePenalty = this._promptOptions.IntentPresencePenalty,
            StopSequences = new string[] { "] bot:" }
        };
    }

    /// <summary>
    /// Calculate the remaining token budget for the chat response prompt.
    /// This is the token limit minus the token count of the user intent, audience, and the system commands.
    /// </summary>
    /// <param name="promptTemplate">All current messages to use for chat completion</param>
    /// <param name="userIntent">The user message.</param>
    /// <returns>The remaining token limit.</returns>
    private int GetChatContextTokenLimit(ChatCompletionContextMessages promptTemplate, string userInput = "")
    {
        return this._promptOptions.CompletionTokenLimit
            - TokenUtils.GetContextMessagesTokenCount(promptTemplate)
            - TokenUtils.GetContextMessageTokenCount(AuthorRole.User, userInput) // User message has to be included in chat history allowance
            - this._promptOptions.ResponseTokenLimit;
    }

    /// <summary>
    /// Gets token usage totals for each semantic function if not undefined.
    /// </summary>
    /// <param name="chatContext">Context maintained during response generation.</param>
    /// <param name="content">String representing bot response. If null, response is still being generated or was hardcoded.</param>
    /// <returns>Dictionary containing function to token usage mapping for each total that's defined.</returns>
    private Dictionary<string, int> GetTokenUsages(SKContext chatContext, string? content = null)
    {
        var tokenUsageDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Total token usage of each semantic function
        foreach (string function in TokenUtils.semanticFunctions.Values)
        {
            if (chatContext.Variables.TryGetValue($"{function}TokenUsage", out string? tokenUsage))
            {
                tokenUsageDict.Add(function, int.Parse(tokenUsage, CultureInfo.InvariantCulture));
            }
        }

        if (content != null)
        {
            tokenUsageDict.Add(TokenUtils.semanticFunctions["SystemCompletion"]!, TokenUtils.TokenCount(content));
        }

        return tokenUsageDict;
    }

    /// <summary>
    /// Stream the response to the client.
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="prompt">Prompt used to generate the response</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="citations">Citations for the message</param>
    /// <returns>The created chat message</returns>
    private async Task<ChatMessage> StreamResponseToClientAsync(
        string chatId,
        string userId,
        BotResponsePrompt prompt,
        CancellationToken cancellationToken,
        IEnumerable<CitationSource>? citations = null)
    {
        // Create the stream
        var chatCompletion = this._kernel.GetService<IChatCompletion>();
        var stream = chatCompletion.GenerateMessageStreamAsync(prompt.MetaPromptTemplate, this.CreateChatRequestSettings(), cancellationToken);

        // Create message on client
        var chatMessage = await this.CreateBotMessageOnClient(
            chatId,
            userId,
            JsonSerializer.Serialize(prompt),
            string.Empty,
            cancellationToken,
            citations
        );

        // Stream the message to the client
        await foreach (string contentPiece in stream)
        {
            chatMessage.Content += contentPiece;
            await this.UpdateMessageOnClient(chatMessage, cancellationToken);
        }

        return chatMessage;
    }

    /// <summary>
    /// Create an empty message on the client to begin the response.
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="prompt">Prompt used to generate the message</param>
    /// <param name="content">Content of the message</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="citations">Citations for the message</param>
    /// <param name="tokenUsage">Total token usage of response completion</param>
    /// <returns>The created chat message</returns>
    private async Task<ChatMessage> CreateBotMessageOnClient(
        string chatId,
        string userId,
        string prompt,
        string content,
        CancellationToken cancellationToken,
        IEnumerable<CitationSource>? citations = null,
        Dictionary<string, int>? tokenUsage = null)
    {
        var chatMessage = ChatMessage.CreateBotResponseMessage(chatId, content, prompt, citations, tokenUsage);
        await this._messageRelayHubContext.Clients.Group(chatId).SendAsync("ReceiveMessage", chatId, userId, chatMessage, cancellationToken);
        return chatMessage;
    }

    /// <summary>
    /// Update the response on the client.
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task UpdateMessageOnClient(ChatMessage message, CancellationToken cancellationToken)
    {
        await this._messageRelayHubContext.Clients.Group(message.ChatId).SendAsync("ReceiveMessageUpdate", message, cancellationToken);
    }

    /// <summary>
    /// Update the status of the response on the client.
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="status">Current status of the response</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task UpdateBotResponseStatusOnClientAsync(string chatId, string status, CancellationToken cancellationToken)
    {
        await this._messageRelayHubContext.Clients.Group(chatId).SendAsync("ReceiveBotResponseStatus", chatId, status, cancellationToken);
    }

    /// <summary>
    /// Set the system description in the prompt options.
    /// </summary>
    /// <param name="chatId">Id of the chat session</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ArgumentException">Throw if the chat session does not exist.</exception>
    private async Task SetSystemDescriptionAsync(string chatId, CancellationToken cancellationToken)
    {
        ChatSession? chatSession = null;
        if (!await this._chatSessionRepository.TryFindByIdAsync(chatId, callback: v => chatSession = v))
        {
            throw new ArgumentException("Chat session does not exist.");
        }

        this._promptOptions.SystemDescription = chatSession!.SystemDescription;
    }

    # endregion
}
