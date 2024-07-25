#pragma warning disable IDE0073 // The file header is missing or not located at the top of the file
///<summary>
/// Repository class for chat specialization
///</summary>
#pragma warning restore IDE0073 // The file header is missing or not located at the top of the file
using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A repository for chat specialization sessions.
/// </summary>
public class ChatSpecializationSessionRepository : Repository<ChatSpecializationSession>
{
    /// <summary>
    /// Initializes a new instance of the ChatSpecializationSessionRepository class.
    /// </summary>
    /// <param name="storageContext">The storage context.</param>
    public ChatSpecializationSessionRepository(IStorageContext<ChatSpecializationSession> storageContext)
        : base(storageContext)
    {
    }
}
