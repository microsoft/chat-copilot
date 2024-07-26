#pragma warning disable IDE0073 // The file header is missing or not located at the top of the file
//Request definition for search
#pragma warning restore IDE0073 // The file header is missing or not located at the top of the file
using System.Text.Json.Serialization;

/// <summary>
/// Request definition for search
/// </summary>
namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Request definition for search
/// This model is built by bearing the MVP requirement of supporting simple text based search.
/// </summary>
public class QSearchParameters
{
    [JsonPropertyName("search")]
    public string Search { get; set; } = string.Empty;

    [JsonPropertyName("specializationKey")]
    public string SpecializationKey { get; set; } = string.Empty;
}
