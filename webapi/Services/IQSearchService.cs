// Copyright (c) Quartech. All rights reserved.

using System;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Response;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Defines search service
/// </summary>
public interface IQSearchService : IDisposable
{
    /// <summary>
    /// Retrieve search results from AzureAISearch endpoint.
    /// </summary>
    /// <param name="qsearchParameters">Search Parameters(Specialization, searchBy)</param>
    /// <returns>Results containing the response from search endpoint.</returns>
    Task<QSearchResult?> GetMatchesAsync(QSearchParameters qsearchParameters);
}
