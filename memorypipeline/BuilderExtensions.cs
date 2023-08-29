// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory;

namespace CopilotChat.MemoryPipeline;

/// <summary>
/// Dependency injection for semantic-memory using configuration defined in appsettings.json
/// </summary>
internal static class BuilderExtensions
{
    private const string ConfigRoot = "SemanticMemory";

    public static WebApplicationBuilder AddMemoryServices(this WebApplicationBuilder builder)
    {
        ISemanticMemoryClient memory = new MemoryClientBuilder(builder.Services).FromAppSettings().Build();

        builder.Services.AddSingleton(memory);

        return builder;
    }
}
