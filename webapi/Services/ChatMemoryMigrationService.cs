// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Extensions;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticMemory;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// $$$
/// </summary>
public class ChatMemoryMigrationService : IChatMemoryMigrationService
{
    private readonly ILogger<ChatMemoryMigrationService> _logger;
    private readonly ISemanticMemoryClient _memoryClient;
    private readonly ChatSessionRepository _chatSessionRepository;
    private readonly PromptsOptions _promptOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMigrationMonitor"/> class.
    /// </summary>
    public ChatMemoryMigrationService(
        ILogger<ChatMemoryMigrationService> logger,
        IOptions<DocumentMemoryOptions> documentMemoryOptions,
        IOptions<PromptsOptions> promptOptions,
        ISemanticMemoryClient memoryClient,
        ChatSessionRepository chatSessionRepository)
    {
        this._logger = logger;
        this._promptOptions = promptOptions.Value;
        this._memoryClient = memoryClient;
        this._chatSessionRepository = chatSessionRepository;
    }

    /// <summary>
    /// Migrates all non-document memory to the semantic-memory index.
    /// </summary>
    public async Task MigrateAsync(ISemanticTextMemory memory, CancellationToken cancelToken = default)
    {
        var shouldMigrate = false;

        var tokenMemory = await GetTokenMemory(cancelToken).ConfigureAwait(false);
        if (tokenMemory == null)
        {
            //  Create token memory
            var token = Guid.NewGuid().ToString();
            await SetTokenMemory(token, cancelToken).ConfigureAwait(false);
            // Allow writes that are racing time to land
            await Task.Delay(TimeSpan.FromSeconds(5), cancelToken).ConfigureAwait(false);
            // Retrieve token memory
            tokenMemory = await GetTokenMemory(cancelToken).ConfigureAwait(false);
            // Set migrate flag if token matches
            shouldMigrate = tokenMemory != null && tokenMemory.Metadata.Text.Equals(token, StringComparison.OrdinalIgnoreCase);
        }

        if (!shouldMigrate)
        {
            return;
        }

        // Extract and store memories, using the original id to avoid duplication should a retry be required.
        await foreach ((string chatId, string memoryName, string memoryId, string memoryText) in QueryMemoriesAsync())
        {
            await this._memoryClient.StoreMemoryAsync(this._promptOptions.MemoryIndexName, chatId, memoryName, memoryId, memoryText, cancelToken);
        }

        await SetTokenMemory("Done", cancelToken).ConfigureAwait(false); // $$$ DONE Const

        // Inline function to extract all memories for a given chat and memory type.
        async IAsyncEnumerable<(string chatId, string memoryName, string memoryId, string memoryText)> QueryMemoriesAsync()
        {
            var chats = await this._chatSessionRepository.GetAllChatsAsync().ConfigureAwait(false);
            foreach (var chat in chats)
            {
                foreach (var memoryType in this._promptOptions.MemoryMap.Keys)
                {
                    var indexName = $"{chat.Id}-{memoryType}";
                    var memories = await memory.SearchAsync(indexName, "*", limit: int.MaxValue, minRelevanceScore: -1, withEmbeddings: false, cancelToken).ToArrayAsync(cancelToken);

                    foreach (var memory in memories)
                    {
                        yield return (chat.Id, memoryType, memory.Metadata.Id, memory.Metadata.Text);
                    }
                }
            }
        }

        // Inline function to read the token memory
        async Task<MemoryQueryResult?> GetTokenMemory(CancellationToken cancelToken)
        {
            return await memory.GetAsync(this._promptOptions.MemoryIndexName, ChatMigrationMonitor.MigrationKey, withEmbedding: false, cancelToken).ConfigureAwait(false);
        }

        // Inline function to write the token memory
        async Task SetTokenMemory(string token, CancellationToken cancelToken)
        {
            await memory.SaveInformationAsync(this._promptOptions.MemoryIndexName, token, ChatMigrationMonitor.MigrationKey, description: null, additionalMetadata: null, cancelToken).ConfigureAwait(false);
        }
    }
}
