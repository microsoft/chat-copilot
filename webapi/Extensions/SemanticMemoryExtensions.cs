// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Core.AppBuilders;

namespace CopilotChat.WebApi.Extensions;

/// <summary>
/// Extension methods for registering Semantic Memory related services.
/// </summary>
internal static class SemanticMemoryExtensions
{
    /// <summary>
    /// Add Semantic Memory services
    /// </summary>
    /// <remarks>
    /// Forced to conform with the current state of semantic-memory.
    /// </remarks>
    public static void AddSemanticMemoryServices(this WebApplicationBuilder builder)
    {
        ISemanticMemoryClient memory =
            new MemoryClientBuilder(builder.Services)
                .FromAppSettings()
                .Build();

        builder.Services.AddSingleton(memory);
    }
}
