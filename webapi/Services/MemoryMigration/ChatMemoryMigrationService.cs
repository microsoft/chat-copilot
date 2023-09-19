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

namespace CopilotChat.WebApi.Services.MemoryMigration;

/// <summary>
/// Service implementation of <see cref="IChatMemoryMigrationService"/>.
/// </summary>
public class ChatMemoryMigrationService : IChatMemoryMigrationService
{
    private readonly ILogger<ChatMemoryMigrationService> _logger;
    private readonly ISemanticTextMemory _memory;
    private readonly ISemanticMemoryClient _memoryClient;
    private readonly ChatSessionRepository _chatSessionRepository;
    private readonly ChatMemorySourceRepository _memorySourceRepository;
    private readonly string _globalIndex;
    private readonly PromptsOptions _promptOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMigrationMonitor"/> class.
    /// </summary>
    public ChatMemoryMigrationService(
        ILogger<ChatMemoryMigrationService> logger,
        IOptions<DocumentMemoryOptions> documentMemoryOptions,
        IOptions<PromptsOptions> promptOptions,
        ISemanticMemoryClient memoryClient,
        ChatSessionRepository chatSessionRepository,
        ChatMemorySourceRepository memorySourceRepository,
        SemanticKernelProvider provider)
    {
        this._logger = logger;
        this._promptOptions = promptOptions.Value;
        this._memoryClient = memoryClient;
        this._chatSessionRepository = chatSessionRepository;
        this._memorySourceRepository = memorySourceRepository;
        this._globalIndex = documentMemoryOptions.Value.GlobalDocumentCollectionName;
        var kernel = provider.GetMigrationKernel();
        this._memory = kernel.Memory;
    }

    ///<inheritdoc/>
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await this.InternalMigrateAsync(cancellationToken);
        }
        catch (Exception exception) when (!exception.IsCriticalException())
        {
            this._logger.LogError(exception, "Error migrating chat memories");
        }
    }

    private async Task InternalMigrateAsync(CancellationToken cancellationToken = default)
    {
        var collectionNames = (await this._memory.GetCollectionsAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var tokenMemory = await GetTokenMemory(cancellationToken);
        if (tokenMemory != null)
        {
            //  Create memory token already exists
            return;
        }

        //  Create memory token
        var token = Guid.NewGuid().ToString();
        await SetTokenMemory(token, cancellationToken);

        await RemoveMemorySourcesAsync();

        bool needsZombie = true;
        // Extract and store memories, using the original id to avoid duplication should a retry be required.
        await foreach ((string chatId, string memoryName, string memoryId, string memoryText) in QueryMemoriesAsync())
        {
            await this._memoryClient.StoreMemoryAsync(this._promptOptions.MemoryIndexName, chatId, memoryName, memoryId, memoryText, cancellationToken);
            needsZombie = false;
        }

        // Store "Zombie" memory in order to create the index since zero writes have occurred.  Won't affect any chats.
        if (needsZombie)
        {
            await this._memoryClient.StoreMemoryAsync(this._promptOptions.MemoryIndexName, Guid.Empty.ToString(), "zombie", Guid.NewGuid().ToString(), "Initialized", cancellationToken);
        }

        await SetTokenMemory(ChatMigrationMonitor.MigrationCompletionToken, cancellationToken);

        // Inline function to extract all memories for a given chat and memory type.
        async IAsyncEnumerable<(string chatId, string memoryName, string memoryId, string memoryText)> QueryMemoriesAsync()
        {
            var chats = await this._chatSessionRepository.GetAllChatsAsync();
            foreach (var chat in chats)
            {
                foreach (var memoryType in this._promptOptions.MemoryMap.Keys)
                {
                    var indexName = $"{chat.Id}-{memoryType}";
                    if (collectionNames.Contains(indexName))
                    {
                        var memories = await this._memory.SearchAsync(indexName, "*", limit: 10000, minRelevanceScore: 0, withEmbeddings: false, cancellationToken).ToArrayAsync(cancellationToken);

                        foreach (var memory in memories)
                        {
                            yield return (chat.Id, memoryType, memory.Metadata.Id, memory.Metadata.Text);
                        }
                    }
                }
            }
        }

        // Inline function to read the token memory
        async Task<MemoryQueryResult?> GetTokenMemory(CancellationToken cancellationToken)
        {
            try
            {
                return await this._memory.GetAsync(this._globalIndex, ChatMigrationMonitor.MigrationKey, withEmbedding: false, cancellationToken);
            }
            catch (Exception ex) when (!ex.IsCriticalException())
            {
                return null;
            }
        }

        // Inline function to write the token memory
        async Task SetTokenMemory(string token, CancellationToken cancellationToken)
        {
            await this._memory.SaveInformationAsync(this._globalIndex, token, ChatMigrationMonitor.MigrationKey, description: null, additionalMetadata: null, cancellationToken);
        }

        async Task RemoveMemorySourcesAsync()
        {
            var documentMemories = await this._memorySourceRepository.GetAllAsync();

            await Task.WhenAll(documentMemories.Select(memory => this._memorySourceRepository.DeleteAsync(memory)));
        }
    }
}
