using System.Text.Json.Serialization;
using Azure.Search.Documents.Indexes;

namespace ADME.Models;

public class SearchResult
{
    [SimpleField(IsKey = true, IsFilterable = false, IsFacetable = false, IsSortable = false, IsHidden = false)]
    [JsonPropertyName("keyfield")]
    public string? KeyField { get; set; }

    [JsonPropertyName("id")] public string? Id { get; set; }

    [JsonPropertyName("searchscore")] public int? SearchScore { get; set; }

    [SimpleField(IsFilterable = false, IsFacetable = false, IsSortable = false, IsKey = false, IsHidden = false)]
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [SimpleField(IsFilterable = false, IsFacetable = false, IsSortable = false, IsKey = false, IsHidden = false)]
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [SimpleField(IsFilterable = false, IsFacetable = false, IsSortable = false, IsKey = false, IsHidden = false)]
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [SimpleField(IsFilterable = false, IsFacetable = false, IsSortable = false, IsKey = false, IsHidden = false)]
    [JsonPropertyName("sourcepage")]
    public string? SourcePage { get; set; }

    [SimpleField(IsFilterable = false, IsFacetable = false, IsSortable = false, IsKey = false, IsHidden = false)]
    [JsonPropertyName("sourcefile")]
    public string? SourceFile { get; set; }

    [JsonPropertyName("acl")]
    [SearchableField(IsHidden = false, IsFilterable = true)]
    public string[]? Acl { get; set; }
}