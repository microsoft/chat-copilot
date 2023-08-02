using System;

namespace SemanticKernel.Service.CopilotChat.Models
{
    /// <summary>
    /// Form for deleting a document from a POST Http request.
    /// </summary>
    public class DocumentDeleteForm
    {
        /// <summary>
        /// The ID of the document to delete.
        /// </summary>
        public Guid DocumentId { get; set; } = Guid.Empty;

        /// <summary>
        /// The ID of the chat that owns the document.
        /// This is used to verify if the user has access to the chat session and delete the document from the appropriate chat.
        /// </summary>
        public Guid ChatId { get; set; } = Guid.Empty;

        /// <summary>
        /// The ID of the user who is deleting the document from a chat session.
        /// This will be used to validate if the user has access to the chat session.
        /// </summary>
        public string UserId { get; set; } = string.Empty;
    }
}
