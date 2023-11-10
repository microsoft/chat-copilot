using System.Reflection;
using System.Text.Json.Serialization;
using ADME.Attributes;
using ADME.Models;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;

namespace ADME.Helpers;

public static class CognitiveSearchExtensions
{
    public static string ToFilterString(this SearchFilters filter)
    {
        List<string> filters = new();

        foreach (PropertyInfo property in filter.GetType().GetProperties())
        {
            FacetNameAttribute? item = property.GetCustomAttribute<FacetNameAttribute>();

            object? value = filter?.GetType()?.GetProperty(property.Name)?.GetValue(filter, null);

            if (value is string && value.ToString() != string.Empty && item != null)
                filters.Add(item.Collection
                    ? $"{item.Name}/any(t: search.in(t,'{value}','|'))"
                    : $"search.in({item.Name},'{value}','|')");
        }

        return string.Join(" and ", filters);
    }

    public static Dictionary<string, List<FacetData>> ToFacetDictionary(
        this IDictionary<string, IList<FacetResult>> facetResults)
    {
        return facetResults.Aggregate(new Dictionary<string, List<FacetData>>(), (acc, x) =>
        {
            acc.Add(x.Key,
                x.Value.Select(e => new FacetData() {Value = e.Value?.ToString(), Count = e.Count}).ToList());
            return acc;
        });
    }

    public static void SetResultValues<T>(this SearchData<T> data, SearchResults<T> result)
    {
        data.ResultList = result.GetResults();
        //data.Facets = result.Facets.ToFacetDictionary();
        data.TotalCount = result.TotalCount;
    }

    public static void SetAttributeFilters<T>(this SearchOptions options, int maxFacets)
    {
        foreach (PropertyInfo prop in typeof(T).GetProperties())
        {
            object[] customAttributes = prop.GetCustomAttributes(true);

            string jsonName = prop.Name;
            bool isFacet = false;
            bool includeInResult = false;
            FacetSortAttribute.SortType sort = FacetSortAttribute.SortType.SortByCount;

            foreach (object att in customAttributes)
            {
                switch (att)
                {
                    case JsonPropertyNameAttribute a:
                        jsonName = a.Name;
                        break;
                    case SimpleFieldAttribute a:
                        if (a.IsFacetable)
                            isFacet = true;
                        break;
                    case FacetSortAttribute a:
                        sort = a.Sort;
                        break;
                    case SearchResultIncludeAttribute:
                        includeInResult = true;
                        break;
                    default:
                        break;
                }
            }

            if (isFacet)
                options.Facets.Add($"{jsonName},count:{maxFacets},{sort.ToSortString()}");

            if (includeInResult)
                options.Select.Add(jsonName);
        }
    }

    public static void SetSecurityFilter(this SearchOptions options, IEnumerable<string> groups)
    {
        if (groups == null) throw new Exception("No security groups");

        string securityFilter =
            $"acl/any(g: search.in(g, '{string.Join("|", groups.Where(e => !string.IsNullOrEmpty(e)))}',' |'))";

        options.Filter = string.IsNullOrEmpty(options.Filter)
            ? securityFilter
            : $"{options.Filter} and {securityFilter}";
    }

    public static void SetDefaultValues(this SearchOptions options)
    {
        options.Size = 3;
        options.IncludeTotalCount = true;
        options.SearchMode = SearchMode.All;
        options.QueryType = SearchQueryType.Semantic;
        options.QueryLanguage = QueryLanguage.EnUs;
        options.ScoringStatistics = ScoringStatistics.Global;
        options.SemanticFields.Add("content");
    }

    public static string ToSortString(this FacetSortAttribute.SortType type) => type switch
    {
        FacetSortAttribute.SortType.SortByValue => "sort:value",
        FacetSortAttribute.SortType.SortByCount => "sort:count",
        _ => throw new NotImplementedException()
    };
}