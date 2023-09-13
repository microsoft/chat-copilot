// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
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
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Memory;

namespace CopilotChat.WebApi.Controllers;

[ApiController]
public class BotController : ControllerBase
{
    private readonly ILogger<BotController> _logger;
    private readonly IMemoryStore? _memoryStore;
    private readonly ChatSessionRepository _chatRepository;
    private readonly ChatMessageRepository _chatMessageRepository;
    private readonly ChatParticipantRepository _chatParticipantRepository;
    private readonly BotEmbeddingConfig _embeddingConfig;
    private readonly BotSchemaOptions _botSchemaOptions;
    private readonly DocumentMemoryOptions _documentMemoryOptions;

    /// <summary>
    /// The constructor of BotController.
    /// </summary>
    /// <param name="memoryStore">Memory store.</param>
    /// <param name="chatRepository">The chat session repository.</param>
    /// <param name="chatMessageRepository">The chat message repository.</param>
    /// <param name="chatParticipantRepository">The chat participant repository.</param>
    /// <param name="botSchemaOptions">The bot schema options.</param>
    /// <param name="documentMemoryOptions">The document memory options.</param>
    /// <param name="logger">The logger.</param>
    public BotController(
        IMemoryStore memoryStore,
        ChatSessionRepository chatRepository,
        ChatMessageRepository chatMessageRepository,
        ChatParticipantRepository chatParticipantRepository,
        BotEmbeddingConfig embeddingConfig,
        IOptions<BotSchemaOptions> botSchemaOptions,
        IOptions<DocumentMemoryOptions> documentMemoryOptions,
        ILogger<BotController> logger)
    {
        this._memoryStore = memoryStore;
        this._logger = logger;
        this._chatRepository = chatRepository;
        this._chatMessageRepository = chatMessageRepository;
        this._chatParticipantRepository = chatParticipantRepository;
        this._embeddingConfig = embeddingConfig;
        this._botSchemaOptions = botSchemaOptions.Value;
        this._documentMemoryOptions = documentMemoryOptions.Value;
    }

    /// <summary>
    /// Download a bot.
    /// </summary>
    /// <param name="kernel">The Semantic Kernel instance.</param>
    /// <param name="chatId">The chat id to be downloaded.</param>
    /// <returns>The serialized Bot object of the chat id.</returns>
    [HttpGet]
    [ActionName("DownloadAsync")]
    [Route("bot/download/{chatId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<ActionResult<Bot?>> DownloadAsync(
        [FromServices] IKernel kernel,
        Guid chatId)
    {
        this._logger.LogDebug("Received call to download a bot");
        var memory = await this.CreateBotAsync(kernel: kernel, chatId: chatId);

        return this.Ok(memory);
    }

    /// <summary>
    /// Get memory from memory store and append the memory records to a given list.
    /// It will update the memory collection name in the new list if the newCollectionName is provided.
    /// </summary>
    /// <param name="kernel">The Semantic Kernel instance.</param>
    /// <param name="collectionName">The current collection name. Used to query the memory storage.</param>
    /// <param name="embeddings">The embeddings list where we will append the fetched memory records.</param>
    /// <param name="newCollectionName">
    /// The new collection name when appends to the embeddings list. Will use the old collection name if not provided.
    /// </param>
    private async Task GetMemoryRecordsAndAppendToEmbeddingsAsync(
        IKernel kernel,
        string collectionName,
        List<KeyValuePair<string, List<MemoryQueryResult>>> embeddings,
        string? newCollectionName = null)
    {
        List<MemoryQueryResult> collectionMemoryRecords;
        try
        {
            collectionMemoryRecords = await kernel.Memory.SearchAsync(
                collectionName,
                "*", // dummy query since we don't care about relevance. An empty string will cause exception.
                limit: int.MaxValue, // temp solution to get as much as record as a workaround.
                minRelevanceScore: -1, // no relevance required since the collection only has one entry
                withEmbeddings: true,
                cancellationToken: default
            ).ToListAsync();
        }
        catch (SKException connectorException)
        {
            // A store exception might be thrown if the collection does not exist, depending on the memory store connector.
            this._logger.LogError(connectorException,
                "Cannot search collection {0}",
                collectionName);
            collectionMemoryRecords = new();
        }

        embeddings.Add(new KeyValuePair<string, List<MemoryQueryResult>>(
            string.IsNullOrEmpty(newCollectionName) ? collectionName : newCollectionName,
            collectionMemoryRecords));
    }

    /// <summary>
    /// Prepare the bot information of a given chat.
    /// </summary>
    /// <param name="kernel">The semantic kernel object.</param>
    /// <param name="chatId">The chat id of the bot</param>
    /// <returns>A Bot object that represents the chat session.</returns>
    private async Task<Bot> CreateBotAsync(IKernel kernel, Guid chatId)
    {
        var chatIdString = chatId.ToString();
        var bot = new Bot
        {
            // get the bot schema version
            Schema = this._botSchemaOptions,

            // get the embedding configuration
            EmbeddingConfigurations = this._embeddingConfig,
        };

        // get the chat title
        ChatSession chat = await this._chatRepository.FindByIdAsync(chatIdString);
        bot.ChatTitle = chat.Title;

        // get the system description
        bot.SystemDescription = chat.SystemDescription;

        // get the chat history
        bot.ChatHistory = await this.GetAllChatMessagesAsync(chatIdString);

        // get the memory collections associated with this chat
        var chatCollections = (await kernel.Memory.GetCollectionsAsync())
            .Where(collection => collection.StartsWith(chatIdString, StringComparison.OrdinalIgnoreCase));

        foreach (var collection in chatCollections)
        {
            await this.GetMemoryRecordsAndAppendToEmbeddingsAsync(kernel: kernel, collectionName: collection, embeddings: bot.Embeddings);
        }

        // get the document memory collection names (global scope)
        await this.GetMemoryRecordsAndAppendToEmbeddingsAsync(
            kernel: kernel,
            collectionName: this._documentMemoryOptions.GlobalDocumentCollectionName,
            embeddings: bot.DocumentEmbeddings);

        // get the document memory collection names (user scope)
        await this.GetMemoryRecordsAndAppendToEmbeddingsAsync(
            kernel: kernel,
            collectionName: this._documentMemoryOptions.ChatDocumentCollectionNamePrefix + chatIdString,
            embeddings: bot.DocumentEmbeddings);

        return bot;
    }

    /// <summary>
    /// Get chat messages of a given chat id.
    /// </summary>
    /// <param name="chatId">The chat id</param>
    /// <returns>The list of chat messages in descending order of the timestamp</returns>
    private async Task<List<ChatMessage>> GetAllChatMessagesAsync(string chatId)
    {
        // TODO: [Issue #47] We might want to set limitation on the number of messages that are pulled from the storage.
        return (await this._chatMessageRepository.FindByChatIdAsync(chatId))
            .OrderByDescending(m => m.Timestamp).ToList();
    }
}
