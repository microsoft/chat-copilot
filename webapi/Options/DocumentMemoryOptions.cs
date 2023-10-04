// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ComponentModel.DataAnnotations;

namespace CopilotChat.WebApi.Options;

/// <summary>
/// Configuration options for handling memorized documents.
/// </summary>
public class DocumentMemoryOptions
{
    public const string PropertyName = "DocumentMemory";

    /// <summary>
    /// Global documents will be tagged by an empty Guid as chat-id ("00000000-0000-0000-0000-000000000000").
    /// </summary>
    internal static readonly Guid GlobalDocumentChatId = Guid.Empty;

    /// <summary>
    /// Gets or sets the name of the global document collection.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string GlobalDocumentCollectionName { get; set; } = "global-documents";

    /// <summary>
    /// Gets or sets the prefix for the chat document collection name.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string ChatDocumentCollectionNamePrefix { get; set; } = "chat-documents-";

    /// <summary>
    /// Gets or sets the maximum number of tokens to use when splitting a document into "lines".
    /// For more details on tokens and how to count them, see:
    /// https://help.openai.com/en/articles/4936856-what-are-tokens-and-how-to-count-them
    /// </summary>
    [Range(0, int.MaxValue)]
    public int DocumentLineSplitMaxTokens { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of tokens to use when splitting documents for embeddings.
    /// For more details on tokens and how to count them, see:
    /// https://help.openai.com/en/articles/4936856-what-are-tokens-and-how-to-count-them
    /// </summary>
    [Range(0, int.MaxValue)]
    public int DocumentChunkMaxTokens { get; set; } = 100;

    /// <summary>
    /// Maximum size in bytes of a document to be allowed for importing.
    /// Prevent large uploads by setting a file size limit (in bytes) as suggested here:
    /// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-6.0
    /// </summary>
    [Range(0, int.MaxValue)]
    public int FileSizeLimit { get; set; } = 1000000;

    /// <summary>
    /// Maximum number of files to be allowed for importing in a single request.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int FileCountLimit { get; set; } = 10;
}
