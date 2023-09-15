// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Services.MemoryMigration;

/// <summary>
/// Set of migration states/status for chat memory migration.
/// </summary>
/// <remarks>
/// Interlocked.CompareExchange doesn't work with enums.
/// </remarks>
public sealed class ChatMigrationStatus
{
    /// <summary>
    /// Represents state where no migration is required or in progress.
    /// </summary>
    public static ChatMigrationStatus None { get; } = new ChatMigrationStatus(nameof(None));

    /// <summary>
    /// Represents state where no migration is required.
    /// </summary>
    public static ChatMigrationStatus RequiresUpgrade { get; } = new ChatMigrationStatus(nameof(RequiresUpgrade));

    /// <summary>
    /// Represents state where no migration is in progress.
    /// </summary>
    public static ChatMigrationStatus Upgrading { get; } = new ChatMigrationStatus(nameof(Upgrading));

    /// <summary>
    /// The state label (no functional impact, but helps debugging).
    /// </summary>
    public string Label { get; }

    private ChatMigrationStatus(string label)
    {
        this.Label = label;
    }
}
