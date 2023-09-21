// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using Microsoft.SemanticMemory;

namespace CopilotChat.WebApi.Models.Response;

/// <summary>
/// The data model of a bot for portability.
/// </summary>
public class Bot
{
    /// <summary>
    /// The schema information of the bot data model.
    /// </summary>
    public BotSchemaOptions Schema { get; set; } = new BotSchemaOptions();

    /// <summary>
    /// The embedding configurations.
    /// </summary>
    public BotEmbeddingConfig EmbeddingConfigurations { get; set; } = new BotEmbeddingConfig();

    /// <summary>
    /// The title of the chat with the bot.
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
    /// The embeddings of the bot.
    /// </summary>
    public Dictionary<string, List<Citation>> Embeddings { get; set; } = new Dictionary<string, List<Citation>>();

    /// <summary>
    /// The embeddings of uploaded documents in Copilot Chat. It represents the document memory which is accessible to all chat sessions of a given user.
    /// </summary>
    public Dictionary<string, List<Citation>> DocumentEmbeddings { get; set; } = new Dictionary<string, List<Citation>>();
}
