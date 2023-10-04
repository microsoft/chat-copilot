// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Extensions;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticMemory;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// Controller for retrieving semantic memory data of chat sessions.
/// </summary>
[ApiController]
public class ChatMemoryController : ControllerBase
{
    private readonly ILogger<ChatMemoryController> _logger;

    private readonly PromptsOptions _promptOptions;

    private readonly ChatSessionRepository _chatSessionRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMemoryController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="promptsOptions">The prompts options.</param>
    /// <param name="chatSessionRepository">The chat session repository.</param>
    public ChatMemoryController(
        ILogger<ChatMemoryController> logger,
        IOptions<PromptsOptions> promptsOptions,
        ChatSessionRepository chatSessionRepository)
    {
        this._logger = logger;
        this._promptOptions = promptsOptions.Value;
        this._chatSessionRepository = chatSessionRepository;
    }

    /// <summary>
    /// Gets the semantic memory for the chat session.
    /// </summary>
    /// <param name="semanticTextMemory">The semantic text memory instance.</param>
    /// <param name="chatId">The chat id.</param>
    /// <param name="type">Type of memory. Must map to a member of <see cref="SemanticMemoryType"/>.</param>
    [HttpGet]
    [Route("chats/{chatId:guid}/memories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> GetSemanticMemoriesAsync(
        [FromServices] ISemanticMemoryClient memoryClient,
        [FromRoute] string chatId,
        [FromQuery] string type)
    {
        // Sanitize the log input by removing new line characters.
        // https://github.com/microsoft/chat-copilot/security/code-scanning/1
        var sanitizedChatId = GetSanitizedParameter(chatId);
        var sanitizedMemoryType = GetSanitizedParameter(type);

        // Map the requested memoryType to the memory store container name
        if (!this._promptOptions.TryGetMemoryContainerName(type, out string memoryContainerName))
        {
            this._logger.LogWarning("Memory type: {0} is invalid.", sanitizedMemoryType);
            return this.BadRequest($"Memory type: {sanitizedMemoryType} is invalid.");
        }

        // Make sure the chat session exists.
        if (!await this._chatSessionRepository.TryFindByIdAsync(chatId))
        {
            this._logger.LogWarning("Chat session: {0} does not exist.", sanitizedChatId);
            return this.BadRequest($"Chat session: {sanitizedChatId} does not exist.");
        }

        // Gather the requested semantic memory.
        // Will use a dummy query since we don't care about relevance.
        // minRelevanceScore is set to 0.0 to return all memories.
        List<string> memories = new();
        try
        {
            // Search if there is already a memory item that has a high similarity score with the new item.
            var filter = new MemoryFilter();
            filter.ByTag("chatid", chatId);
            filter.ByTag("memory", memoryContainerName);
            filter.MinRelevance = 0;

            var searchResult =
                await memoryClient.SearchMemoryAsync(
                    this._promptOptions.MemoryIndexName,
                    "*",
                    relevanceThreshold: 0,
                    resultCount: 1,
                    chatId,
                    memoryContainerName);

            foreach (var memory in searchResult.Results.SelectMany(c => c.Partitions))
            {
                memories.Add(memory.Text);
            }
        }
        catch (Exception connectorException) when (!connectorException.IsCriticalException())
        {
            // A store exception might be thrown if the collection does not exist, depending on the memory store connector.
            this._logger.LogError(connectorException, "Cannot search collection {0}", memoryContainerName);
        }

        return this.Ok(memories);
    }

    #region Private

    private static string GetSanitizedParameter(string parameterValue)
    {
        return parameterValue.Replace(Environment.NewLine, string.Empty, StringComparison.Ordinal);
    }

    # endregion
}
