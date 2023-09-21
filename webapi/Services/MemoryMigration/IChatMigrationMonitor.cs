// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

namespace CopilotChat.WebApi.Services.MemoryMigration;

/// <summary>
/// Contract for monitoring the status of chat memory migration.
/// </summary>
public interface IChatMigrationMonitor
{
    /// <summary>
    /// Inspects the current state of affairs to determine the chat migration status.
    /// </summary>
    Task<ChatMigrationStatus> GetCurrentStatusAsync(CancellationToken cancellationToken = default);
}
