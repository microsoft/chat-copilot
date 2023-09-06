// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Memory;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// $$$
/// </summary>
public interface IChatMemoryMigrationService
{
    /// <summary>
    /// $$$
    /// </summary>
    Task MigrateAsync(ISemanticTextMemory memory, CancellationToken cancelToken = default);
}
