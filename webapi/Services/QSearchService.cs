// Copyright (c) Quartech. All rights reserved.

using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Plugins.Chat.Ext;
using CopilotChat.WebApi.Storage;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// The implementation class for search service.
/// </summary>
public class QSearchService : IQSearchService
{
    private readonly HttpClient _httpClient;
    private readonly HttpClientHandler? _httpClientHandler;
    private readonly SpecializationRepository _specializationRepository;
    private QAzureOpenAIChatExtension _qAzureOpenAIChatExtension;

    public QSearchService(
        QAzureOpenAIChatOptions qAzureOpenAIChatOptions,
        SpecializationRepository specializationSourceRepository
    )
    {
        this._qAzureOpenAIChatExtension = new QAzureOpenAIChatExtension(
            qAzureOpenAIChatOptions,
            specializationSourceRepository
        );
        this._httpClientHandler = new() { CheckCertificateRevocationList = true };
        this._httpClient = new(this._httpClientHandler);
        this._specializationRepository = specializationSourceRepository;
    }

    /// <summary>
    /// Retrieves the search results using AzureAISearch service.
    /// </summary>
    public async Task<QSearchResult?> GetMatchesAsync(QSearchParameters qsearchParameters)
    {
        string specializationId = qsearchParameters.SpecializationId;
        QAzureSearchRequest requestBody = new(qsearchParameters.Search);
        var indexName = await this.GetIndexName(specializationId);
        if (indexName == null)
        {
            return null;
        }
        var (apiKey, endpoint) = this._qAzureOpenAIChatExtension.GetAISearchDeploymentConnectionDetails(indexName);
        if (apiKey == null || endpoint == null)
        {
            return null;
        }
        using var httpRequestMessage = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{endpoint}indexes/{indexName}/docs/search?api-version=2020-06-30"),
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
        };
        httpRequestMessage.Headers.Add("api-Key", apiKey);
        var response = await this._httpClient.SendAsync(httpRequestMessage);
        var body = await response.Content.ReadAsStringAsync();
        var searchResponse = JsonSerializer.Deserialize<QAzureSearchResponse>(body!);
        return this.formatSearchResponse(searchResponse);
    }

    /// <summary>
    /// Formatter to support the nested display of results i.e, FileName -> Matches
    /// </summary>
    private QSearchResult formatSearchResponse(QAzureSearchResponse? searchResponse)
    {
        if (searchResponse != null)
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            var groupedByfilename = searchResponse
                .values.Where(res => res.highlights != null)
                .GroupBy(value => value.filename)
                .Select(g => new QSearchResultValue
                {
                    filename = g.Key,
                    matches = g.Select(
                            (value, index) =>
                                new QSearchMatch
                                {
                                    id = value.id,
                                    label = "Match-" + (index + 1),
                                    content = value.highlights.content,
                                    metadata = JsonSerializer.Deserialize<QSearchMetadata>(value.metaJsonString),
                                }
                        )
                        .ToArray(),
                });
#pragma warning restore CS8601 // Possible null reference assignment.
            return new QSearchResult { count = searchResponse.Count, values = groupedByfilename };
        }
        return new QSearchResult();
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._httpClient.Dispose();
            this._httpClientHandler?.Dispose();
        }
    }

    private async Task<string?> GetIndexName(string specializationId)
    {
        var specialiazation = await this._specializationRepository.FindByIdAsync(specializationId);
        return specialiazation?.IndexName;
    }
}
