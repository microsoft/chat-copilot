// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Memory;

namespace CopilotChat.WebApi.Services.MemoryMigration;

/// <summary>
/// Service implementation of <see cref=IChatMigrationMonitor""/>.
/// </summary>
/// <remarks>
/// Migration is fundamentally determined by presence of the new consolidated index.
/// That is, if the new index exists then migration was considered to have occurred.
/// A tracking record is created in the historical global-document index: <see cref="DocumentMemoryOptions.GlobalDocumentCollectionName"/>
/// to managed race condition during the migration process (having migration triggered a second time while in progress).
/// In the event that somehow two migration processes are initiated in parallel, no duplication will result...only extraneous processing.
/// If the desire exists to reset/re-execute migration, simply delete the new index.
/// </remarks>
public class ChatMigrationMonitor : IChatMigrationMonitor
{
    internal const string MigrationCompletionToken = "DONE";
    internal const string MigrationKey = "migrate-00000000-0000-0000-0000-000000000000";

    private static ChatMigrationStatus? _cachedStatus;
    private static bool? _hasCurrentIndex;

    private readonly ILogger<ChatMigrationMonitor> _logger;
    private readonly string _indexNameGlobalDocs;
    private readonly string _indexNameAllMemory;
    private readonly ISemanticTextMemory _memory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMigrationMonitor"/> class.
    /// </summary>
    public ChatMigrationMonitor(
        ILogger<ChatMigrationMonitor> logger,
        IOptions<DocumentMemoryOptions> docOptions,
        IOptions<PromptsOptions> promptOptions,
        SemanticKernelProvider provider)
    {
        this._logger = logger;
        this._indexNameGlobalDocs = docOptions.Value.GlobalDocumentCollectionName;
        this._indexNameAllMemory = promptOptions.Value.MemoryIndexName;
        var kernel = provider.GetMigrationKernel();
        this._memory = kernel.Memory;
    }

    /// <inheritdoc/>
    public async Task<ChatMigrationStatus> GetCurrentStatusAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedStatus == null)
        {
            // Attempt to determine migration status looking at index existence. (Once)
            Interlocked.CompareExchange(
                ref _cachedStatus,
                await QueryCollectionAsync(),
                null);

            if (_cachedStatus == null)
            {
                // Attempt to determine migration status looking at index state.
                _cachedStatus = await QueryStatusAsync();
            }
        }
        else
        {
            // Refresh status if we have a cached value for any state other than: ChatVersionStatus.None.
            switch (_cachedStatus)
            {
                case ChatMigrationStatus s when s != ChatMigrationStatus.None:
                    _cachedStatus = await QueryStatusAsync();
                    break;

                default: // ChatVersionStatus.None
                    break;
            }
        }

        return _cachedStatus ?? ChatMigrationStatus.None;

        // Reports and caches migration state as either: None or null depending on existence of the target index.
        async Task<ChatMigrationStatus?> QueryCollectionAsync()
        {
            if (_hasCurrentIndex == null)
            {
                try
                {
                    // Cache "found" index state to reduce query count and avoid handling truth mutation.
                    var collections = await this._memory.GetCollectionsAsync(cancellationToken);

                    // Does the new "target" index already exist?
                    _hasCurrentIndex = collections.Any(c => c.Equals(this._indexNameAllMemory, StringComparison.OrdinalIgnoreCase));

                    return (_hasCurrentIndex ?? false) ? ChatMigrationStatus.None : null;
                }
                catch (Exception exception) when (!exception.IsCriticalException())
                {
                    this._logger.LogError(exception, "Unable to search collections");
                }
            }

            return (_hasCurrentIndex ?? false) ? ChatMigrationStatus.None : null;
        }

        // Note: Only called once determined that target index does not exist.
        async Task<ChatMigrationStatus> QueryStatusAsync()
        {
            try
            {
                var result =
                    await this._memory.GetAsync(
                        this._indexNameGlobalDocs,
                        MigrationKey,
                        withEmbedding: false,
                        cancellationToken);

                if (result == null)
                {
                    // No migration token
                    return ChatMigrationStatus.RequiresUpgrade;
                }

                var isDone = MigrationCompletionToken.Equals(result.Metadata.Text, StringComparison.OrdinalIgnoreCase);

                return isDone ? ChatMigrationStatus.None : ChatMigrationStatus.Upgrading;
            }
            catch (Exception exception) when (!exception.IsCriticalException())
            {
                this._logger.LogWarning("Failure searching collections: {0}\n{1}", this._indexNameGlobalDocs, exception.Message);
                return ChatMigrationStatus.RequiresUpgrade;
            }
        }
    }
}
