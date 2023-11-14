using ADME.Attributes;

namespace ADME.Models;

public class SearchFilters
{
    [FacetName("kind")] public string KindFilter { get; set; } = string.Empty;

    [FacetName("category")] public string CategoryFilter { get; set; } = string.Empty;
    
    [FacetName("id")] public string Id { get; set; } = string.Empty;
}