// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Plugins.Utils;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using CopilotChatMessage = CopilotChat.WebApi.Models.Storage.CopilotChatMessage;

namespace CopilotChat.WebApi.Plugins.Chat;

/// <summary>
/// ChatPlugin offers a more coherent chat experience by using memories
/// to extract conversation history and user intentions.
/// </summary>
public class ChatPlugin
{
    /// <summary>
    /// A kernel instance to create a completion function since each invocation
    /// of the <see cref="ChatAsync"/> function will generate a new prompt dynamically.
    /// </summary>
    private readonly Kernel _kernel;

    /// <summary>
    /// Client for the kernel memory service.
    /// </summary>
    private readonly IKernelMemory _memoryClient;

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
    /// A kernel memory retriever instance to query semantic memories.
    /// </summary>
    private readonly SemanticMemoryRetriever _semanticMemoryRetriever;

    /// <summary>
    /// Azure content safety moderator.
    /// </summary>
    private readonly AzureContentSafety? _contentSafety = null;

    /// <summary>
    /// Create a new instance of <see cref="ChatPlugin"/>.
    /// </summary>
    public ChatPlugin(
        Kernel kernel,
        IKernelMemory memoryClient,
        ChatMessageRepository chatMessageRepository,
        ChatSessionRepository chatSessionRepository,
        IHubContext<MessageRelayHub> messageRelayHubContext,
        IOptions<PromptsOptions> promptOptions,
        IOptions<DocumentMemoryOptions> documentImportOptions,
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

        this._contentSafety = contentSafety;
    }

    /// <summary>
    /// Extract user intent from the conversation history.
    /// </summary>
    /// <param name="kernelArguments">The KernelArguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<string> ExtractUserIntentAsync(KernelArguments kernelArguments, CancellationToken cancellationToken = default)
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
        KernelArguments intentExtractionContext = new(kernelArguments);
        intentExtractionContext["tokenLimit"] = historyTokenBudget.ToString(new NumberFormatInfo());
        intentExtractionContext["knowledgeCutoff"] = this._promptOptions.KnowledgeCutoffDate;

        var completionFunction = this._kernel.CreateFunctionFromPrompt(
            this._promptOptions.SystemIntentExtraction,
            this.CreateIntentCompletionSettings(),
            functionName: nameof(ChatPlugin),
            description: "Complete the prompt.");

        var result = await completionFunction.InvokeAsync(
            this._kernel,
            intentExtractionContext,
            cancellationToken
        );

        // Get token usage from ChatCompletion result and add to context
        TokenUtils.GetFunctionTokenUsage(result, intentExtractionContext, this._logger, "SystemIntentExtraction");

