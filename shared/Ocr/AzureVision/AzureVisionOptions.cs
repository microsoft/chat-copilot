using System.ComponentModel.DataAnnotations;
namespace CopilotChat.Shared.Ocr.AzureVision;

public sealed class AzureVisionOptions
{
    public const string SectionName = "AzureVision";

    [Required]
    public string? Endpoint { get; set; } = string.Empty;
    [Required]
    public string? Key { get; set; } = string.Empty;
}