// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Memory;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// $$$
/// </summary>
public sealed class ChatVersionStatus
{
    public static ChatVersionStatus None { get; } = new ChatVersionStatus(nameof(None));
    public static ChatVersionStatus RequiresUpgrade { get; } = new ChatVersionStatus(nameof(RequiresUpgrade));
    public static ChatVersionStatus Upgrading { get; } = new ChatVersionStatus(nameof(Upgrading));

    public string Label { get; }

    private ChatVersionStatus(string label)
    {
        this.Label = label;
    }
}

/// <summary>
/// $$$
/// </summary>
public interface IChatMigrationMonitor
{
    /// <summary>
    /// $$$
    /// </summary>
    Task<ChatVersionStatus> GetCurrentStatusAsync(ISemanticTextMemory memory, CancellationToken cancelToken = default);
}
