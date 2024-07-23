#pragma warning disable IDE0073 // The file header is missing or not located at the top of the file
/// <summary>
/// Model for managing specialization in DB.
/// Currently, this is defined to store only specialization metadata(Key/Name/Description/ImageUrl).
/// NOTE: The key will be mapped with QAzureOpenAIChatOptions for LLM request settings.
/// </summary>
#pragma warning restore IDE0073 // The file header is missing or not located at the top of the file
namespace CopilotChat.WebApi.Models.Storage;

/// <summary>
/// A specialization session
/// </summary>
public class SpecializationSession
{
    /// <summary>
    /// Key that is persistent and unique.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Name of the specialization.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description of the specialization.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Image URL for pictorial description of specialization or logo etc.
    /// </summary>
    public string ImageFilepath { get; set; }

    public SpecializationSession(string Key, string Name, string Description, string ImageFilepath)
    {
        this.Key = Key;
        this.Name = Name;
        this.Description = Description;
        this.ImageFilepath = ImageFilepath;
    }
}
