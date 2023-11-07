using ADME.Attributes;

namespace ADME.Models;

public class SearchFilters
{
    [FacetName("kind")] public string Kind { get; set; } = string.Empty;

    [FacetName("category")] public string Category { get; set; } = string.Empty;
}