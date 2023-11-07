using ADME.Models;

namespace ADME.Services.Interfaces;

public interface ICognitiveSearchService
{
    Task<T?> GetDocumentInfoByKeyAsync<T>(string key) where T : SearchResult;
    Task<SearchData<T>> SearchAsync<T>(string searchText, SearchFilters filter, CancellationToken cts);
}