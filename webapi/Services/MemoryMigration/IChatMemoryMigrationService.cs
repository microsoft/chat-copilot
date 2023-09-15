// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

namespace CopilotChat.WebApi.Services.MemoryMigration;

/// <summary>
/// Defines contract for migrating chat memory.
/// </summary>
public interface IChatMemoryMigrationService
{
    /// <summary>
    /// Migrates all non-document memory to the semantic-memory index.
    /// Subsequent/redunant migration is non-destructive/no-impact to migrated index.
    /// </summary>
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
