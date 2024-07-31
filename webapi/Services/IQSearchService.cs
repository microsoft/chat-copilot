#pragma warning disable IDE0073 // The file header is missing or not located at the top of the file
/// <summary>
/// Defines search service
/// </summary>
#pragma warning restore IDE0073 // The file header is missing or not located at the top of the file
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
    Task<QSearchResult> GetMatchesAsync(QSearchParameters qsearchParameters);
}
