using System.Text.Json;
using ADME.Helpers;
using ADME.Models;
using ADME.Services.Interfaces;
using Azure;
using Azure.Core.Serialization;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;

namespace ADME.Services;

public class CognitiveSearchService : ICognitiveSearchService
{
    private readonly ILogger<CognitiveSearchService> _logger;
    private readonly SearchClient _searchClient;

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        Converters = {new MicrosoftSpatialGeoJsonConverter()}
    };
    //private readonly ISecurityGroupService _securityGroupService;

    public CognitiveSearchService(SearchClient searchClient, ILogger<CognitiveSearchService> logger)
    {
        _searchClient = searchClient;
        //_securityGroupService = securityGroupService;
        _logger = logger;
    }

    public async Task<SearchData<T>> SearchAsync<T>(string searchText, SearchFilters filter,
        CancellationToken cancellationToken)
    {
        SearchData<T> data = new()
        {
            SearchText = searchText,
            Filter = filter,
        };

        SearchOptions searchOptions = new() {Filter = filter.ToFilterString()};
        searchOptions.SetDefaultValues();
        searchOptions.SetAttributeFilters<T>(3);
        //searchOptions.SetSecurityFilter(await _securityGroupService.GetMyGroupsAsync());

        SearchResults<T> searchResult =
            await _searchClient.SearchAsync<T>(searchText, searchOptions, cancellationToken);
        data.SetResultValues(searchResult);

        return data;
    }

    public async Task<T?> GetDocumentInfoByKeyAsync<T>(string key) where T : SearchResult
    {
        try
        {
            Response<object> tmpResult = await _searchClient.GetDocumentAsync<object>(key);

            string tmpObject = JsonSerializer.Serialize(tmpResult.Value);

            T? searchResult = JsonSerializer.Deserialize<T>(tmpObject, _serializerOptions);

            if (searchResult == null)
                return default;

            // if (await _securityGroupService.UserHasAclGroupAsync(searchResult.Acl))
            //     return searchResult;
            return searchResult;
        }
        // catch (UnauthorizedAccessException)
        // {
        //     throw new UnauthorizedAccessException("No access to documents");
        // }
        catch (Exception e)
        {
            _logger.LogError(e, "CognitiveSearchService: {Message}", e.Message);
        }

        return default;
    }
}