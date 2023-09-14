// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Skills.ChatSkills;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Memory;

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
    /// <param name="memoryName">Name of the memory type.</param>
    [HttpGet]
    [Route("chatMemory/{chatId:guid}/{memoryName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> GetSemanticMemoriesAsync(
        [FromServices] ISemanticTextMemory semanticTextMemory,
        [FromRoute] string chatId,
        [FromRoute] string memoryName)
    {
        // Sanitize the log input by removing new line characters.
        // https://github.com/microsoft/chat-copilot/security/code-scanning/1
        var sanitizedChatId = chatId.Replace(Environment.NewLine, string.Empty, StringComparison.Ordinal);
        var sanitizedMemoryName = memoryName.Replace(Environment.NewLine, string.Empty, StringComparison.Ordinal);

        // Make sure the chat session exists.
        if (!await this._chatSessionRepository.TryFindByIdAsync(chatId))
        {
            this._logger.LogWarning("Chat session: {0} does not exist.", sanitizedChatId);
            return this.BadRequest($"Chat session: {sanitizedChatId} does not exist.");
        }

        // Make sure the memory name is valid.
        if (!this.ValidateMemoryName(sanitizedMemoryName))
        {
            this._logger.LogWarning("Memory name: {0} is invalid.", sanitizedMemoryName);
            return this.BadRequest($"Memory name: {sanitizedMemoryName} is invalid.");
        }

        // Gather the requested semantic memory.
        // ISemanticTextMemory doesn't support retrieving all memories.
        // Will use a dummy query since we don't care about relevance. An empty string will cause exception.
        // minRelevanceScore is set to 0.0 to return all memories.
        List<string> memories = new();
        string memoryCollectionName = SemanticChatMemoryExtractor.MemoryCollectionName(sanitizedChatId, sanitizedMemoryName);
#pragma warning disable CA1031 // Each connector may throw different exception type
        try
        {
            var results = semanticTextMemory.SearchAsync(
                memoryCollectionName,
                "abc",
                limit: 100,
                minRelevanceScore: 0.0);
            await foreach (var memory in results)
            {
                memories.Add(memory.Metadata.Text);
            }
        }
        catch (Exception connectorException)
        {
            // A store exception might be thrown if the collection does not exist, depending on the memory store connector.
            var sanitizedMemoryCollectionName = memoryCollectionName.Replace(Environment.NewLine, string.Empty, StringComparison.Ordinal);
            this._logger.LogError(connectorException, "Cannot search collection {0}", sanitizedMemoryCollectionName);
        }
#pragma warning restore CA1031 // Each connector may throw different exception type

        return this.Ok(memories);
    }

    #region Private

    /// <summary>
    /// Validates the memory name.
    /// </summary>
    /// <param name="memoryName">Name of the memory requested.</param>
    /// <returns>True if the memory name is valid.</returns>
    private bool ValidateMemoryName(string memoryName)
    {
        return this._promptOptions.MemoryMap.ContainsKey(memoryName);
    }

    # endregion
}
