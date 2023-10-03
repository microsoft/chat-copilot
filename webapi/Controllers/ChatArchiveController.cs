// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Extensions;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticMemory;

namespace CopilotChat.WebApi.Controllers;

[ApiController]
public class ChatArchiveController : ControllerBase
{
    private readonly ILogger<ChatArchiveController> _logger;
    private readonly ISemanticMemoryClient _memoryClient;
    private readonly ChatSessionRepository _chatRepository;
    private readonly ChatMessageRepository _chatMessageRepository;
    private readonly ChatParticipantRepository _chatParticipantRepository;
    private readonly ChatArchiveEmbeddingConfig _embeddingConfig;
    private readonly PromptsOptions _promptOptions;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="memoryClient">Memory client.</param>
    /// <param name="chatRepository">The chat session repository.</param>
    /// <param name="chatMessageRepository">The chat message repository.</param>
    /// <param name="chatParticipantRepository">The chat participant repository.</param>
    /// <param name="promptOptions">The document memory options.</param>
    /// <param name="logger">The logger.</param>
    public ChatArchiveController(
        ISemanticMemoryClient memoryClient,
        ChatSessionRepository chatRepository,
        ChatMessageRepository chatMessageRepository,
        ChatParticipantRepository chatParticipantRepository,
        ChatArchiveEmbeddingConfig embeddingConfig,
        IOptions<PromptsOptions> promptOptions,
        ILogger<ChatArchiveController> logger)
    {
        this._memoryClient = memoryClient;
        this._logger = logger;
        this._chatRepository = chatRepository;
        this._chatMessageRepository = chatMessageRepository;
        this._chatParticipantRepository = chatParticipantRepository;
        this._embeddingConfig = embeddingConfig;
        this._promptOptions = promptOptions.Value;
    }

    /// <summary>
    /// Download a chat archive.
    /// </summary>
    /// <param name="chatId">The ID of chat to be downloaded.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The serialized chat archive object of the chat id.</returns>
    [HttpGet]
    [Route("chats/{chatId:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<ActionResult<ChatArchive?>> DownloadAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        this._logger.LogDebug("Received call to download a chat archive");

        var chatArchive = await this.CreateChatArchiveAsync(chatId, cancellationToken);

        return this.Ok(chatArchive);
    }

    /// <summary>
    /// Prepare a chat archive.
    /// </summary>
    /// <param name="chatId">The chat id of the chat archive</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A ChatArchive object that represents the chat session.</returns>
    private async Task<ChatArchive> CreateChatArchiveAsync(Guid chatId, CancellationToken cancellationToken)
    {
        var chatIdString = chatId.ToString();
        var chatArchive = new ChatArchive
        {
            // Get embedding configuration
            EmbeddingConfigurations = this._embeddingConfig,
        };

        // get the chat title
        ChatSession chat = await this._chatRepository.FindByIdAsync(chatIdString);
        chatArchive.ChatTitle = chat.Title;

        // get the system description
        chatArchive.SystemDescription = chat.SystemDescription;

        // get the chat history
        chatArchive.ChatHistory = await this.GetAllChatMessagesAsync(chatIdString);

        foreach (var memory in this._promptOptions.MemoryMap.Keys)
        {
            chatArchive.Embeddings.Add(
                memory,
                await this.GetMemoryRecordsAndAppendToEmbeddingsAsync(chatIdString, memory, cancellationToken));
        }

        // get the document memory collection names (global scope)
        chatArchive.DocumentEmbeddings.Add(
            "GlobalDocuments",
            await this.GetMemoryRecordsAndAppendToEmbeddingsAsync(
                Guid.Empty.ToString(),
                this._promptOptions.DocumentMemoryName,
                cancellationToken));

        // get the document memory collection names (user scope)
        chatArchive.DocumentEmbeddings.Add(
            "ChatDocuments",
            await this.GetMemoryRecordsAndAppendToEmbeddingsAsync(
                chatIdString,
                this._promptOptions.DocumentMemoryName,
                cancellationToken));

        return chatArchive;
    }

    /// <summary>
    /// Get memory from memory store and append the memory records to a given list.
    /// It will update the memory collection name in the new list if the newCollectionName is provided.
    /// </summary>
    /// <param name="memoryName">The current collection name. Used to query the memory storage.</param>
    /// <param name="embeddings">The embeddings list where we will append the fetched memory records.</param>
    /// <param name="newCollectionName">
    /// The new collection name when appends to the embeddings list. Will use the old collection name if not provided.
    /// </param>
    private async Task<List<Citation>> GetMemoryRecordsAndAppendToEmbeddingsAsync(
        string chatId,
        string memoryName,
        CancellationToken cancellationToken)
    {
        List<Citation> collectionMemoryRecords;
        try
        {
            var result = await this._memoryClient.SearchMemoryAsync(
                this._promptOptions.MemoryIndexName,
                query: "*", // dummy query since we don't care about relevance. An empty string will cause exception.
                relevanceThreshold: -1, // no relevance required since the collection only has one entry
                chatId,
                memoryName,
                cancellationToken);

            collectionMemoryRecords = result.Results;
        }
        catch (Exception connectorException) when (!connectorException.IsCriticalException())
        {
            // A store exception might be thrown if the collection does not exist, depending on the memory store connector.
            this._logger.LogError(connectorException,
                "Cannot search collection {0}",
                memoryName);
            collectionMemoryRecords = new();
        }

        return collectionMemoryRecords;
    }

    /// <summary>
    /// Get chat messages of a given chat id.
    /// </summary>
    /// <param name="chatId">The chat id</param>
    /// <returns>The list of chat messages in descending order of the timestamp</returns>
    private async Task<List<ChatMessage>> GetAllChatMessagesAsync(string chatId)
    {
        return (await this._chatMessageRepository.FindByChatIdAsync(chatId))
            .OrderByDescending(m => m.Timestamp).ToList();
    }
}
