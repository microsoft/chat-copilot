// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Memory;

namespace CopilotChat.WebApi.Services.MemoryMigration;

/// <summary>
/// Service implementation of <see cref=IChatMigrationMonitor""/>.
/// </summary>
public class ChatMigrationMonitor : IChatMigrationMonitor
{
    internal const string MigrationCompletionToken = "DONE";
    internal const string MigrationKey = "migrate-00000000-0000-0000-0000-000000000000";

    private static ChatMigrationStatus? _cachedStatus;
    private static bool? _hasCurrentIndex;

    private readonly ILogger<ChatMigrationMonitor> _logger;
    private readonly string _indexNameAllMemory;
    private readonly ISemanticTextMemory _memory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMigrationMonitor"/> class.
    /// </summary>
    public ChatMigrationMonitor(
        ILogger<ChatMigrationMonitor> logger,
        IOptions<PromptsOptions> promptOptions,
        SemanticKernelProvider provider)
    {
        this._logger = logger;
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
                await QueryCollectionAsync().ConfigureAwait(false),
                null);

            if (_cachedStatus == null)
            {
                // Attempt to determine migration status looking at index state.
                _cachedStatus = await QueryStatusAsync().ConfigureAwait(false);
            }
        }
        else
        {
            // Refresh status if we have a cached value for any state other than: ChatVersionStatus.None.
            switch (_cachedStatus)
            {
                case ChatMigrationStatus s when s == ChatMigrationStatus.RequiresUpgrade || s == ChatMigrationStatus.Upgrading:
                    _cachedStatus = await QueryStatusAsync().ConfigureAwait(false);
                    break;

                default: // ChatVersionStatus.None
                    break;
            }
        }

        return _cachedStatus ?? ChatMigrationStatus.None;

        // Inline function to determine if the new "target" index already exists.
        // If not, we need to upgrade; otherwise, further inspection is required.
        async Task<ChatMigrationStatus?> QueryCollectionAsync()
        {
            try
            {
                if (_hasCurrentIndex == null)
                {
                    // Cache "found" index state to reduce query count and avoid handling truth mutation.
                    var collections = await this._memory.GetCollectionsAsync(cancellationToken).ConfigureAwait(false);

                    // Does the new "target" index already exist?
                    _hasCurrentIndex = collections.Any(c => c.Equals(this._indexNameAllMemory, StringComparison.OrdinalIgnoreCase));

                    if (!_hasCurrentIndex ?? false)
                    {
                        return ChatMigrationStatus.RequiresUpgrade; // No index == update required
                    }
                }
            }
            catch (SKException exception)
            {
                this._logger.LogError(exception, "Unable to search collections");
            }

            return null; // Further inspection required
        }

        async Task<ChatMigrationStatus> QueryStatusAsync()
        {
            if (_hasCurrentIndex ?? false)
            {
                try
                {
                    var result =
                        await this._memory.GetAsync(
                            this._indexNameAllMemory,
                            MigrationKey,
                            withEmbedding: false,
                            cancellationToken).ConfigureAwait(false);

                    if (result != null)
                    {
                        var text = result.Metadata.Text;

                        if (!string.IsNullOrWhiteSpace(text) && text.Equals(MigrationCompletionToken, StringComparison.OrdinalIgnoreCase))
                        {
                            return ChatMigrationStatus.None;
                        }

                        return ChatMigrationStatus.Upgrading;
                    }
                }
                catch (SKException exception)
                {
                    this._logger.LogError(exception, "Unable to search collection {0}", this._indexNameAllMemory);
                }
            }

            return ChatMigrationStatus.RequiresUpgrade;
        }
    }
}
