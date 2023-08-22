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
    /// Inject <see cref="ISemanticMemoryClient"/>.
    /// </summary>
    public static void AddSemanticMemoryServices(this WebApplicationBuilder builder)
    {
        ISemanticMemoryClient memory =
            new MemoryClientBuilder(builder.Services)
                .FromAppSettings()
                .Build();

        builder.Services.AddSingleton(memory);
    }
}
