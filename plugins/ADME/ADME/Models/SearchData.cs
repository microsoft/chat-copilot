using Azure;
using Azure.Search.Documents.Models;

namespace ADME.Models;

public class SearchData<T>
{
    public string SearchText { get; set; }

    public long? TotalCount { get; set; }

    public Pageable<SearchResult<T>>? ResultList { get; set; }

    public Dictionary<string, List<FacetData>> Facets { get; set; } = new();
    public SearchFilters? Filter { get; set; }

    public int? MaxFacets { get; set; }

    public string[]? FacetText { get; set; }

    public bool[]? FacetOn { get; set; }

    public string? Paging { get; set; }

    public string? Scoring { get; set; }
}