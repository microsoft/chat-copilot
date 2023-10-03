// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using Microsoft.SemanticMemory;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// The data model of a chat archive.
/// </summary>
public class ChatArchive
{
    /// <summary>
    /// Schema information for the chat archive.
    /// </summary>
    public ChatArchiveSchemaInfo Schema { get; set; } = new ChatArchiveSchemaInfo();

    /// <summary>
    /// The embedding configurations.
    /// </summary>
    public ChatArchiveEmbeddingConfig EmbeddingConfigurations { get; set; } = new ChatArchiveEmbeddingConfig();

    /// <summary>
    /// Chat title.
    /// </summary>
    public string ChatTitle { get; set; } = string.Empty;

    /// <summary>
    /// The system description of the chat that is used to generate responses.
    /// </summary>
    public string SystemDescription { get; set; } = string.Empty;

    /// <summary>
    /// The chat history. It contains all the messages in the conversation with the bot.
    /// </summary>
    public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();

    /// <summary>
    /// Chat archive's embeddings.
    /// </summary>
    public Dictionary<string, List<Citation>> Embeddings { get; set; } = new Dictionary<string, List<Citation>>();

    /// <summary>
    /// The embeddings of uploaded documents in Copilot Chat. It represents the document memory which is accessible to all chat sessions of a given user.
    /// </summary>
    public Dictionary<string, List<Citation>> DocumentEmbeddings { get; set; } = new Dictionary<string, List<Citation>>();
}
