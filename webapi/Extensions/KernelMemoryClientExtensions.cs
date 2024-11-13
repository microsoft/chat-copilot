// Copyright (c) Microsoft. All rights reserved.

using CopilotChat.Shared;
using CopilotChat.Shared.Ocr.Tesseract;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Services;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DataFormats.AzureAIDocIntel;

namespace CopilotChat.WebApi.Extensions;

/// <summary>
/// Extension methods for <see cref="IKernelMemory"/> and service registration.
/// </summary>
internal static class KernelMemoryClientExtensions
{
    private static readonly List<string> s_pipelineSteps = new() { "extract", "partition", "gen_embeddings", "save_records" };

    /// <summary>
    /// Inject <see cref="IKernelMemory"/>.
    /// </summary>
    public static void AddKernelMemoryServices(this WebApplicationBuilder appBuilder)
    {
        var serviceProvider = appBuilder.Services.BuildServiceProvider();

        var memoryConfig = serviceProvider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;

        var ocrType = memoryConfig.DataIngestion.ImageOcrType;
        var hasOcr = !string.IsNullOrWhiteSpace(ocrType) && !ocrType.Equals(MemoryConfiguration.NoneType, StringComparison.OrdinalIgnoreCase);

        var pipelineType = memoryConfig.DataIngestion.OrchestrationType;
        var isDistributed = pipelineType.Equals(MemoryConfiguration.OrchestrationTypeDistributed, StringComparison.OrdinalIgnoreCase);

        appBuilder.Services.AddSingleton(sp => new DocumentTypeProvider(hasOcr));

        var memoryBuilder = new KernelMemoryBuilder(appBuilder.Services);

        if (isDistributed)
        {
            memoryBuilder.WithoutDefaultHandlers();
        }
        else
        {
            if (hasOcr)
            {
                // Image OCR
                switch (ocrType)
                {
                    case string x when x.Equals("AzureAIDocIntel", StringComparison.OrdinalIgnoreCase):
                    {
                        AzureAIDocIntelConfig? cfg = appBuilder.Configuration
                            .GetSection($"{MemoryConfiguration.KernelMemorySection}:{MemoryConfiguration.ServicesSection}:AzureAIDocIntel")
                            .Get<AzureAIDocIntelConfig>() ?? throw new ConfigurationException("Missing Azure AI Document Intelligence configuration");
                        memoryBuilder.Services.AddSingleton(cfg);
                        memoryBuilder.WithCustomImageOcr<AzureAIDocIntelEngine>();
                        break;
                    }

                    case string x when x.Equals("Tesseract", StringComparison.OrdinalIgnoreCase):
                    {
                        TesseractConfig? cfg = appBuilder.Configuration
                            .GetSection($"{MemoryConfiguration.KernelMemorySection}:{MemoryConfiguration.ServicesSection}:Tesseract")
                            .Get<TesseractConfig>() ?? throw new ConfigurationException("Missing Tesseract configuration");
                        memoryBuilder.Services.AddSingleton(cfg);
                        memoryBuilder.WithCustomImageOcr<TesseractOcrEngine>();
                        break;
                    }
                }
            }
        }

        IKernelMemory memory = memoryBuilder.FromMemoryConfiguration(
            memoryConfig,
            appBuilder.Configuration
        ).Build();

        appBuilder.Services.AddSingleton(memory);
    }

    public static Task<SearchResult> SearchMemoryAsync(
        this IKernelMemory memoryClient,
        string indexName,
        string query,
        float relevanceThreshold,
        string chatId,
        string? memoryName = null,
        CancellationToken cancellationToken = default)
    {
        return memoryClient.SearchMemoryAsync(indexName, query, relevanceThreshold, resultCount: -1, chatId, memoryName, cancellationToken);
    }

    public static async Task<SearchResult> SearchMemoryAsync(
        this IKernelMemory memoryClient,
        string indexName,
        string query,
        float relevanceThreshold,
        int resultCount,
        string chatId,
        string? memoryName = null,
        CancellationToken cancellationToken = default)
    {
        var filter = new MemoryFilter();

        filter.ByTag(MemoryTags.TagChatId, chatId);

        if (!string.IsNullOrWhiteSpace(memoryName))
        {
            filter.ByTag(MemoryTags.TagMemory, memoryName);
        }

        var searchResult =
            await memoryClient.SearchAsync(
                query,
                indexName,
                filter,
                null,
                relevanceThreshold, // minRelevance param
                resultCount,
                cancellationToken: cancellationToken);

        return searchResult;
    }

    public static async Task StoreDocumentAsync(
        this IKernelMemory memoryClient,
        string indexName,
        string documentId,
        string chatId,
        string memoryName,
        string fileName,
        Stream fileContent,
        CancellationToken cancellationToken = default)
    {
        var uploadRequest =
            new DocumentUploadRequest
            {
                DocumentId = documentId,
                Files = new List<DocumentUploadRequest.UploadedFile> { new(fileName, fileContent) },
                Index = indexName,
                Steps = s_pipelineSteps,
            };

        uploadRequest.Tags.Add(MemoryTags.TagChatId, chatId);
        uploadRequest.Tags.Add(MemoryTags.TagMemory, memoryName);

        await memoryClient.ImportDocumentAsync(uploadRequest, cancellationToken: cancellationToken);
    }

    public static Task StoreMemoryAsync(
        this IKernelMemory memoryClient,
        string indexName,
        string chatId,
        string memoryName,
        string memory,
        CancellationToken cancellationToken = default)
    {
        return memoryClient.StoreMemoryAsync(indexName, chatId, memoryName, memoryId: Guid.NewGuid().ToString(), memory, cancellationToken);
    }

    public static async Task StoreMemoryAsync(
        this IKernelMemory memoryClient,
        string indexName,
        string chatId,
        string memoryName,
        string memoryId,
        string memory,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(memory);
        await writer.FlushAsync(cancellationToken);
        stream.Position = 0;

        var uploadRequest = new DocumentUploadRequest
        {
            DocumentId = memoryId,
            Index = indexName,
            Files =
                new()
                {
                    // Document file name not relevant, but required.
                    new DocumentUploadRequest.UploadedFile("memory.txt", stream)
                },
            Steps = s_pipelineSteps,
        };

        uploadRequest.Tags.Add(MemoryTags.TagChatId, chatId);
        uploadRequest.Tags.Add(MemoryTags.TagMemory, memoryName);

        await memoryClient.ImportDocumentAsync(uploadRequest, cancellationToken: cancellationToken);
    }

    public static async Task RemoveChatMemoriesAsync(
        this IKernelMemory memoryClient,
        string indexName,
        string chatId,
        CancellationToken cancellationToken = default)
    {
        var memories = await memoryClient.SearchMemoryAsync(indexName, "*", 0.0F, chatId, cancellationToken: cancellationToken);
        var documentIds = memories.Results.Select(memory => memory.DocumentId).Distinct().ToArray();
        var tasks = documentIds.Select(documentId => memoryClient.DeleteDocumentAsync(documentId, indexName, cancellationToken)).ToArray();

        Task.WaitAll(tasks, cancellationToken);
    }
}
