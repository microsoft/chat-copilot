// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Extensions;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Skills;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticMemory;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// Controller for chat history.
/// This controller is responsible for creating new chat sessions, retrieving chat sessions,
/// retrieving chat messages, and editing chat sessions.
/// </summary>
[ApiController]
public class ChatHistoryController : ControllerBase
{
    private const string ChatEditedClientCall = "ChatEdited";
    private const string ChatDeletedClientCall = "ChatDeleted";

    private readonly ILogger<ChatHistoryController> _logger;
    private readonly ISemanticMemoryClient _memoryClient;
    private readonly ChatSessionRepository _sessionRepository;
    private readonly ChatMessageRepository _messageRepository;
    private readonly ChatParticipantRepository _participantRepository;
    private readonly ChatMemorySourceRepository _sourceRepository;
    private readonly PromptsOptions _promptOptions;
    private readonly IAuthInfo _authInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatHistoryController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="memoryClient">Memory client.</param>
    /// <param name="sessionRepository">The chat session repository.</param>
    /// <param name="messageRepository">The chat message repository.</param>
    /// <param name="participantRepository">The chat participant repository.</param>
    /// <param name="sourceRepository">The chat memory resource repository.</param>
    /// <param name="promptsOptions">The prompts options.</param>
    /// <param name="authInfo">The auth info for the current request.</param>
    public ChatHistoryController(
        ILogger<ChatHistoryController> logger,
        ISemanticMemoryClient memoryClient,
        ChatSessionRepository sessionRepository,
        ChatMessageRepository messageRepository,
        ChatParticipantRepository participantRepository,
        ChatMemorySourceRepository sourceRepository,
        IOptions<PromptsOptions> promptsOptions,
        IAuthInfo authInfo)
    {
        this._logger = logger;
        this._memoryClient = memoryClient;
        this._sessionRepository = sessionRepository;
        this._messageRepository = messageRepository;
        this._participantRepository = participantRepository;
        this._sourceRepository = sourceRepository;
        this._promptOptions = promptsOptions.Value;
        this._authInfo = authInfo;
    }

