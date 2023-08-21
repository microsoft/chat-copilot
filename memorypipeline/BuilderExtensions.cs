// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Handlers;

namespace CopilotChat.MemoryPipeline;

/// <summary>
/// Dependency injection for semantic-memory using configuration defined in appsettings.json
/// </summary>
internal static class BuilderExtensions
{
    private const string ConfigRoot = "SemanticMemory";

    public static WebApplicationBuilder AddMemoryServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHandlerAsHostedService<TextExtractionHandler>("extract");
        builder.Services.AddHandlerAsHostedService<TextPartitioningHandler>("partition");
        builder.Services.AddHandlerAsHostedService<GenerateEmbeddingsHandler>("gen_embeddings");
        builder.Services.AddHandlerAsHostedService<SaveEmbeddingsHandler>("save_embeddings");

        ISemanticMemoryClient memory = new MemoryClientBuilder(builder.Services).FromAppSettings().Build();

        builder.Services.AddSingleton(memory);

        return builder;
    }
}
