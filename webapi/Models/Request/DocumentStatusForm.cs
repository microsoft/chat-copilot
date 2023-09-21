// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Form for importing a document from a POST Http request.
/// </summary>
public class DocumentStatusForm
{
    /// <summary>
    /// The file to import.
    /// </summary>
    public IEnumerable<string> FileReferences { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Scope of the document. This determines the collection name in the document memory.
    /// </summary>
    public DocumentScopes DocumentScope { get; set; } = DocumentScopes.Chat;

    /// <summary>
    /// The ID of the chat that owns the document.
    /// This is used to create a unique collection name for the chat.
    /// If the chat ID is not specified or empty, the documents will be stored in a global collection.
    /// If the document scope is set to global, this value is ignored.
    /// </summary>
    public Guid ChatId { get; set; } = Guid.Empty;

    /// <summary>
    /// The ID of the user who is importing the document to a chat session.
    /// Will be use to validate if the user has access to the chat session.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the user who sent this message.
    /// Will be used to create the chat message representing the document upload.
    /// </summary>
    public string UserName { get; set; } = string.Empty;
}
