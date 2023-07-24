// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.TemplateEngine;
using SemanticKernel.Service.CopilotChat.Hubs;
using SemanticKernel.Service.CopilotChat.Models;
using SemanticKernel.Service.CopilotChat.Options;
using SemanticKernel.Service.CopilotChat.Storage;

namespace SemanticKernel.Service.CopilotChat.Skills.ChatSkills;

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
    /// A semantic chat memory skill instance to query semantic memories.
    /// </summary>
    private readonly SemanticChatMemorySkill _semanticChatMemorySkill;

    /// <summary>
    /// A document memory skill instance to query document memories.
    /// </summary>
    private readonly DocumentMemorySkill _documentMemorySkill;

    /// <summary>
    /// A skill instance to acquire external information.
    /// </summary>
    private readonly ExternalInformationSkill _externalInformationSkill;

    /// <summary>
    /// Create a new instance of <see cref="ChatSkill"/>.
    /// </summary>
    public ChatSkill(
        IKernel kernel,
        ChatMessageRepository chatMessageRepository,
        ChatSessionRepository chatSessionRepository,
        IHubContext<MessageRelayHub> messageRelayHubContext,
        IOptions<PromptsOptions> promptOptions,
        IOptions<DocumentMemoryOptions> documentImportOptions,
        CopilotChatPlanner planner,
        ILogger logger)
    {
        this._kernel = kernel;
        this._chatMessageRepository = chatMessageRepository;
        this._chatSessionRepository = chatSessionRepository;
        this._promptOptions = promptOptions.Value;
        this._messageRelayHubContext = messageRelayHubContext;

        this._semanticChatMemorySkill = new SemanticChatMemorySkill(
            promptOptions);
        this._documentMemorySkill = new DocumentMemorySkill(
            promptOptions,
            documentImportOptions);
        this._externalInformationSkill = new ExternalInformationSkill(
            promptOptions,
            planner);
    }

    /// <summary>
    /// Extract user intent from the conversation history.
    /// </summary>
    /// <param name="context">The SKContext.</param>
    [SKFunction, Description("Extract user intent")]
    [SKParameter("chatId", "Chat ID to extract history from")]
    [SKParameter("audience", "The audience the chat bot is interacting with.")]
    public async Task<string> ExtractUserIntentAsync(SKContext context)
    {
        var tokenLimit = this._promptOptions.CompletionTokenLimit;
        var historyTokenBudget =
            tokenLimit -
            this._promptOptions.ResponseTokenLimit -
            Utilities.TokenCount(string.Join("\n", new string[]
                {
                    this._promptOptions.SystemDescription,
                    this._promptOptions.SystemIntent,
                    this._promptOptions.SystemIntentContinuation
                })
            );

        // Clone the context to avoid modifying the original context variables.
        var intentExtractionContext = Utilities.CopyContextWithVariablesClone(context);
        intentExtractionContext.Variables.Set("tokenLimit", historyTokenBudget.ToString(new NumberFormatInfo()));
        intentExtractionContext.Variables.Set("knowledgeCutoff", this._promptOptions.KnowledgeCutoffDate);

        var completionFunction = this._kernel.CreateSemanticFunction(
            this._promptOptions.SystemIntentExtraction,
            skillName: nameof(ChatSkill),
            description: "Complete the prompt.");

        var result = await completionFunction.InvokeAsync(
            intentExtractionContext,
            settings: this.CreateIntentCompletionSettings()
        );

        if (result.ErrorOccurred)
        {
            context.Log.LogError("{0}: {1}", result.LastErrorDescription, result.LastException);
            context.Fail(result.LastErrorDescription);
            return string.Empty;
        }

        return $"User intent: {result}";
    }

    /// <summary>
    /// Extract the list of participants from the conversation history.
    /// Note that only those who have spoken will be included.
    /// </summary>
    [SKFunction, Description("Extract audience list")]
    [SKParameter("chatId", "Chat ID to extract history from")]
    public async Task<string> ExtractAudienceAsync(SKContext context)
    {
        var tokenLimit = this._promptOptions.CompletionTokenLimit;
        var historyTokenBudget =
            tokenLimit -
            this._promptOptions.ResponseTokenLimit -
            Utilities.TokenCount(string.Join("\n", new string[]
                {
                    this._promptOptions.SystemAudience,
                    this._promptOptions.SystemAudienceContinuation,
                })
            );

        // Clone the context to avoid modifying the original context variables.
        var audienceExtractionContext = Utilities.CopyContextWithVariablesClone(context);
        audienceExtractionContext.Variables.Set("tokenLimit", historyTokenBudget.ToString(new NumberFormatInfo()));

        var completionFunction = this._kernel.CreateSemanticFunction(
            this._promptOptions.SystemAudienceExtraction,
            skillName: nameof(ChatSkill),
            description: "Complete the prompt.");

        var result = await completionFunction.InvokeAsync(
            audienceExtractionContext,
            settings: this.CreateIntentCompletionSettings()
        );

        if (result.ErrorOccurred)
        {
            context.Log.LogError("{0}: {1}", result.LastErrorDescription, result.LastException);
            context.Fail(result.LastErrorDescription);
            return string.Empty;
        }

        return $"List of participants: {result}";
    }

    /// <summary>
    /// Extract chat history.
    /// </summary>
    /// <param name="context">Contains the 'tokenLimit' controlling the length of the prompt.</param>
    [SKFunction, Description("Extract chat history")]
    public async Task<string> ExtractChatHistoryAsync(
        [Description("Chat ID to extract history from")] string chatId,
        [Description("Maximum number of tokens")] int tokenLimit)
    {
        var messages = await this._chatMessageRepository.FindByChatIdAsync(chatId);
        var sortedMessages = messages.OrderByDescending(m => m.Timestamp);

        var remainingToken = tokenLimit;

        string historyText = "";
        foreach (var chatMessage in sortedMessages)
        {
            var formattedMessage = chatMessage.ToFormattedString();

            // Plan object is not meaningful content in generating bot response, so shorten to intent only to save on tokens
            if (formattedMessage.Contains("proposedPlan\":", StringComparison.InvariantCultureIgnoreCase))
            {
                string pattern = @"(\[.*?\]).*User Intent:User intent: (.*)(?=""}})";
                Match match = Regex.Match(formattedMessage, pattern);
                if (match.Success)
                {
                    string timestamp = match.Groups[1].Value.Trim();
                    string userIntent = match.Groups[2].Value.Trim();

                    formattedMessage = $"{timestamp} Bot proposed plan to fulfill user intent: {userIntent}";
                }
                else
                {
                    formattedMessage = "Bot proposed plan";
                }
            }

            var tokenCount = Utilities.TokenCount(formattedMessage);

            if (remainingToken - tokenCount >= 0)
            {
                historyText = $"{formattedMessage}\n{historyText}";
                remainingToken -= tokenCount;
            }
            else
            {
                break;
            }
        }

        return $"Chat history:\n{historyText.Trim()}";
    }

    /// <summary>
    /// This is the entry point for getting a chat response. It manages the token limit, saves
    /// messages to memory, and fill in the necessary context variables for completing the
    /// prompt that will be rendered by the template engine.
    /// </summary>
    [SKFunction, Description("Get chat response")]
    public async Task<SKContext> ChatAsync(
        [Description("The new message")] string message,
        [Description("Unique and persistent identifier for the user")] string userId,
        [Description("Name of the user")] string userName,
        [Description("Unique and persistent identifier for the chat")] string chatId,
        [Description("Type of the message")] string messageType,
        [Description("Previously proposed plan that is approved"), DefaultValue(null), SKName("proposedPlan")] string? planJson,
        [Description("ID of the response message for planner"), DefaultValue(null), SKName("responseMessageId")] string? messageId,
        SKContext context)
    {
        // Save this new message to memory such that subsequent chat responses can use it
        await this.UpdateBotResponseStatusOnClient(chatId, "Saving user message to chat history");
        await this.SaveNewMessageAsync(message, userId, userName, chatId, messageType);

        // Clone the context to avoid modifying the original context variables.
        var chatContext = Utilities.CopyContextWithVariablesClone(context);
        chatContext.Variables.Set("knowledgeCutoff", this._promptOptions.KnowledgeCutoffDate);

        // Check if plan exists in ask's context variables.
        // If plan was returned at this point, that means it was approved or cancelled.
        // Update the response previously saved in chat history with state
        if (!string.IsNullOrWhiteSpace(planJson) &&
            !string.IsNullOrEmpty(messageId))
        {
            await this.UpdateChatMessageContentAsync(planJson, messageId);
        }

        // Get the chat response
        ChatMessage? chatMessage = await (chatContext.Variables.ContainsKey("userCancelledPlan")
            ? this.SaveNewResponseAsync("I am sorry the plan did not meet your goals.", string.Empty, chatId, userId)
            : this.GetChatResponseAsync(chatId, userId, chatContext));

        if (chatMessage == null)
        {
            context.Fail(chatContext.LastErrorDescription);
            return context;
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
    /// <returns>The created chat message.</returns>
    private async Task<ChatMessage?> GetChatResponseAsync(string chatId, string userId, SKContext chatContext)
    {
        // Get the audience
        await this.UpdateBotResponseStatusOnClient(chatId, "Extracting audience");
        var audience = await this.GetAudienceAsync(chatContext);
        if (chatContext.ErrorOccurred)
        {
            return null;
        }

        // Extract user intent from the conversation history.
        await this.UpdateBotResponseStatusOnClient(chatId, "Extracting user intent");
        var userIntent = await this.GetUserIntentAsync(chatContext);
        if (chatContext.ErrorOccurred)
        {
            return null;
        }

        chatContext.Variables.Set("audience", audience);
        chatContext.Variables.Set("userIntent", userIntent);

        // Calculate the remaining token budget.
        await this.UpdateBotResponseStatusOnClient(chatId, "Calculating remaining token budget");
        var remainingToken = this.GetChatContextTokenLimit(audience, userIntent);

        // Acquire external information from planner
        await this.UpdateBotResponseStatusOnClient(chatId, "Acquiring external information from planner");
        var externalInformationTokenLimit = (int)(remainingToken * this._promptOptions.ExternalInformationContextWeight);
        var planResult = await this.AcquireExternalInformationAsync(chatContext, userIntent, externalInformationTokenLimit);
        if (chatContext.ErrorOccurred)
        {
            return null;
        }

        // If plan is suggested, send back to user for approval before running
        var proposedPlan = this._externalInformationSkill.ProposedPlan;
        if (proposedPlan != null)
        {
            var prompt = proposedPlan.Plan.Description;
            chatContext.Variables.Set("prompt", prompt);

            // Save a new response to the chat history with the proposed plan content
            return await this.SaveNewResponseAsync(JsonSerializer.Serialize<ProposedPlan>(proposedPlan), prompt, chatId, userId);
        }

        // Query relevant semantic and document memories
        await this.UpdateBotResponseStatusOnClient(chatId, "Extracting semantic and document memories");
        var chatMemoriesTokenLimit = (int)(remainingToken * this._promptOptions.MemoriesResponseContextWeight);
        var documentContextTokenLimit = (int)(remainingToken * this._promptOptions.DocumentContextWeight);
        string[] tasks;
        try {
            tasks = await Task.WhenAll<string>(
                this._semanticChatMemorySkill.QueryMemoriesAsync(userIntent, chatId, chatMemoriesTokenLimit, chatContext.Memory),
                this._documentMemorySkill.QueryDocumentsAsync(userIntent, chatId, documentContextTokenLimit, chatContext.Memory)
            );
        } catch (Exception ex) {
            chatContext.Fail(ex.Message, ex);
            return null;
        }
        var chatMemories = tasks[0];
        var documentMemories = tasks[1];

        // Fill in the chat history if there is any token budget left
        var chatContextComponents = new List<string>() { chatMemories, documentMemories, planResult };
        var chatContextText = string.Join("\n\n", chatContextComponents.Where(c => !string.IsNullOrEmpty(c)));
        var chatHistoryTokenLimit = remainingToken - Utilities.TokenCount(chatContextText);
        if (chatHistoryTokenLimit > 0)
        {
            await this.UpdateBotResponseStatusOnClient(chatId, "Extracting chat history");
            var chatHistory = await this.ExtractChatHistoryAsync(chatId, chatHistoryTokenLimit);
            if (chatContext.ErrorOccurred)
            {
                return null;
            }
            chatContextText = $"{chatContextText}\n{chatHistory}";
        }

        // Set variables needed in prompt
        chatContext.Variables.Set("audience", audience);
        chatContext.Variables.Set("userIntent", userIntent);
        chatContext.Variables.Set("chatContext", chatContextText);

        // Render the prompt
        var promptRenderer = new PromptTemplateEngine();
        var renderedPrompt = await promptRenderer.RenderAsync(
            this._promptOptions.SystemChatPrompt,
            chatContext);
        chatContext.Variables.Set("prompt", renderedPrompt);

        if (chatContext.ErrorOccurred)
        {
            return null;
        }

        // Stream the response to the client
        await this.UpdateBotResponseStatusOnClient(chatId, "Invoking the AI model");
        var chatMessage = await this.StreamResponseToClient(chatId, userId, renderedPrompt);

        // Extract semantic chat memory
        await this.UpdateBotResponseStatusOnClient(chatId, "Extracting semantic chat memory");
        await SemanticChatMemoryExtractor.ExtractSemanticChatMemoryAsync(
            chatId,
            this._kernel,
            chatContext,
            this._promptOptions);

        // Save the message
        await this.UpdateBotResponseStatusOnClient(chatId, "Saving message to chat history");
        await this._chatMessageRepository.UpsertAsync(chatMessage);
        return chatMessage;
    }

    /// <summary>
    /// Helper function create the correct context variables to
    /// extract audience from the conversation history.
    /// </summary>
    private async Task<string> GetAudienceAsync(SKContext context)
    {
        var contextVariables = new ContextVariables();
        contextVariables.Set("chatId", context["chatId"]);

        var audienceContext = new SKContext(
            contextVariables,
            context.Memory,
            context.Skills,
            context.Log,
            context.CancellationToken
        );

        var audience = await this.ExtractAudienceAsync(audienceContext);

        // Propagate the error
        if (audienceContext.ErrorOccurred)
        {
            context.Fail(audienceContext.LastErrorDescription);
        }

        return audience;
    }

    /// <summary>
    /// Helper function create the correct context variables to
    /// extract user intent from the conversation history.
    /// </summary>
    private async Task<string> GetUserIntentAsync(SKContext context)
    {
        // TODO: Regenerate user intent if plan was modified
        if (!context.Variables.TryGetValue("planUserIntent", out string? userIntent))
        {
            var contextVariables = new ContextVariables();
            contextVariables.Set("chatId", context["chatId"]);
            contextVariables.Set("audience", context["userName"]);

            var intentContext = new SKContext(
                contextVariables,
                context.Memory,
                context.Skills,
                context.Log,
                context.CancellationToken
            );

            userIntent = await this.ExtractUserIntentAsync(intentContext);
            // Propagate the error
            if (intentContext.ErrorOccurred)
            {
                context.Fail(intentContext.LastErrorDescription);
            }
        }

        return userIntent;
    }

    /// <summary>
    /// Helper function create the correct context variables to
    /// query chat memories from the chat memory store.
    /// </summary>
    /// <returns>The chat memories.</returns>
    /// <param name="context">The SKContext.</param>
    /// <param name="userIntent">The user intent.</param>
    /// <param name="tokenLimit">Maximum number of tokens.</param>
    private Task<string> QueryChatMemoriesAsync(SKContext context, string userIntent, int tokenLimit)
    {
        return this._semanticChatMemorySkill.QueryMemoriesAsync(userIntent, context["chatId"], tokenLimit, context.Memory);
    }

    /// <summary>
    /// Helper function create the correct context variables to
    /// query document memories from the document memory store.
    /// </summary>
    /// <returns>The document memories.</returns>
    /// <param name="context">The SKContext.</param>
    /// <param name="userIntent">The user intent.</param>
    /// <param name="tokenLimit">Maximum number of tokens.</param>
    private Task<string> QueryDocumentsAsync(SKContext context, string userIntent, int tokenLimit)
    {
        return this._documentMemorySkill.QueryDocumentsAsync(userIntent, context["chatId"], tokenLimit, context.Memory);
    }

    /// <summary>
    /// Helper function create the correct context variables to acquire external information.
    /// </summary>
    /// <returns>The plan.</returns>
    /// <param name="context">The SKContext.</param>
    /// <param name="userIntent">The user intent.</param>
    /// <param name="tokenLimit">Maximum number of tokens.</param>
    private async Task<string> AcquireExternalInformationAsync(SKContext context, string userIntent, int tokenLimit)
    {
        var contextVariables = context.Variables.Clone();
        contextVariables.Set("tokenLimit", tokenLimit.ToString(new NumberFormatInfo()));

        var planContext = new SKContext(
            contextVariables,
            context.Memory,
            context.Skills,
            context.Log,
            context.CancellationToken
        );

        var plan = await this._externalInformationSkill.AcquireExternalInformationAsync(userIntent, planContext);

        // Propagate the error
        if (planContext.ErrorOccurred)
        {
            context.Fail(planContext.LastErrorDescription);
        }

        return plan;
    }

    /// <summary>
    /// Save a new message to the chat history.
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="userId">The user ID</param>
    /// <param name="userName"></param>
    /// <param name="chatId">The chat ID</param>
    /// <param name="type">Type of the message</param>
    private async Task<ChatMessage> SaveNewMessageAsync(string message, string userId, string userName, string chatId, string type)
    {
        // Make sure the chat exists.
        if (!await this._chatSessionRepository.TryFindByIdAsync(chatId, v => _ = v))
        {
            throw new ArgumentException("Chat session does not exist.");
        }

        var chatMessage = new ChatMessage(
            userId,
            userName,
            chatId,
            message,
            "",
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
    /// <returns>The created chat message.</returns>
    private async Task<ChatMessage?> SaveNewResponseAsync(string response, string prompt, string chatId, string userId)
    {
        // Make sure the chat exists.
        if (!await this._chatSessionRepository.TryFindByIdAsync(chatId, v => _ = v))
        {
            throw new ArgumentException("Chat session does not exist.");
        }

        // Save message to chat history
        var chatMessage = await this.CreateBotMessageOnClient(chatId, userId, prompt, response);
        await this._chatMessageRepository.UpsertAsync(chatMessage);
        return chatMessage;
    }

    /// <summary>
    /// Updates previously saved response in the chat history.
    /// </summary>
    /// <param name="updatedResponse">Updated response from the chat.</param>
    /// <param name="messageId">The chat message ID</param>
    private async Task UpdateChatMessageContentAsync(string updatedResponse, string messageId)
    {
        // Make sure the chat exists.
        var chatMessage = await this._chatMessageRepository.FindByIdAsync(messageId);
        chatMessage.Content = updatedResponse;

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
    /// This is the token limit minus the token count of the user intent and the system commands.
    /// </summary>
    /// <param name="audience">The audience returned by the model.</param>
    /// <param name="userIntent">The user intent returned by the model.</param>
    /// <returns>The remaining token limit.</returns>
    private int GetChatContextTokenLimit(string audience, string userIntent)
    {
        var tokenLimit = this._promptOptions.CompletionTokenLimit;
        var remainingToken =
            tokenLimit -
            Utilities.TokenCount(audience) -
            Utilities.TokenCount(userIntent) -
            this._promptOptions.ResponseTokenLimit -
            Utilities.TokenCount(string.Join("\n", new string[]
                {
                            this._promptOptions.SystemDescription,
                            this._promptOptions.SystemResponse,
                            this._promptOptions.SystemChatContinuation
                })
            );

        return remainingToken;
    }

    /// <summary>
    /// Stream the response to the client.
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="prompt">Prompt used to generate the response</param>
    /// <returns>The created chat message</returns>
    private async Task<ChatMessage> StreamResponseToClient(string chatId, string userId, string prompt)
    {
        // Create the stream
        var chatCompletion = this._kernel.GetService<IChatCompletion>();
        var stream = chatCompletion.GenerateMessageStreamAsync(chatCompletion.CreateNewChat(prompt), this.CreateChatRequestSettings());

        // Create message on client
        var chatMessage = await this.CreateBotMessageOnClient(chatId, userId, prompt, string.Empty);

        // Stream the message to the client
        await foreach (string contentPiece in stream)
        {
            chatMessage.Content += contentPiece;
            await this.UpdateMessageContentOnClient(chatId, chatMessage);
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
    /// <returns>The created chat message</returns>
    private async Task<ChatMessage> CreateBotMessageOnClient(string chatId, string userId, string prompt, string content)
    {
        var chatMessage = ChatMessage.CreateBotResponseMessage(chatId, content, prompt);
        await this._messageRelayHubContext.Clients.Group(chatId).SendAsync("ReceiveMessage", chatId, userId, chatMessage);
        return chatMessage;
    }

    /// <summary>
    /// Update the response on the client.
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="message">The message</param>
    private async Task UpdateMessageContentOnClient(string chatId, ChatMessage message)
    {
        await this._messageRelayHubContext.Clients.Group(chatId).SendAsync("ReceiveMessageContent", chatId, message.Id, message.Content);
    }

    /// <summary>
    /// Update the status of the response on the client.
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="status">Current status of the response</param>
    private async Task UpdateBotResponseStatusOnClient(string chatId, string status)
    {
        await this._messageRelayHubContext.Clients.Group(chatId).SendAsync("ReceiveBotResponseStatus", chatId, status);
    }

    # endregion
}
