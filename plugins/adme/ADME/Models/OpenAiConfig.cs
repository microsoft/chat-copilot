using System.Text.Json.Serialization;

namespace ADME.Models;

public class OpenAiConfig
{
    [JsonPropertyName("EndPoint")] public string EndPoint { get; set; } = string.Empty;
    [JsonPropertyName("EmbeddingModel")] public string EmbeddingModel { get; set; } = string.Empty;
}