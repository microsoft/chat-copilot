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
    private QAzureOpenAIChatExtension _qAzureOpenAIChatExtension;

    public QSearchService(QAzureOpenAIChatOptions qAzureOpenAIChatOptions, SpecializationSourceRepository specializationSourceRepository)
    {
        this._qAzureOpenAIChatExtension = new QAzureOpenAIChatExtension(qAzureOpenAIChatOptions, specializationSourceRepository);
        this._httpClientHandler = new() { CheckCertificateRevocationList = true };
        this._httpClient = new(this._httpClientHandler);
    }

    /// <summary>
    /// Retrieves the search results using AzureAISearch service.
    /// </summary>
    public async Task<QSearchResult> GetMatchesAsync(QSearchParameters qsearchParameters)
    {
        string specializationKey = qsearchParameters.SpecializationKey;
        QAzureSearchRequest requestBody = new(qsearchParameters.Search);
        using var httpRequestMessage = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{this.GetEndpoint(specializationKey)}indexes/{this.GetIndexName(specializationKey)}/docs/search?api-version=2020-06-30"),
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
        };
        httpRequestMessage.Headers.Add("api-Key", this.GetApiKey(specializationKey));
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
            var groupedByfilename = searchResponse.values.Where(res => res.highlights != null)
                         .GroupBy(value => value.filename)
                         .Select(g => new QSearchResultValue
                         {
                             filename = g.Key,
                             matches = g.Select((value, index) => new QSearchMatch
                             {
                                 id = value.id,
                                 label = "Match-" + (index + 1),
                                 content = value.highlights.content,
                                 metadata = JsonSerializer.Deserialize<QSearchMetadata>(value.metaJsonString)
                             })
                                        .ToArray(),
                         });
#pragma warning restore CS8601 // Possible null reference assignment.
            return new QSearchResult
            {
                count = searchResponse.Count,
                values = groupedByfilename
            };
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
    private string GetApiKey(string specializationKey)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        return this._qAzureOpenAIChatExtension.AzureConfig.APIKey;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }

    private string GetEndpoint(string specializationKey)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        return this._qAzureOpenAIChatExtension.AzureConfig.Endpoint.ToString();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }

    private string GetIndexName(string specializationKey)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        return this._qAzureOpenAIChatExtension.GetSpecializationIndexByKey(specializationKey).IndexName;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }
}
