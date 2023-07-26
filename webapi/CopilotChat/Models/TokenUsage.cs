using System.Text.Json.Serialization;

namespace SemanticKernel.Service.CopilotChat.Models;

/// <summary>
/// Information about token usage used to generate bot response.
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// Semantic functions required to generate bot response prompt.
    /// </summary>
    public static readonly string[] semanticDependencies = {
        "audienceExtraction",
        "userIntentExtraction",
        "planner",
        "memoryExtraction"
    };

    /// <summary>
    /// Total token usage of prompt chat completion.
    /// </summary>
    [JsonPropertyName("prompt")]
    public int PromptTotal { get; set; }

    /// <summary>
    /// Total token usage across all semantic dependencies used to generate prompt.
    /// </summary>
    [JsonPropertyName("dependency")]
    public int DependencyTotal { get; set; }

    public TokenUsage(int promptToken, int dependencyTotal)
    {
        this.PromptTotal = promptToken;
        this.DependencyTotal = dependencyTotal;
    }
}