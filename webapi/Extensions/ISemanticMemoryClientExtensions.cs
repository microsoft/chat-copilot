// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.Shared;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticMemory;

namespace CopilotChat.WebApi.Extensions;

/// <summary>
/// Extension methods for <see cref="ISemanticMemoryClient"/> and service registration.
/// </summary>
internal static class ISemanticMemoryClientExtensions
{
    /// <summary>
    /// Inject <see cref="ISemanticMemoryClient"/>.
    /// </summary>
    public static void AddSemanticMemoryServices(this WebApplicationBuilder appBuilder)
    {
        var serviceProvider = appBuilder.Services.BuildServiceProvider();

        var memoryConfig = serviceProvider.GetRequiredService<IOptions<SemanticMemoryConfig>>().Value;

        var ocrType = memoryConfig.ImageOcrType;
        var hasOcr = !string.IsNullOrWhiteSpace(ocrType) && !ocrType.Equals(MemoryConfiguration.NoneType, StringComparison.OrdinalIgnoreCase);

        var pipelineType = memoryConfig.DataIngestion.OrchestrationType;
        var isDistributed = pipelineType.Equals(MemoryConfiguration.OrchestrationTypeDistributed, StringComparison.OrdinalIgnoreCase);

        appBuilder.Services.AddSingleton(sp => new DocumentTypeProvider(hasOcr));

        var memoryBuilder = new MemoryClientBuilder(appBuilder.Services);

        if (isDistributed)
        {
            memoryBuilder.WithoutDefaultHandlers();
        }
        else
        {
            memoryBuilder.WithCustomOcr(appBuilder.Configuration);
        }

        ISemanticMemoryClient memory = memoryBuilder.FromAppSettings().Build();

        appBuilder.Services.AddSingleton(memory);
    }

    public static async Task<SearchResult> SearchMemoryAsync(
        this ISemanticMemoryClient memoryClient,
        string indexName,
        string query,
        float relevanceThreshold,
        string chatId,
        string? memoryName = null,
        CancellationToken cancelToken = default)
    {
        var filter =
            new MemoryFilter
            {
                MinRelevance = relevanceThreshold,
            };

        filter.ByTag(MemoryTags.TagChatId, chatId);

        if (!string.IsNullOrWhiteSpace(memoryName))
        {
            filter.ByTag(MemoryTags.TagMemory, memoryName);
        }

        var searchResult =
            await memoryClient.SearchAsync(
                query,
                index: indexName,
                filter: filter,
                cancellationToken: cancelToken)
            .ConfigureAwait(false);

        return searchResult;
    }

    public static async Task StoreDocumentAsync(
        this ISemanticMemoryClient memoryClient,
        string indexName,
        string documentId,
        string chatId,
        string memoryName,
        string fileName,
        Stream fileContent,
        CancellationToken cancelToken = default)
    {
        var uploadRequest =
            new DocumentUploadRequest
            {
                DocumentId = documentId,
                Files = new List<DocumentUploadRequest.UploadedFile> { new DocumentUploadRequest.UploadedFile(fileName, fileContent) },
                Index = indexName,
            };

        uploadRequest.Tags.Add(MemoryTags.TagChatId, chatId);
        uploadRequest.Tags.Add(MemoryTags.TagMemory, memoryName);

        await memoryClient.ImportDocumentAsync(uploadRequest, cancelToken);
    }

    public static async Task StoreMemoryAsync(
        this ISemanticMemoryClient memoryClient,
        string indexName,
        string chatId,
        string memoryName,
        string memory,
        CancellationToken cancelToken = default)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(memory);
        await writer.FlushAsync();
        stream.Position = 0;

        var id = Guid.NewGuid().ToString();
        var uploadRequest = new DocumentUploadRequest
        {
            DocumentId = id,
            Index = indexName,
            Files =
                new()
                {
                    // Document file name not relevant, but required.
                    new DocumentUploadRequest.UploadedFile("memory.txt", stream)
                },
        };

        uploadRequest.Tags.Add(MemoryTags.TagChatId, chatId);
        uploadRequest.Tags.Add(MemoryTags.TagMemory, memoryName);

        await memoryClient.ImportDocumentAsync(uploadRequest, cancelToken);
    }

    public static async Task RemoveChatMemoriesAsync(
        this ISemanticMemoryClient memoryClient,
        string indexName,
        string chatId,
        CancellationToken cancelToken = default)
    {
        var memories = await memoryClient.SearchMemoryAsync(indexName, "*", 0.0F, chatId, cancelToken: cancelToken);
        foreach (var memory in memories.Results)
        {
            await memoryClient.DeleteDocumentAsync(indexName, memory.Link, cancelToken);
        }
    }
}