        return $"User intent: {result}";
    }

    /// <summary>
    /// Extract the list of participants from the conversation history.
    /// Note that only those who have spoken will be included.
    /// </summary>
    /// <param name="context">The SKContext.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<string> ExtractAudienceAsync(KernelArguments context, CancellationToken cancellationToken = default)
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
        KernelArguments audienceExtractionContext = new(context);
        audienceExtractionContext["tokenLimit"] = historyTokenBudget.ToString(new NumberFormatInfo());

        var completionFunction = this._kernel.CreateFunctionFromPrompt(
            this._promptOptions.SystemAudienceExtraction,
            this.CreateIntentCompletionSettings(),
            functionName: nameof(ChatPlugin),
            description: "Complete the prompt.");

        var result = await completionFunction.InvokeAsync(
            this._kernel,
            audienceExtractionContext,
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
    [KernelFunction, Description("Extract chat history")]
    public Task<string> ExtractChatHistory(
        [Description("Chat ID to extract history from")] string chatId,
        [Description("Maximum number of tokens")] int tokenLimit,
        CancellationToken cancellationToken = default)
    {
        return this.GetAllowedChatHistoryAsync(chatId, tokenLimit, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Extract chat history within token limit as a formatted string and optionally update the ChatHistory object with the allotted messages
    /// </summary>
    /// <param name="chatId">Chat ID to extract history from.</param>
    /// <param name="tokenLimit">Maximum number of tokens.</param>
    /// <param name="chatHistory">Optional ChatHistory object tracking allotted messages.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Chat history as a string.</returns>
    private async Task<string> GetAllowedChatHistoryAsync(
        string chatId,
        int tokenLimit,
        ChatHistory? chatHistory = null,
        CancellationToken cancellationToken = default)
    {
        var messages = await this._chatMessageRepository.FindByChatIdAsync(chatId);
        var sortedMessages = messages.OrderByDescending(m => m.Timestamp);

        ChatHistory allottedChatHistory = new();
        var remainingToken = tokenLimit;
        string historyText = string.Empty;

        foreach (var chatMessage in sortedMessages)
        {
            var formattedMessage = chatMessage.ToFormattedString();

            if (chatMessage.Type == CopilotChatMessage.ChatMessageType.Document)
            {
                continue;
            }

            var promptRole = chatMessage.AuthorRole == CopilotChatMessage.AuthorRoles.Bot ? AuthorRole.System : AuthorRole.User;
            var tokenCount = chatHistory is not null ? TokenUtils.GetContextMessageTokenCount(promptRole, formattedMessage) : TokenUtils.TokenCount(formattedMessage);

            if (remainingToken - tokenCount >= 0)
            {
                historyText = $"{formattedMessage}\n{historyText}";
                if (chatMessage.AuthorRole == CopilotChatMessage.AuthorRoles.Bot)
                {
                    // Message doesn't have to be formatted for bot. This helps with asserting a natural language response from the LLM (no date or author preamble).
                    var botMessage = chatMessage.Content;
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

        chatHistory?.AddRange(allottedChatHistory.Reverse());

        return $"Chat history:\n{historyText.Trim()}";
    }

    /// <summary>
    /// This is the entry point for getting a chat response. It manages the token limit, saves
    /// messages to memory, and fill in the necessary context variables for completing the
    /// prompt that will be rendered by the template engine.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    [KernelFunction, Description("Get chat response")]
    public async Task<KernelArguments> ChatAsync(
        [Description("The new message")] string message,
        [Description("Unique and persistent identifier for the user")] string userId,
        [Description("Name of the user")] string userName,
        [Description("Unique and persistent identifier for the chat")] string chatId,
        [Description("Type of the message")] string messageType,
        KernelArguments context,
        CancellationToken cancellationToken = default)
    {
        // Set the system description in the prompt options
        await this.SetSystemDescriptionAsync(chatId, cancellationToken);

        // Save this new message to memory such that subsequent chat responses can use it
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Saving user message to chat history", cancellationToken);
        var newUserMessage = await this.SaveNewMessageAsync(message, userId, userName, chatId, messageType, cancellationToken);

        // Clone the context to avoid modifying the original context variables.
        KernelArguments chatContext = new(context);
        chatContext["knowledgeCutoff"] = this._promptOptions.KnowledgeCutoffDate;

        CopilotChatMessage chatMessage = await this.GetChatResponseAsync(chatId, userId, chatContext, newUserMessage, cancellationToken);
        context["input"] = chatMessage.Content;

        if (chatMessage.TokenUsage != null)
        {
            context["tokenUsage"] = JsonSerializer.Serialize(chatMessage.TokenUsage);
        }
        else
        {
            this._logger.LogWarning("ChatPlugin.ChatAsync token usage unknown. Ensure token management has been implemented correctly.");
        }

        return context;
    }

    /// <summary>
    /// Generate the necessary chat context to create a prompt then invoke the model to get a response.
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="chatContext">The KernelArguments.</param>
    /// <param name="userMessage">ChatMessage object representing new user message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created chat message containing the model-generated response.</returns>
    private async Task<CopilotChatMessage> GetChatResponseAsync(string chatId, string userId, KernelArguments chatContext, CopilotChatMessage userMessage, CancellationToken cancellationToken)
    {
        // Render system instruction components and create the meta-prompt template
        var systemInstructions = await AsyncUtils.SafeInvokeAsync(
            () => this.RenderSystemInstructions(chatId, chatContext, cancellationToken), nameof(RenderSystemInstructions));
        var chatCompletion = this._kernel.GetRequiredService<IChatCompletionService>();
        ChatHistory chatHistory = new(systemInstructions);

        // Bypass audience extraction if Auth is disabled
        var audience = string.Empty;
        if (!PassThroughAuthenticationHandler.IsDefaultUser(userId))
        {
            // Get the audience
            await this.UpdateBotResponseStatusOnClientAsync(chatId, "Extracting audience", cancellationToken);
            audience = await AsyncUtils.SafeInvokeAsync(
                () => this.GetAudienceAsync(chatContext, cancellationToken), nameof(GetAudienceAsync));
            chatHistory.AddSystemMessage(audience);
        }

        // Extract user intent from the conversation history.
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Extracting user intent", cancellationToken);
        var userIntent = await AsyncUtils.SafeInvokeAsync(
            () => this.GetUserIntentAsync(chatContext, cancellationToken), nameof(GetUserIntentAsync));
        chatHistory.AddSystemMessage(userIntent);

        // Calculate the remaining token budget.
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Calculating remaining token budget", cancellationToken);
        var remainingTokenBudget = this.GetChatContextTokenLimit(chatHistory, userMessage.ToFormattedString());

        // Query relevant semantic and document memories
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Extracting semantic and document memories", cancellationToken);
        var chatMemoriesTokenLimit = (int)(remainingTokenBudget * this._promptOptions.MemoriesResponseContextWeight);
        (var memoryText, var citationMap) = await this._semanticMemoryRetriever.QueryMemoriesAsync(userIntent, chatId, chatMemoriesTokenLimit);

        if (!string.IsNullOrWhiteSpace(memoryText))
        {
            chatHistory.AddSystemMessage(memoryText);
        }

        // Fill in the chat history with remaining token budget.
        string allowedChatHistory = string.Empty;
        var allowedChatHistoryTokenBudget = remainingTokenBudget - TokenUtils.GetContextMessageTokenCount(AuthorRole.System, memoryText);

        // Append previous messages
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Extracting chat history", cancellationToken);
        allowedChatHistory = await this.GetAllowedChatHistoryAsync(chatId, allowedChatHistoryTokenBudget, chatHistory, cancellationToken);

        // Calculate token usage of prompt template
        chatContext[TokenUtils.GetFunctionKey(this._logger, "SystemMetaPrompt")!] = TokenUtils.GetContextMessagesTokenCount(chatHistory).ToString(CultureInfo.CurrentCulture);

        // Stream the response to the client
        var promptView = new BotResponsePrompt(systemInstructions, audience, userIntent, memoryText, allowedChatHistory, chatHistory);
        return await this.HandleBotResponseAsync(chatId, userId, chatContext, promptView, cancellationToken, citationMap.Values.AsEnumerable());
    }

    /// <summary>
    /// Helper function to render system instruction components.
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="context">The KernelArguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<string> RenderSystemInstructions(string chatId, KernelArguments context, CancellationToken cancellationToken)
    {
        // Render system instruction components
        await this.UpdateBotResponseStatusOnClientAsync(chatId, "Initializing prompt", cancellationToken);

        var promptTemplateFactory = new KernelPromptTemplateFactory();
        var promptTemplate = promptTemplateFactory.Create(new PromptTemplateConfig(this._promptOptions.SystemPersona));
        return await promptTemplate.RenderAsync(this._kernel, context, cancellationToken);
    }

    /// <summary>
    /// Helper function to handle final steps of bot response generation, including streaming to client, generating semantic text memory, calculating final token usages, and saving to chat history.
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="chatContext">Chat context.</param>
    /// <param name="promptView">The prompt view.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<CopilotChatMessage> HandleBotResponseAsync(
        string chatId,
        string userId,
        KernelArguments chatContext,
        BotResponsePrompt promptView,
        CancellationToken cancellationToken,
        IEnumerable<CitationSource>? citations = null,
        string? responseContent = null)
    {
        CopilotChatMessage chatMessage;
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
    private async Task<string> GetAudienceAsync(KernelArguments context, CancellationToken cancellationToken)
    {
        KernelArguments audienceContext = new(context);
        var audience = await this.ExtractAudienceAsync(audienceContext, cancellationToken);

        // Copy token usage into original chat context
        var functionKey = TokenUtils.GetFunctionKey(this._logger, "SystemAudienceExtraction")!;
        if (audienceContext.TryGetValue(functionKey, out object? tokenUsage))
        {
            context[functionKey] = tokenUsage;
        }

        return audience;
    }

    /// <summary>
    /// Helper function that creates the correct context variables to
    /// extract the user intent from the conversation history.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<string> GetUserIntentAsync(KernelArguments context, CancellationToken cancellationToken)
    {
        KernelArguments intentContext = new(context);
        string userIntent = await this.ExtractUserIntentAsync(intentContext, cancellationToken);

        // Copy token usage into original chat context
        var functionKey = TokenUtils.GetFunctionKey(this._logger, "SystemIntentExtraction")!;
        if (intentContext.TryGetValue(functionKey!, out object? tokenUsage))
        {
            context[functionKey!] = tokenUsage;
        }

        return userIntent;
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
    private async Task<CopilotChatMessage> SaveNewMessageAsync(string message, string userId, string userName, string chatId, string type, CancellationToken cancellationToken)
    {
        // Make sure the chat exists.
        if (!await this._chatSessionRepository.TryFindByIdAsync(chatId))
        {
            throw new ArgumentException("Chat session does not exist.");
        }

        var chatMessage = new CopilotChatMessage(
            userId,
            userName,
            chatId,
            message,
            string.Empty,
            null,
            CopilotChatMessage.AuthorRoles.User,
            // Default to a standard message if the `type` is not recognized
            Enum.TryParse(type, out CopilotChatMessage.ChatMessageType typeAsEnum) && Enum.IsDefined(typeof(CopilotChatMessage.ChatMessageType), typeAsEnum)
                ? typeAsEnum
                : CopilotChatMessage.ChatMessageType.Message);

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
    private async Task<CopilotChatMessage> SaveNewResponseAsync(
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
        CopilotChatMessage? chatMessage = null;
        if (!await this._chatMessageRepository.TryFindByIdAsync(messageId, chatId, callback: v => chatMessage = v))
        {
            throw new ArgumentException($"Chat message {messageId} does not exist.");
        }

        chatMessage!.Content = updatedResponse;
        await this._chatMessageRepository.UpsertAsync(chatMessage);
    }

    /// <summary>
    /// Create `OpenAIPromptExecutionSettings` for chat response. Parameters are read from the PromptSettings class.
    /// </summary>
    private OpenAIPromptExecutionSettings CreateChatRequestSettings()
    {
        return new OpenAIPromptExecutionSettings
        {
            MaxTokens = this._promptOptions.ResponseTokenLimit,
            Temperature = this._promptOptions.ResponseTemperature,
            TopP = this._promptOptions.ResponseTopP,
            FrequencyPenalty = this._promptOptions.ResponseFrequencyPenalty,
            PresencePenalty = this._promptOptions.ResponsePresencePenalty,
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };
    }

    /// <summary>
    /// Create `OpenAIPromptExecutionSettings` for intent response. Parameters are read from the PromptSettings class.
    /// </summary>
    private OpenAIPromptExecutionSettings CreateIntentCompletionSettings()
    {
        return new OpenAIPromptExecutionSettings
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
    private int GetChatContextTokenLimit(ChatHistory promptTemplate, string userInput = "")
    {
        return this._promptOptions.CompletionTokenLimit
            - TokenUtils.GetContextMessagesTokenCount(promptTemplate)
            - TokenUtils.GetContextMessageTokenCount(AuthorRole.User, userInput) // User message has to be included in chat history allowance
            - this._promptOptions.ResponseTokenLimit;
    }

    /// <summary>
    /// Gets token usage totals for each semantic function if not undefined.
    /// </summary>
    /// <param name="kernelArguments">Context maintained during response generation.</param>
    /// <param name="content">String representing bot response. If null, response is still being generated or was hardcoded.</param>
    /// <returns>Dictionary containing function to token usage mapping for each total that's defined.</returns>
    private Dictionary<string, int> GetTokenUsages(KernelArguments kernelArguments, string? content = null)
    {
        var tokenUsageDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Total token usage of each semantic function
        foreach (string function in TokenUtils.semanticFunctions.Values)
        {
            if (kernelArguments.TryGetValue($"{function}TokenUsage", out object? tokenUsage))
            {
                if (tokenUsage is string tokenUsageString)
                {
                    tokenUsageDict.Add(function, int.Parse(tokenUsageString, CultureInfo.InvariantCulture));
                }
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
    private async Task<CopilotChatMessage> StreamResponseToClientAsync(
        string chatId,
        string userId,
        BotResponsePrompt prompt,
        CancellationToken cancellationToken,
        IEnumerable<CitationSource>? citations = null)
    {
        // Create the stream
        var chatCompletion = this._kernel.GetRequiredService<IChatCompletionService>();
        var stream =
            chatCompletion.GetStreamingChatMessageContentsAsync(
                prompt.MetaPromptTemplate,
                this.CreateChatRequestSettings(),
                this._kernel,
                cancellationToken);

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
        await foreach (var contentPiece in stream)
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
    private async Task<CopilotChatMessage> CreateBotMessageOnClient(
        string chatId,
        string userId,
        string prompt,
        string content,
        CancellationToken cancellationToken,
        IEnumerable<CitationSource>? citations = null,
        Dictionary<string, int>? tokenUsage = null)
    {
        var chatMessage = CopilotChatMessage.CreateBotResponseMessage(chatId, content, prompt, citations, tokenUsage);
        await this._messageRelayHubContext.Clients.Group(chatId).SendAsync("ReceiveMessage", chatId, userId, chatMessage, cancellationToken);
        return chatMessage;
    }

    /// <summary>
    /// Update the response on the client.
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task UpdateMessageOnClient(CopilotChatMessage message, CancellationToken cancellationToken)
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

        this._promptOptions.SystemDescription = chatSession!.SafeSystemDescription;
    }
}
