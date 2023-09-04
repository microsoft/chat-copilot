// Copyright (c) Microsoft. All rights reserved.


using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace CopilotChat.WebApi.Models.Request;

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

}

