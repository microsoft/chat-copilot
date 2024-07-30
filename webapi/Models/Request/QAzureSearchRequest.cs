#pragma warning disable IDE0073 // The file header is missing or not located at the top of the file
//Request definition for AzureAIsearch
#pragma warning restore IDE0073 // The file header is missing or not located at the top of the file
using System.Text.Json.Serialization;

/// <summary>
/// Request definition for AzureAIsearch
/// </summary>
namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Request definition for AzureAIsearch
/// This model is built by bearing the MVP requirement of supporting simple text based search.
/// </summary>
public record QAzureSearchRequest
{
    [JsonPropertyName("search")]
    public string? Search { get; set; }

    public bool? count { get; } = true;

    public string highlight { get; } = "content";

    public string queryType { get; } = "simple";

    public string searchMode { get; } = "all";

    public string select { get; } = "content, filepath, url, meta_json_string, id";

    public string highlightPreTag { get; } = "<mark>";

    public string highlightPostTag { get; } = "</mark>";

    public QAzureSearchRequest(string searchby)
    {
        this.Search = searchby;
    }
}
