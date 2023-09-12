// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory;

namespace CopilotChat.WebApi.Models.Storage;

/// <summary>
/// Information about a citation source.
/// This is a replica of the <see cref="Citation"/> class in Semantic Memory.
/// Creating a replica here is to avoid taking a direct dependency on Semantic Memory in our data model.
/// </summary>
public class CitationSource
{
    /// <summary>
    /// Link of the citation.
    /// </summary>
    public string Link { get; set; } = string.Empty;

    /// <summary>
    /// Type of source, e.g. PDF, Word, Chat, etc.
    /// </summary>
    public string SourceContentType { get; set; } = string.Empty;

    /// <summary>
    /// Name of the source, e.g. file name.
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// The snippet of the citation.
    /// </summary>
    public string Snippet { get; set; } = string.Empty;

    /// <summary>
    /// Relevance score of the citation against the query.
    /// </summary>
    public double RelevanceScore { get; set; } = 0.0;

    /// <summary>
    /// Converts a <see cref="Citation"/> to a <see cref="CitationSource"/>.
    /// </summary>
    public static CitationSource FromSemanticMemoryCitation(Citation citation, string snippet, double relevanceScore)
    {
        var citationSource = new CitationSource
        {
            Link = citation.Link,
            SourceContentType = citation.SourceContentType,
            SourceName = citation.SourceName,
            Snippet = snippet,
            RelevanceScore = relevanceScore
        };

        return citationSource;
    }
}