    /// <summary>
    /// Create a new chat session and populate the session with the initial bot message.
    /// </summary>
    /// <param name="chatParameter">Contains the title of the chat.</param>
    /// <returns>The HTTP action result.</returns>
    [HttpPost]
    [Route("chatSession/create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateChatSessionAsync(
        [FromBody] CreateChatParameters chatParameters)
    {
        if (chatParameters.Title == null)
        {
            return this.BadRequest("Chat session parameters cannot be null.");
        }

        // Create a new chat session
        var newChat = new ChatSession(chatParameters.Title, this._promptOptions.SystemDescription);
        await this._sessionRepository.CreateAsync(newChat);

        // Create initial bot message
        var chatMessage = ChatMessage.CreateBotResponseMessage(
            newChat.Id,
            this._promptOptions.InitialBotMessage,
            string.Empty, // The initial bot message doesn't need a prompt.
            null,
            TokenUtilities.EmptyTokenUsages());
        await this._messageRepository.CreateAsync(chatMessage);

        // Add the user to the chat session
        await this._participantRepository.CreateAsync(new ChatParticipant(this._authInfo.UserId, newChat.Id));

        this._logger.LogDebug("Created chat session with id {0}.", newChat.Id);
        return this.CreatedAtAction(
            nameof(this.GetChatSessionByIdAsync),
            new { chatId = newChat.Id },
            new CreateChatResponse(newChat, chatMessage));
    }

    /// <summary>
    /// Get a chat session by id.
    /// </summary>
    /// <param name="chatId">The chat id.</param>
    [HttpGet]
    [ActionName("GetChatSessionByIdAsync")]
    [Route("chatSession/getChat/{chatId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> GetChatSessionByIdAsync(Guid chatId)
    {
        ChatSession? chat = null;
        if (await this._sessionRepository.TryFindByIdAsync(chatId.ToString(), callback: v => chat = v))
        {
            return this.Ok(chat);
        }

        return this.NotFound($"No chat session found for chat id '{chatId}'.");
    }

    /// <summary>
    /// Get all chat sessions associated with the logged in user. Return an empty list if no chats are found.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns>A list of chat sessions. An empty list if the user is not in any chat session.</returns>
    [HttpGet]
    [Route("chatSession/getAllChats/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllChatSessionsAsync(string userId)
    {
        // Get all participants that belong to the user.
        // Then get all the chats from the list of participants.
        var chatParticipants = await this._participantRepository.FindByUserIdAsync(this._authInfo.UserId);

        var chats = new List<ChatSession>();
        foreach (var chatParticipant in chatParticipants)
        {
            ChatSession? chat = null;
            if (await this._sessionRepository.TryFindByIdAsync(chatParticipant.ChatId, callback: v => chat = v))
            {
                chats.Add(chat!);
            }
            else
            {
                this._logger.LogDebug(
                    "Failed to find chat session with id {0}", chatParticipant.ChatId);
            }
        }

        return this.Ok(chats);
    }

    /// <summary>
    /// Get all chat messages for a chat session.
    /// The list will be ordered with the first entry being the most recent message.
    /// </summary>
    /// <param name="chatId">The chat id.</param>
    /// <param name="startIdx">The start index at which the first message will be returned.</param>
    /// <param name="count">The number of messages to return. -1 will return all messages starting from startIdx.</param>
    [HttpGet]
    [Route("chatSession/getChatMessages/{chatId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> GetChatMessagesAsync(
        Guid chatId,
        [FromQuery] int startIdx = 0,
        [FromQuery] int count = -1)
    {
        // TODO:  [Issue #48] the code mixes strings and Guid without being explicit about the serialization format
        var chatMessages = await this._messageRepository.FindByChatIdAsync(chatId.ToString());
        if (!chatMessages.Any())
        {
            return this.NotFound($"No messages found for chat id '{chatId}'.");
        }

        chatMessages = chatMessages.OrderByDescending(m => m.Timestamp).Skip(startIdx);
        if (count >= 0) { chatMessages = chatMessages.Take(count); }

        return this.Ok(chatMessages);
    }

    /// <summary>
    /// Edit a chat session.
    /// </summary>
    /// <param name="chatParameters">Object that contains the parameters to edit the chat.</param>
    [HttpPost]
    [Route("chatSession/edit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> EditChatSessionAsync(
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromBody] EditChatParameters chatParameters)
    {
        string? chatId = chatParameters.Id;

        if (chatId == null)
        {
            return this.BadRequest("Chat id must be specified.");
        }

        // Verify access to chat session
        // TODO: [Issue #141] This can be removed when route is updated to include chatId, so that we can leverage RequireChatParticipant policy.
        bool isUserInChat = await this._participantRepository.IsUserInChatAsync(this._authInfo.UserId, chatId);
        if (!isUserInChat)
        {
            return this.Forbid("User does not have access to the specified chat.");
        }

        ChatSession? chat = null;
        if (await this._sessionRepository.TryFindByIdAsync(chatId, callback: v => chat = v))
        {
            chat!.Title = chatParameters.Title ?? chat!.Title;
            chat!.SystemDescription = chatParameters.SystemDescription ?? chat!.SystemDescription;
            chat!.MemoryBalance = chatParameters.MemoryBalance ?? chat!.MemoryBalance;
            await this._sessionRepository.UpsertAsync(chat);
            await messageRelayHubContext.Clients.Group(chatId).SendAsync(ChatEditedClientCall, chat);
            return this.Ok(chat);
        }

        return this.NotFound($"No chat session found for chat id '{chatId}'.");
    }

    /// <summary>
    /// Service API to get a list of imported sources.
    /// </summary>
    [Route("chatSession/{chatId:guid}/sources")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<ActionResult<IEnumerable<MemorySource>>> GetSourcesAsync(Guid chatId)
    {
        this._logger.LogInformation("Get imported sources of chat session {0}", chatId);

        if (await this._sessionRepository.TryFindByIdAsync(chatId.ToString()))
        {
            var sources = await this._sourceRepository.FindByChatIdAsync(chatId.ToString());
            return this.Ok(sources);
        }

        return this.NotFound($"No chat session found for chat id '{chatId}'.");
    }

    /// <summary>
    /// Delete a chat session.
    /// </summary>
    /// <param name="chatId">The chat id.</param>
    [HttpDelete]
    [Route("chatSession/{chatId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> DeleteChatSessionAsync(
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        Guid chatId,
        CancellationToken cancellationToken)
    {
        var chatIdString = chatId.ToString();
        ChatSession? chatToDelete = null;
        try
        {
            // Make sure the chat session exists
            chatToDelete = await this._sessionRepository.FindByIdAsync(chatIdString);
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"No chat session found for chat id '{chatId}'.");
        }

        // Delete any resources associated with the chat session.
        try
        {
            await this.DeleteChatResourcesAsync(chatIdString, cancellationToken);
        }
        catch (AggregateException)
        {
            return this.StatusCode(500, $"Failed to delete resources for chat id '{chatId}'.");
        }

        // Delete chat session and broadcast update to all participants.
        await this._sessionRepository.DeleteAsync(chatToDelete);
        await messageRelayHubContext.Clients.Group(chatIdString).SendAsync(ChatDeletedClientCall, chatIdString, this._authInfo.UserId, cancellationToken: cancellationToken);

        return this.NoContent();
    }

    /// <summary>
    /// Deletes all associated resources (messages, memories, participants) associated with a chat session.
    /// </summary>
    /// <param name="chatId">The chat id.</param>
    private async Task DeleteChatResourcesAsync(string chatId, CancellationToken cancellationToken)
    {
        var cleanupTasks = new List<Task>();

        // Create and store the tasks for deleting all users tied to the chat.
        var participants = await this._participantRepository.FindByChatIdAsync(chatId);
        foreach (var participant in participants)
        {
            cleanupTasks.Add(this._participantRepository.DeleteAsync(participant));
        }

        // Create and store the tasks for deleting chat messages.
        var messages = await this._messageRepository.FindByChatIdAsync(chatId);
        foreach (var message in messages)
        {
            cleanupTasks.Add(this._messageRepository.DeleteAsync(message));
        }

        // Create and store the tasks for deleting memory sources.
        var sources = await this._sourceRepository.FindByChatIdAsync(chatId, false);
        foreach (var source in sources)
        {
            cleanupTasks.Add(this._sourceRepository.DeleteAsync(source));
        }

        // Create and store the tasks for deleting semantic memories.
        cleanupTasks.Add(this._memoryClient.RemoveChatMemoriesAsync(this._promptOptions.MemoryIndexName, chatId, cancellationToken));

        // Create a task that represents the completion of all cleanupTasks
        Task aggregationTask = Task.WhenAll(cleanupTasks);
        try
        {
            // Await the completion of all tasks in parallel
            await aggregationTask;
        }
        catch (Exception ex)
        {
            // Handle any exceptions that occurred during the tasks
            if (aggregationTask?.Exception?.InnerExceptions != null && aggregationTask.Exception.InnerExceptions.Count != 0)
            {
                foreach (var innerEx in aggregationTask.Exception.InnerExceptions)
                {
                    this._logger.LogInformation("Failed to delete an entity of chat {0}: {1}", chatId, innerEx.Message);
                }

                throw aggregationTask.Exception;
            }

            throw new AggregateException($"Resource deletion failed for chat {chatId}.", ex);
        }
    }
}
