﻿// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Form for importing a document from a POST Http request.
/// </summary>
public class DocumentImportForm
{
    /// <summary>
    /// The file to import.
    /// </summary>
    public IEnumerable<IFormFile> FormFiles { get; set; } = Enumerable.Empty<IFormFile>();

    /// <summary>
    /// Flag indicating whether user has content safety enabled from the client.
    /// </summary>
    public bool UseContentSafety { get; set; } = false;
}
