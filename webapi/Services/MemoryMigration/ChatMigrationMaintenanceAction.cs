// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CopilotChat.WebApi.Services.MemoryMigration;

/// <summary>
/// Middleware action to handle memory migration maintenance.
/// </summary>
public class ChatMigrationMaintenanceAction : IMaintenanceAction
{
    private readonly IChatMigrationMonitor _migrationMonitor;
    private readonly IChatMemoryMigrationService _migrationService;
    private readonly ILogger<ChatMigrationMaintenanceAction> _logger;

    public ChatMigrationMaintenanceAction(
        IChatMigrationMonitor migrationMonitor,
        IChatMemoryMigrationService migrationService,
        ILogger<ChatMigrationMaintenanceAction> logger)

    {
        this._migrationMonitor = migrationMonitor;
        this._migrationService = migrationService;
        this._logger = logger;
    }

    public async Task<bool> InvokeAsync(CancellationToken cancellation = default)
    {
        var migrationStatus = await this._migrationMonitor.GetCurrentStatusAsync(cancellation).ConfigureAwait(false);

        switch (migrationStatus)
        {
            case ChatMigrationStatus s when (s == ChatMigrationStatus.RequiresUpgrade):
                try
                {
                    // Migrate all chats to single index (in background)
                    var task = this._migrationService.MigrateAsync(cancellation);
                }
                catch (Exception ex) when (!ex.IsCriticalException())
                {
                    this._logger.LogError(ex, "Error migrating chat memories");
                }
                return true; // In maintenance

            case ChatMigrationStatus s when (s == ChatMigrationStatus.Upgrading):
                return true; // In maintenance

            case ChatMigrationStatus s when (s == ChatMigrationStatus.None):
            default:
                return false; // No maintenance
        }
    }
}
