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

namespace CopilotChat.WebApi.Services;

/// <summary>
/// $$$
/// </summary>
public class ChatMigrationMonitor : IChatMigrationMonitor
{
    internal const string MigrationKey = "migrate-00000000-0000-0000-0000-000000000000";

    private static ChatVersionStatus? _cachedStatus;
    private static bool? _hasCurrentIndex;

    private readonly ILogger<ChatMigrationMonitor> _logger;
    private readonly string _indexNameAllMemory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMigrationMonitor"/> class.
    /// </summary>
    public ChatMigrationMonitor(
        ILogger<ChatMigrationMonitor> logger,
        IOptions<PromptsOptions> promptOptions)
    {
        this._logger = logger;
        this._indexNameAllMemory = promptOptions.Value.MemoryIndexName;
    }

    /// <summary>
    /// $$$
    /// </summary>
    public async Task<ChatVersionStatus> GetCurrentStatusAsync(ISemanticTextMemory memory, CancellationToken cancelToken = default) // $$$ RETURN string / id ???
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
                case (ChatVersionStatus s) when (s == ChatVersionStatus.RequiresUpgrade || s == ChatVersionStatus.Upgrading):
                    _cachedStatus = await QueryStatusAsync().ConfigureAwait(false);
                    break;

                default: // ChatVersionStatus.None
                    break;
            }
        }

        return _cachedStatus ?? ChatVersionStatus.None;

        // Inline function to determine if the new "target" index already exists.
        // If not, we need to upgrade; otherwise, further inspection is required.
        async Task<ChatVersionStatus?> QueryCollectionAsync()
        {
            try
            {
                if (_hasCurrentIndex == null)
                {
                    // Cache "found" index state to reduce query count and avoid handling truth mutation.
                    var collections = await memory.GetCollectionsAsync(cancelToken).ConfigureAwait(false);

                    // Does the new "target" index already exist?
                    _hasCurrentIndex = collections.Any(c => c.Equals(this._indexNameAllMemory, StringComparison.OrdinalIgnoreCase));

                    if (!_hasCurrentIndex ?? false)
                    {
                        return ChatVersionStatus.RequiresUpgrade; // No index == update required
                    }
                }
            }
            catch (SKException exception)
            {
                this._logger.LogError(exception, "Unable to search collections");
            }

            return null; // Further inspection required
        }

        async Task<ChatVersionStatus> QueryStatusAsync()
        {
            if (_hasCurrentIndex ?? false)
            {
                try
                {
                    var result =
                        await memory.GetAsync(
                            this._indexNameAllMemory,
                            MigrationKey,
                            withEmbedding: false,
                            cancelToken).ConfigureAwait(false);

                    if (result != null)
                    {
                        var text = result.Metadata.Text; // $$$ MODEL: Status, Initiator, Timestamp

                        if (!string.IsNullOrWhiteSpace(text) && text.Equals("Done", StringComparison.OrdinalIgnoreCase)) // $$$ DONE Const
                        {
                            return ChatVersionStatus.None;
                        }

                        return ChatVersionStatus.Upgrading;
                    }
                }
                catch (SKException exception)
                {
                    this._logger.LogError(exception, "Unable to search collection {0}", this._indexNameAllMemory);
                }
            }

            return ChatVersionStatus.RequiresUpgrade;
        }
    }
}
