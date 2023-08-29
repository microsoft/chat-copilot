// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory;

namespace CopilotChat.WebApi.Extensions;

/// <summary>
/// Extension methods for <see cref="ISemanticMemoryClient"/> and service registration.
/// </summary>
internal static class ISemanticMemoryClientExtensions
{
    private const string TagChatId = "chatid";
    public const string TagMemory = "memory";

    /// <summary>
    /// Inject <see cref="ISemanticMemoryClient"/>.
    /// </summary>
    public static void AddSemanticMemoryServices(this WebApplicationBuilder builder)
    {
        ISemanticMemoryClient memory =
            new MemoryClientBuilder(builder.Services)
                .WithoutDefaultHandlers()
                .FromAppSettings()
                .Build();

        builder.Services.AddSingleton(memory);
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

        filter.ByTag(TagChatId, chatId);

        if (!string.IsNullOrWhiteSpace(memoryName))
        {
            filter.ByTag(TagMemory, memoryName);
        }

        var searchResult =
            await memoryClient.SearchAsync(
                query,
                indexName,
                filter,
                cancelToken)
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

        uploadRequest.Tags.Add(TagChatId, chatId);
        uploadRequest.Tags.Add(TagMemory, memoryName);

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

        uploadRequest.Tags.Add(TagChatId, chatId);
        uploadRequest.Tags.Add(TagMemory, memoryName);

        await memoryClient.ImportDocumentAsync(uploadRequest, cancelToken);
    }
}
